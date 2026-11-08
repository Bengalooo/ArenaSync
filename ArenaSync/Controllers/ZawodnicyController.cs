using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    public class ZawodnicyController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ZawodnicyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Zawodnicy?druzynaId=1
        // Kibic widzi zamaskowane nazwiska (RODO), admin i kapitan pełne dane swojej kadry.
        public async Task<IActionResult> Index(int? druzynaId, string? szukaj, string? sort, string? kier)
        {
            var query = _context.Zawodnicy.Include(z => z.Druzyna).ThenInclude(d => d!.Turniej).AsQueryable();

            if (!CzyAdmin)
                query = query.Where(z => z.Druzyna!.Zatwierdzona || (UserId != null && z.Druzyna.KapitanId == UserId));
            if (druzynaId != null)
                query = query.Where(z => z.DruzynaId == druzynaId);

            // Po nazwisku można szukać tylko tam, gdzie nazwisko jest widoczne (RODO)
            var zawodnicy = (await query.OrderBy(z => z.Druzyna!.Nazwa).ThenBy(z => z.NumerKoszulki).ToListAsync())
                .Where(z => Wyszukiwanie.KtorekolwiekZawiera(szukaj, z.Imie, z.Druzyna?.Nazwa, z.Pozycja.NazwaWyswietlana(),
                    Maskowanie.PelnyWglad(User, z.Druzyna) ? z.Nazwisko : null));

            ViewBag.DruzynaId = new SelectList(await DostepneDruzyny(false), "IdDruzyny", "Nazwa", druzynaId);
            ViewBag.Szukaj = szukaj;
            return View(Sortowanie.Sortuj(zawodnicy, sort, kier, new()
            {
                ["nr"] = z => z.NumerKoszulki,
                ["zawodnik"] = z => Maskowanie.Zawodnik(User, z),
                ["pozycja"] = z => (int)z.Pozycja,
                ["druzyna"] = z => z.Druzyna?.Nazwa,
                ["turniej"] = z => z.Druzyna?.Turniej?.Nazwa
            }));
        }

        // GET: Zawodnicy/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var zawodnik = await _context.Zawodnicy.Include(z => z.Druzyna).FirstOrDefaultAsync(z => z.IdZawodnika == id);
            if (zawodnik == null) return NotFound();
            if (!zawodnik.Druzyna!.Zatwierdzona && !MozeZarzadzac(zawodnik.Druzyna)) return NotFound();

            ViewBag.MozeZarzadzac = MozeZarzadzac(zawodnik.Druzyna);
            return View(zawodnik);
        }

        // GET: Zawodnicy/Create?druzynaId=1
        [Authorize(Roles = Role.Admin + "," + Role.Kapitan)]
        public async Task<IActionResult> Create(int? druzynaId)
        {
            var druzyny = await DostepneDruzyny(true);
            if (!druzyny.Any())
            {
                Komunikat("Najpierw zgłoś drużynę, potem dodasz do niej zawodników.");
                return RedirectToAction("Create", "Druzyny");
            }
            ViewData["DruzynaId"] = new SelectList(druzyny, "IdDruzyny", "Nazwa", druzynaId);
            return View(new Zawodnik { DruzynaId = druzynaId ?? 0 });
        }

        // POST: Zawodnicy/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin + "," + Role.Kapitan)]
        public async Task<IActionResult> Create([Bind("Imie,Nazwisko,DataUrodzenia,Pozycja,NumerKoszulki,DruzynaId")] Zawodnik zawodnik)
        {
            await SprawdzZawodnika(zawodnik);
            if (!ModelState.IsValid)
            {
                ViewData["DruzynaId"] = new SelectList(await DostepneDruzyny(true), "IdDruzyny", "Nazwa", zawodnik.DruzynaId);
                return View(zawodnik);
            }

            _context.Add(zawodnik);
            await _context.SaveChangesAsync();
            Komunikat($"Dodano zawodnika {zawodnik.Imie} {zawodnik.Nazwisko}.");
            return RedirectToAction("Details", "Druzyny", new { id = zawodnik.DruzynaId });
        }

        // GET: Zawodnicy/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var zawodnik = await _context.Zawodnicy.Include(z => z.Druzyna).FirstOrDefaultAsync(z => z.IdZawodnika == id);
            if (zawodnik == null) return NotFound();
            if (!MozeZarzadzac(zawodnik.Druzyna!)) return Forbid();

            ViewData["DruzynaId"] = new SelectList(await DostepneDruzyny(true), "IdDruzyny", "Nazwa", zawodnik.DruzynaId);
            return View(zawodnik);
        }

        // POST: Zawodnicy/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("IdZawodnika,Imie,Nazwisko,DataUrodzenia,Pozycja,NumerKoszulki,DruzynaId")] Zawodnik zawodnik)
        {
            if (id != zawodnik.IdZawodnika) return NotFound();
            var oryginal = await _context.Zawodnicy.Include(z => z.Druzyna).AsNoTracking().FirstOrDefaultAsync(z => z.IdZawodnika == id);
            if (oryginal == null) return NotFound();
            if (!MozeZarzadzac(oryginal.Druzyna!)) return Forbid();

            await SprawdzZawodnika(zawodnik);
            if (!ModelState.IsValid)
            {
                ViewData["DruzynaId"] = new SelectList(await DostepneDruzyny(true), "IdDruzyny", "Nazwa", zawodnik.DruzynaId);
                return View(zawodnik);
            }

            _context.Update(zawodnik);
            await _context.SaveChangesAsync();
            Komunikat("Zapisano zmiany zawodnika.");
            return RedirectToAction("Details", "Druzyny", new { id = zawodnik.DruzynaId });
        }

        // GET: Zawodnicy/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var zawodnik = await _context.Zawodnicy.Include(z => z.Druzyna).FirstOrDefaultAsync(m => m.IdZawodnika == id);
            if (zawodnik == null) return NotFound();
            if (!MozeZarzadzac(zawodnik.Druzyna!)) return Forbid();
            return View(zawodnik);
        }

        // POST: Zawodnicy/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var zawodnik = await _context.Zawodnicy.Include(z => z.Druzyna).FirstOrDefaultAsync(z => z.IdZawodnika == id);
            if (zawodnik == null) return RedirectToAction(nameof(Index));
            if (!MozeZarzadzac(zawodnik.Druzyna!)) return Forbid();

            _context.Zawodnicy.Remove(zawodnik);
            await _context.SaveChangesAsync();
            Komunikat($"Usunięto zawodnika {zawodnik.Imie} {zawodnik.Nazwisko}.");
            return RedirectToAction("Details", "Druzyny", new { id = zawodnik.DruzynaId });
        }

        // tylkoWlasne = drużyny, którymi użytkownik może zarządzać
        private async Task<List<Druzyna>> DostepneDruzyny(bool tylkoWlasne)
        {
            var query = _context.Druzyny.AsQueryable();
            if (tylkoWlasne && !CzyAdmin)
                query = query.Where(d => d.KapitanId == UserId);
            else if (!CzyAdmin)
                query = query.Where(d => d.Zatwierdzona || (UserId != null && d.KapitanId == UserId));
            return await query.OrderBy(d => d.Nazwa).ToListAsync();
        }

        private async Task SprawdzZawodnika(Zawodnik zawodnik)
        {
            var druzyna = await _context.Druzyny.FindAsync(zawodnik.DruzynaId);
            if (druzyna == null || !MozeZarzadzac(druzyna))
            {
                ModelState.AddModelError(nameof(Zawodnik.DruzynaId), "Wybierz swoją drużynę.");
                return;
            }
            if (await _context.Zawodnicy.AnyAsync(z => z.DruzynaId == zawodnik.DruzynaId
                    && z.NumerKoszulki == zawodnik.NumerKoszulki && z.IdZawodnika != zawodnik.IdZawodnika))
                ModelState.AddModelError(nameof(Zawodnik.NumerKoszulki), "Ten numer jest już zajęty w drużynie.");
            if (zawodnik.DataUrodzenia > DateTime.Today.AddYears(-10))
                ModelState.AddModelError(nameof(Zawodnik.DataUrodzenia), "Zawodnik musi mieć co najmniej 10 lat.");
        }
    }
}
