using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            SprawdzZgode();
            if (!ModelState.IsValid) return View(model);

            var wynik = await _signInManager.PasswordSignInAsync(model.Email, model.Haslo, isPersistent: false, lockoutOnFailure: true);
            if (wynik.Succeeded)
                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");

            if (wynik.IsLockedOut)
            {
                // Komunikat z adresem administratora wyświetla widok Login
                ViewBag.Zablokowane = true;
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Niepoprawny e-mail lub hasło.");
            ViewBag.Ostrzezenie = true;
            return View(model);
        }

        // Rejestracja kapitana albo zawodnika (członka drużyny)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            await WypelnijDruzyny(null);
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            SprawdzZgode();
            if (model.Rola != Role.Kapitan && model.Rola != Role.Zawodnik)
                ModelState.AddModelError(nameof(model.Rola), "Wybierz rodzaj konta.");
            if (model.Rola == Role.Zawodnik && !await _context.Druzyny.AnyAsync(d => d.IdDruzyny == model.DruzynaId && d.Zatwierdzona))
                ModelState.AddModelError(nameof(model.DruzynaId), "Zawodnik musi wybrać swoją drużynę.");

            if (!ModelState.IsValid)
            {
                await WypelnijDruzyny(model.DruzynaId);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                ImieNazwisko = model.ImieNazwisko,
                DruzynaId = model.Rola == Role.Zawodnik ? model.DruzynaId : null
            };
            var wynik = await _userManager.CreateAsync(user, model.Haslo);
            if (wynik.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Rola);
                await _signInManager.SignInAsync(user, isPersistent: false);
                if (model.Rola == Role.Kapitan)
                {
                    TempData["Komunikat"] = "Konto kapitana utworzone. Zgłoś drużynę - administrator ją zatwierdzi.";
                    return RedirectToAction("Create", "Druzyny");
                }
                TempData["Komunikat"] = "Konto zawodnika utworzone. Witaj w drużynie!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var blad in wynik.Errors)
                ModelState.AddModelError(string.Empty, TlumaczBlad(blad));
            await WypelnijDruzyny(model.DruzynaId);
            return View(model);
        }

        // Ustawienia konta: imię i nazwisko (zmiana hasła - osobna strona ZmianaHasla)
        [HttpGet]
        public async Task<IActionResult> Ustawienia()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            ViewBag.Druzyna = user.DruzynaId == null ? null : (await _context.Druzyny.FindAsync(user.DruzynaId))?.Nazwa;
            ViewBag.Role = await _userManager.GetRolesAsync(user);
            return View(new UstawieniaViewModel { ImieNazwisko = user.ImieNazwisko ?? "" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ustawienia(UstawieniaViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                ViewBag.Druzyna = user.DruzynaId == null ? null : (await _context.Druzyny.FindAsync(user.DruzynaId))?.Nazwa;
                ViewBag.Role = await _userManager.GetRolesAsync(user);
                return View(model);
            }

            user.ImieNazwisko = model.ImieNazwisko;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            TempData["Komunikat"] = "Zapisano ustawienia konta.";
            return RedirectToAction(nameof(Ustawienia));
        }

        // Zmiana hasła przez zalogowanego użytkownika (każda rola)
        [HttpGet]
        public IActionResult ZmianaHasla() => View(new ZmianaHaslaViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ZmianaHasla(ZmianaHaslaViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (ModelState.IsValid && model.NoweHaslo == model.ObecneHaslo)
                ModelState.AddModelError(nameof(model.NoweHaslo), "Nowe hasło musi być inne niż obecne.");

            if (ModelState.IsValid)
            {
                var zmiana = await _userManager.ChangePasswordAsync(user, model.ObecneHaslo, model.NoweHaslo);
                foreach (var blad in zmiana.Errors)
                {
                    if (blad.Code == "PasswordMismatch")
                        ModelState.AddModelError(nameof(model.ObecneHaslo), "Obecne hasło jest niepoprawne.");
                    else
                        ModelState.AddModelError(nameof(model.NoweHaslo), TlumaczBlad(blad));
                }
            }

            if (!ModelState.IsValid) return View(model);

            // Zmiana hasła zmienia SecurityStamp - odświeżamy logowanie, żeby nie wylogować użytkownika
            await _signInManager.RefreshSignInAsync(user);
            TempData["Komunikat"] = "Hasło zostało zmienione.";
            return RedirectToAction(nameof(Ustawienia));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // Logowanie używa ciasteczka sesji, więc wymaga wcześniejszego zapoznania się z zasadami cookies
        private void SprawdzZgode()
        {
            if (string.IsNullOrEmpty(Request.Cookies[ZgodaCookie]))
                ModelState.AddModelError(string.Empty, "Najpierw zaakceptuj zasady korzystania z plików cookies (okno na stronie).");
        }

        public const string ZgodaCookie = "ArenaSync.Zgoda";

        private async Task WypelnijDruzyny(int? wybrana)
        {
            var druzyny = await _context.Druzyny.Include(d => d.Turniej).Where(d => d.Zatwierdzona)
                .OrderBy(d => d.Nazwa).ToListAsync();
            ViewBag.DruzynaId = druzyny.Select(d => new SelectListItem($"{d.Nazwa} ({d.Turniej!.Nazwa})", d.IdDruzyny.ToString(), d.IdDruzyny == wybrana));
        }

        private static string TlumaczBlad(IdentityError blad) => blad.Code switch
        {
            "DuplicateUserName" or "DuplicateEmail" => "Konto z tym adresem e-mail już istnieje.",
            "PasswordTooShort" => "Hasło jest za krótkie.",
            "PasswordRequiresDigit" => "Hasło musi zawierać cyfrę.",
            "PasswordRequiresLower" => "Hasło musi zawierać małą literę.",
            "PasswordRequiresUpper" => "Hasło musi zawierać wielką literę.",
            "PasswordRequiresNonAlphanumeric" => "Hasło musi zawierać znak specjalny (np. ! @ # ?).",
            _ => blad.Description
        };
    }
}
