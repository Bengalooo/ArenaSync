using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Models.ViewModels;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    // Zarządzanie kontami i rolami - tylko administrator
    [Authorize(Roles = Role.Admin)]
    public class UzytkownicyController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public UzytkownicyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Uzytkownicy?rola=Kapitan
        public async Task<IActionResult> Index(string? rola, string? szukaj, string? status, string? sort, string? kier)
        {
            var uzytkownicy = await _context.Users.Include(u => u.Druzyna).OrderBy(u => u.Email).ToListAsync();
            var prowadzone = await _context.Druzyny.Where(d => d.KapitanId != null).ToListAsync();

            var lista = new List<UzytkownikViewModel>();
            foreach (var u in uzytkownicy)
            {
                var role = await _userManager.GetRolesAsync(u);
                lista.Add(new UzytkownikViewModel
                {
                    Id = u.Id,
                    Email = u.Email ?? u.UserName ?? "",
                    ImieNazwisko = u.ImieNazwisko,
                    Rola = role.FirstOrDefault() ?? "",
                    DruzynaId = u.DruzynaId,
                    NazwaDruzyny = u.Druzyna?.Nazwa,
                    ProwadzoneDruzyny = prowadzone.Where(d => d.KapitanId == u.Id).Select(d => d.Nazwa).ToList(),
                    Zablokowane = CzyZablokowane(u),
                    BlokadaWlaczona = u.LockoutEnabled,
                    NieudaneProby = u.AccessFailedCount
                });
            }

            if (!string.IsNullOrEmpty(rola)) lista = lista.Where(u => u.Rola == rola).ToList();
            lista = lista.Where(u => Wyszukiwanie.KtorekolwiekZawiera(szukaj, u.Email, u.ImieNazwisko, u.NazwaDruzyny)).ToList();

            if (status == "zablokowane") lista = lista.Where(u => u.Zablokowane).ToList();

            lista = Sortowanie.Sortuj(lista, sort, kier, new()
            {
                ["uzytkownik"] = u => u.ImieNazwisko ?? u.Email,
                ["rola"] = u => Role.Nazwa(u.Rola),
                ["druzyna"] = u => u.NazwaDruzyny ?? string.Join(", ", u.ProwadzoneDruzyny),
                ["stan"] = u => u.Zablokowane
            });

            ViewBag.Rola = rola;
            ViewBag.Status = status;
            ViewBag.Szukaj = szukaj;
            return View(lista);
        }

        // GET: Uzytkownicy/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UzytkownikViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                ImieNazwisko = user.ImieNazwisko,
                Rola = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Role.Zawodnik,
                DruzynaId = user.DruzynaId,
                Zablokowane = CzyZablokowane(user),
                BlokadaWlaczona = user.LockoutEnabled,
                NieudaneProby = user.AccessFailedCount
            };
            await WypelnijListy(model);
            return View(model);
        }

        // POST: Uzytkownicy/Edit/id - zmiana roli, drużyny, imienia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UzytkownikViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!Role.Wszystkie.Contains(model.Rola))
                ModelState.AddModelError(nameof(model.Rola), "Niepoprawna rola.");
            if (user.Id == UserId && model.Rola != Role.Admin)
                ModelState.AddModelError(nameof(model.Rola), "Nie możesz odebrać sobie roli administratora.");
            ModelState.Remove(nameof(model.Email));

            if (!ModelState.IsValid)
            {
                model.Email = user.Email ?? "";
                model.Zablokowane = CzyZablokowane(user);
                model.BlokadaWlaczona = user.LockoutEnabled;
                model.NieudaneProby = user.AccessFailedCount;
                await WypelnijListy(model);
                return View(model);
            }

            user.ImieNazwisko = model.ImieNazwisko;
            user.DruzynaId = model.Rola == Role.Zawodnik ? model.DruzynaId : null;
            await _userManager.UpdateAsync(user);

            var obecne = await _userManager.GetRolesAsync(user);
            if (!obecne.SequenceEqual(new[] { model.Rola }))
            {
                await _userManager.RemoveFromRolesAsync(user, obecne);
                await _userManager.AddToRoleAsync(user, model.Rola);
            }
            // Administratorów nie blokuje się po błędnych hasłach
            bool blokowalne = model.Rola != Role.Admin;
            if (user.LockoutEnabled != blokowalne)
            {
                if (!blokowalne) await Odblokuj(user);
                await _userManager.SetLockoutEnabledAsync(user, blokowalne);
            }
            // Wymusza odświeżenie ciasteczka logowania z nową rolą
            await _userManager.UpdateSecurityStampAsync(user);
            if (user.Id == UserId) await _signInManager.RefreshSignInAsync(user);

            Komunikat($"Zapisano konto {user.Email} (rola: {Role.Nazwa(model.Rola)}).");
            return RedirectToAction(nameof(Index));
        }

        // GET: Uzytkownicy/Delete/id
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _context.Users.Include(u => u.Druzyna).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            if (user.Id == UserId)
            {
                Komunikat("Nie możesz usunąć własnego konta.");
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Rola = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
            ViewBag.Prowadzone = await _context.Druzyny.Where(d => d.KapitanId == id).Select(d => d.Nazwa).ToListAsync();
            return View(user);
        }

        // POST: Uzytkownicy/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.Id == UserId) return RedirectToAction(nameof(Index));

            // Drużyny prowadzone przez kapitana zostają, tracą tylko kapitana
            foreach (var d in _context.Druzyny.Where(d => d.KapitanId == id))
                d.KapitanId = null;
            await _context.SaveChangesAsync();

            await _userManager.DeleteAsync(user);
            Komunikat($"Usunięto konto {user.Email}.");
            return RedirectToAction(nameof(Index));
        }

        // POST: Uzytkownicy/Odblokuj/id - zdjęcie blokady po błędnych hasłach
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Odblokuj(string id, string? returnUrl)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            await Odblokuj(user);
            Komunikat($"Odblokowano konto {user.Email}.");
            return Powrot(returnUrl);
        }

        // POST: Uzytkownicy/Zablokuj/id - ręczna blokada konta (np. na prośbę drużyny)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Zablokuj(string id, string? returnUrl)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            if (user.Id == UserId || await _userManager.IsInRoleAsync(user, Role.Admin))
            {
                Komunikat("Nie można zablokować konta administratora.");
                return Powrot(returnUrl);
            }
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            // Wylogowuje użytkownika ze wszystkich otwartych sesji
            await _userManager.UpdateSecurityStampAsync(user);
            Komunikat($"Zablokowano konto {user.Email}. Użytkownik nie zaloguje się, dopóki go nie odblokujesz.");
            return Powrot(returnUrl);
        }

        // GET: Uzytkownicy/ResetHasla/id
        public async Task<IActionResult> ResetHasla(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(new ResetHaslaViewModel { Id = user.Id, Email = user.Email ?? "" });
        }

        // POST: Uzytkownicy/ResetHasla/id - ustawia nowe hasło (puste pole = hasło tymczasowe) i odblokowuje konto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetHasla(string id, ResetHaslaViewModel model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            model.Email = user.Email ?? "";
            if (!ModelState.IsValid) return View(model);

            bool tymczasowe = string.IsNullOrEmpty(model.NoweHaslo);
            string haslo = tymczasowe ? Hasla.Tymczasowe() : model.NoweHaslo!;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var wynik = await _userManager.ResetPasswordAsync(user, token, haslo);
            if (!wynik.Succeeded)
            {
                foreach (var e in wynik.Errors) ModelState.AddModelError(nameof(model.NoweHaslo), e.Description);
                return View(model);
            }
            await Odblokuj(user);
            if (user.Id == UserId) await _signInManager.RefreshSignInAsync(user);

            if (tymczasowe)
            {
                ViewBag.Haslo = haslo;
                return View("HasloUstawione", model);
            }
            Komunikat($"Ustawiono nowe hasło dla {user.Email}. Konto jest odblokowane.");
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Konta adminów (LockoutEnabled = false) mogą mieć ustawioną datę blokady, ale Identity jej nie egzekwuje
        private static bool CzyZablokowane(ApplicationUser u) => u.LockoutEnabled && u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow;

        private async Task Odblokuj(ApplicationUser user)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        private IActionResult Powrot(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));

        private async Task WypelnijListy(UzytkownikViewModel model)
        {
            ViewBag.Role = Role.Wszystkie.Select(r => new SelectListItem(Role.Nazwa(r), r, r == model.Rola));
            ViewBag.DruzynaId = new SelectList(await _context.Druzyny.OrderBy(d => d.Nazwa).ToListAsync(), "IdDruzyny", "Nazwa", model.DruzynaId);
        }
    }
}
