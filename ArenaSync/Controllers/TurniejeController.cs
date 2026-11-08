using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    public class TurniejeController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public TurniejeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Turnieje
        public async Task<IActionResult> Index(string? szukaj, StatusTurnieju? status, string? sort, string? kier)
        {
            var query = _context.Turnieje.Include(t => t.Druzyny).AsQueryable();
            if (status != null) query = query.Where(t => t.Status == status);

            var turnieje = (await query.OrderBy(t => t.DataRozpoczecia).ToListAsync())
                .Where(t => Wyszukiwanie.KtorekolwiekZawiera(szukaj, t.Nazwa, t.Lokalizacja, t.Dyscyplina));

            ViewBag.Szukaj = szukaj;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Kier = kier;
            return View(Sortowanie.Sortuj(turnieje, sort, kier, new()
            {
                ["data"] = t => t.DataRozpoczecia,
                ["nazwa"] = t => t.Nazwa,
                ["druzyny"] = t => t.Druzyny.Count(d => d.Zatwierdzona)
            }));
        }

        // GET: Turnieje/Details/5 - tabela, terminarz i drużyny
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var turniej = await _context.Turnieje
                .Include(t => t.Druzyny)
                .Include(t => t.Mecze).ThenInclude(m => m.Gospodarz)
                .Include(t => t.Mecze).ThenInclude(m => m.Gosc)
                .FirstOrDefaultAsync(t => t.IdTurnieju == id);
            if (turniej == null) return NotFound();

            var zatwierdzone = turniej.Druzyny.Where(d => d.Zatwierdzona).ToList();
            ViewBag.Tabela = TabelaService.Oblicz(zatwierdzone, turniej.Mecze);
            return View(turniej);
        }

        // GET: Turnieje/Create
        [Authorize(Roles = Role.Admin)]
        public IActionResult Create()
        {
            return View(new Turniej());
        }

        // POST: Turnieje/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Create([Bind("Nazwa,Dyscyplina,Lokalizacja,DataRozpoczecia,DataZakonczenia,MaksLiczbaDruzyn,Opis,Status")] Turniej turniej)
        {
            SprawdzDaty(turniej);
            if (!ModelState.IsValid) return View(turniej);

            _context.Add(turniej);
            await _context.SaveChangesAsync();
            Komunikat($"Dodano turniej „{turniej.Nazwa}”.");
            return RedirectToAction(nameof(Index));
        }

        // GET: Turnieje/Edit/5
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var turniej = await _context.Turnieje.FindAsync(id);
            if (turniej == null) return NotFound();
            return View(turniej);
        }

        // POST: Turnieje/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Edit(int id, [Bind("IdTurnieju,Nazwa,Dyscyplina,Lokalizacja,DataRozpoczecia,DataZakonczenia,MaksLiczbaDruzyn,Opis,Status")] Turniej turniej)
        {
            if (id != turniej.IdTurnieju) return NotFound();
            SprawdzDaty(turniej);
            if (!ModelState.IsValid) return View(turniej);

            try
            {
                _context.Update(turniej);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Turnieje.Any(e => e.IdTurnieju == id)) return NotFound();
                throw;
            }
            Komunikat("Zapisano zmiany turnieju.");
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Turnieje/Delete/5
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var turniej = await _context.Turnieje.Include(t => t.Druzyny).Include(t => t.Mecze)
                .FirstOrDefaultAsync(m => m.IdTurnieju == id);
            if (turniej == null) return NotFound();
            return View(turniej);
        }

        // POST: Turnieje/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var turniej = await _context.Turnieje.Include(t => t.Mecze).FirstOrDefaultAsync(t => t.IdTurnieju == id);
            if (turniej != null)
            {
                // Mecze najpierw (relacje do drużyn są bez kaskady)
                _context.Mecze.RemoveRange(turniej.Mecze);
                _context.Turnieje.Remove(turniej);
                await _context.SaveChangesAsync();
                Komunikat($"Usunięto turniej „{turniej.Nazwa}”.");
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Turnieje/GenerujTerminarz/5 - automatyczny terminarz "każdy z każdym"
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> GenerujTerminarz(int id, DateTime start, int odstepMinut = 45, string? boisko = null)
        {
            var turniej = await _context.Turnieje.Include(t => t.Druzyny).Include(t => t.Mecze)
                .FirstOrDefaultAsync(t => t.IdTurnieju == id);
            if (turniej == null) return NotFound();

            var ids = turniej.Druzyny.Where(d => d.Zatwierdzona).OrderBy(d => d.IdDruzyny).Select(d => d.IdDruzyny).ToList();
            if (ids.Count < 2)
            {
                Komunikat("Do wygenerowania terminarza potrzebne są co najmniej 2 zatwierdzone drużyny.");
                return RedirectToAction(nameof(Details), new { id });
            }

            // Nowy terminarz zastępuje mecze bez wyniku, rozegrane zostają
            _context.Mecze.RemoveRange(turniej.Mecze.Where(m => !m.Rozegrany));
            if (start == default) start = turniej.DataRozpoczecia.AddHours(10);

            var nowe = TerminarzGenerator.Generuj(id, ids, start, 0, Math.Clamp(odstepMinut, 10, 240), boisko);
            var rozegrane = turniej.Mecze.Where(m => m.Rozegrany)
                .Select(m => (Math.Min(m.GospodarzId, m.GoscId), Math.Max(m.GospodarzId, m.GoscId))).ToHashSet();
            nowe = nowe.Where(m => !rozegrane.Contains((Math.Min(m.GospodarzId, m.GoscId), Math.Max(m.GospodarzId, m.GoscId)))).ToList();

            _context.Mecze.AddRange(nowe);
            await _context.SaveChangesAsync();
            Komunikat($"Wygenerowano terminarz (nowe mecze: {nowe.Count}).");
            return RedirectToAction(nameof(Details), new { id });
        }

        private void SprawdzDaty(Turniej turniej)
        {
            if (turniej.DataZakonczenia < turniej.DataRozpoczecia)
                ModelState.AddModelError(nameof(Turniej.DataZakonczenia), "Data zakończenia nie może być wcześniejsza niż data rozpoczęcia.");
        }
    }
}
