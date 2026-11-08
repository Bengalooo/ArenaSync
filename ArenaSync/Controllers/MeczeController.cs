using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    public class MeczeController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public MeczeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Mecze?turniejId=1 - terminarz i wyniki
        public async Task<IActionResult> Index(int? turniejId, string? szukaj, string? status, string? sort, string? kier)
        {
            var query = _context.Mecze.Include(m => m.Turniej).Include(m => m.Gospodarz).Include(m => m.Gosc).AsQueryable();
            if (turniejId != null) query = query.Where(m => m.TurniejId == turniejId);
            if (status == "rozegrane") query = query.Where(m => m.BramkiGospodarza != null);
            if (status == "nadchodzace") query = query.Where(m => m.BramkiGospodarza == null);

            var mecze = (await query.OrderBy(m => m.Termin).ToListAsync())
                .Where(m => Wyszukiwanie.KtorekolwiekZawiera(szukaj, m.Gospodarz?.Nazwa, m.Gosc?.Nazwa, m.Boisko));

            ViewBag.TurniejId = new SelectList(await _context.Turnieje.OrderBy(t => t.Nazwa).ToListAsync(), "IdTurnieju", "Nazwa", turniejId);
            ViewBag.Szukaj = szukaj;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Kier = kier;
            return View(Sortowanie.Sortuj(mecze, sort, kier, new()
            {
                ["termin"] = m => m.Termin,
                ["turniej"] = m => m.Turniej?.Nazwa,
                ["gospodarz"] = m => m.Gospodarz?.Nazwa,
                ["bramki"] = m => (m.BramkiGospodarza ?? -1) + (m.BramkiGoscia ?? 0)
            }));
        }

        // GET: Mecze/Create
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Create(int? turniejId)
        {
            var turniej = turniejId == null ? null : await _context.Turnieje.FindAsync(turniejId);
            var mecz = new Mecz { TurniejId = turniejId ?? 0 };
            if (turniej != null) mecz.Termin = turniej.DataRozpoczecia.AddHours(12);
            await WypelnijListy(mecz);
            return View(mecz);
        }

        // POST: Mecze/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Create([Bind("TurniejId,GospodarzId,GoscId,Termin,Boisko,BramkiGospodarza,BramkiGoscia")] Mecz mecz)
        {
            await SprawdzDruzyny(mecz);
            if (!ModelState.IsValid)
            {
                await WypelnijListy(mecz);
                return View(mecz);
            }

            _context.Add(mecz);
            await _context.SaveChangesAsync();
            Komunikat("Dodano mecz.");
            return RedirectToAction("Details", "Turnieje", new { id = mecz.TurniejId });
        }

        // GET: Mecze/Edit/5
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var mecz = await _context.Mecze.FindAsync(id);
            if (mecz == null) return NotFound();
            await WypelnijListy(mecz);
            return View(mecz);
        }

        // POST: Mecze/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Edit(int id, [Bind("IdMeczu,TurniejId,GospodarzId,GoscId,Termin,Boisko,BramkiGospodarza,BramkiGoscia")] Mecz mecz)
        {
            if (id != mecz.IdMeczu) return NotFound();
            await SprawdzDruzyny(mecz);
            if (!ModelState.IsValid)
            {
                await WypelnijListy(mecz);
                return View(mecz);
            }

            _context.Update(mecz);
            await _context.SaveChangesAsync();
            Komunikat("Zapisano mecz.");
            return RedirectToAction("Details", "Turnieje", new { id = mecz.TurniejId });
        }

        // GET: Mecze/Wynik/5 - szybkie wpisanie wyniku (np. z telefonu na boisku)
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Wynik(int? id)
        {
            if (id == null) return NotFound();
            var mecz = await _context.Mecze.Include(m => m.Gospodarz).Include(m => m.Gosc).Include(m => m.Turniej)
                .FirstOrDefaultAsync(m => m.IdMeczu == id);
            if (mecz == null) return NotFound();
            return View(mecz);
        }

        // POST: Mecze/Wynik/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Wynik(int id, int? bramkiGospodarza, int? bramkiGoscia)
        {
            var mecz = await _context.Mecze.Include(m => m.Gospodarz).Include(m => m.Gosc).Include(m => m.Turniej)
                .FirstOrDefaultAsync(m => m.IdMeczu == id);
            if (mecz == null) return NotFound();

            if (bramkiGospodarza is < 0 or > 99 || bramkiGoscia is < 0 or > 99 || bramkiGospodarza.HasValue != bramkiGoscia.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Podaj poprawny wynik obu drużyn (0–99) albo zostaw oba pola puste.");
                return View(mecz);
            }

            mecz.BramkiGospodarza = bramkiGospodarza;
            mecz.BramkiGoscia = bramkiGoscia;
            await _context.SaveChangesAsync();
            Komunikat($"Zapisano wynik: {mecz.Gospodarz!.Nazwa} {bramkiGospodarza}:{bramkiGoscia} {mecz.Gosc!.Nazwa}.");
            return RedirectToAction("Details", "Turnieje", new { id = mecz.TurniejId });
        }

        // GET: Mecze/Delete/5
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var mecz = await _context.Mecze.Include(m => m.Turniej).Include(m => m.Gospodarz).Include(m => m.Gosc)
                .FirstOrDefaultAsync(m => m.IdMeczu == id);
            if (mecz == null) return NotFound();
            return View(mecz);
        }

        // POST: Mecze/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mecz = await _context.Mecze.FindAsync(id);
            if (mecz == null) return RedirectToAction(nameof(Index));
            _context.Mecze.Remove(mecz);
            await _context.SaveChangesAsync();
            Komunikat("Usunięto mecz.");
            return RedirectToAction("Details", "Turnieje", new { id = mecz.TurniejId });
        }

        private async Task WypelnijListy(Mecz mecz)
        {
            ViewData["TurniejId"] = new SelectList(await _context.Turnieje.OrderBy(t => t.Nazwa).ToListAsync(), "IdTurnieju", "Nazwa", mecz.TurniejId);
            var druzyny = await _context.Druzyny.Include(d => d.Turniej).Where(d => d.Zatwierdzona)
                .OrderBy(d => d.Turniej!.Nazwa).ThenBy(d => d.Nazwa).ToListAsync();
            // jedna grupa <optgroup> na turniej
            var grupy = druzyny.Select(d => d.Turniej!.Nazwa).Distinct()
                .ToDictionary(n => n, n => new SelectListGroup { Name = n });
            var pozycje = druzyny.Select(d => new SelectListItem(d.Nazwa, d.IdDruzyny.ToString()) { Group = grupy[d.Turniej!.Nazwa] }).ToList();

            ViewData["Gospodarze"] = pozycje.Select(p => new SelectListItem(p.Text, p.Value, p.Value == mecz.GospodarzId.ToString()) { Group = p.Group }).ToList();
            ViewData["Goscie"] = pozycje.Select(p => new SelectListItem(p.Text, p.Value, p.Value == mecz.GoscId.ToString()) { Group = p.Group }).ToList();
        }

        private async Task SprawdzDruzyny(Mecz mecz)
        {
            var ids = new[] { mecz.GospodarzId, mecz.GoscId };
            var druzyny = await _context.Druzyny.Where(d => ids.Contains(d.IdDruzyny)).ToListAsync();
            if (druzyny.Any(d => d.TurniejId != mecz.TurniejId))
                ModelState.AddModelError(nameof(Mecz.GospodarzId), "Obie drużyny muszą należeć do wybranego turnieju.");
        }
    }
}
