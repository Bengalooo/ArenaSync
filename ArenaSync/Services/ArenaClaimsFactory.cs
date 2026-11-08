using ArenaSync.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ArenaSync.Services
{
    // Dodaje do ciasteczka logowania drużynę zawodnika i jego imię, żeby nie pytać bazy przy każdym widoku
    public class ArenaClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public const string DruzynaClaim = "arena:druzyna";
        public const string ImieClaim = "arena:imie";
        public const string StartClaim = "arena:start";

        // Identyfikator bieżącego uruchomienia serwera; logowanie z poprzedniego uruchomienia jest odrzucane
        public static readonly string IdUruchomienia = Guid.NewGuid().ToString("N");

        public ArenaClaimsFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            if (user.DruzynaId != null)
                identity.AddClaim(new Claim(DruzynaClaim, user.DruzynaId.Value.ToString()));
            if (!string.IsNullOrWhiteSpace(user.ImieNazwisko))
                identity.AddClaim(new Claim(ImieClaim, user.ImieNazwisko));
            identity.AddClaim(new Claim(StartClaim, IdUruchomienia));
            return identity;
        }
    }
}
