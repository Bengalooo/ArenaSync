using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Baza danych SQLite (plik arenasync.db w folderze aplikacji)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity z rolami (RBAC)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Wymagania haseł (te same w formularzach: Services/Hasla.cs)
        options.Password.RequiredLength = Hasla.MinDlugosc;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
        // 3 błędne hasła = blokada do odblokowania przez administratora (konta adminów nie są blokowane)
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(36500);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ArenaClaimsFactory>();

// Cała aplikacja wymaga logowania; publiczne są tylko akcje z [AllowAnonymous]
// (strona startowa, logowanie, rejestracja, polityka prywatności)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // Po każdym starcie aplikacji trzeba zalogować się od nowa
    options.Events.OnValidatePrincipal = async kontekst =>
    {
        if (kontekst.Principal?.FindFirstValue(ArenaClaimsFactory.StartClaim) != ArenaClaimsFactory.IdUruchomienia)
        {
            kontekst.RejectPrincipal();
            await kontekst.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        // Domyślna walidacja Identity: zmiana roli, blokada lub reset hasła przez admina działa od razu
        await SecurityStampValidator.ValidatePrincipalAsync(kontekst);
    };
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddScoped<ArenaSeeder>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Polskie formatowanie dat i liczb
var kultura = new CultureInfo("pl-PL");
CultureInfo.DefaultThreadCurrentCulture = kultura;
CultureInfo.DefaultThreadCurrentUICulture = kultura;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(kultura),
    SupportedCultures = new[] { kultura },
    SupportedUICultures = new[] { kultura }
});

// Migracje + dane przykładowe przy starcie
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ArenaSeeder>();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
