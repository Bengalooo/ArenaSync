using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    // Zgłaszanie uwag: każdy zalogowany zgłasza i widzi swoje zgłoszenia, administrator odpowiada i zmienia status
    public class UwagiController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public UwagiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Uwagi?status=Nowa&kategoria=Blad
        public async Task<IActionResult> Index(StatusUwagi? status, KategoriaUwagi? kategoria, string? szukaj, string? sort, string? kier)
        {
            var query = _context.Uwagi.Include(u => u.Autor).AsQueryable();
            if (!CzyAdmin) query = query.Where(u => u.AutorId == UserId);
            if (status != null) query = query.Where(u => u.Status == status);
            if (kategoria != null) query = query.Where(u => u.Kategoria == kategoria);

            var uwagi = (await query.OrderByDescending(u => u.DataZgloszenia).ToListAsync())
                .Where(u => Wyszukiwanie.KtorekolwiekZawiera(szukaj, u.Temat, u.Tresc, u.Autor?.ImieNazwisko, u.Autor?.Email));

            ViewBag.Status = status;
            ViewBag.Kategoria = kategoria;
            ViewBag.Szukaj = szukaj;
            return View(Sortowanie.Sortuj(uwagi, sort, kier, new()
            {
                ["data"] = u => u.DataZgloszenia,
                ["temat"] = u => u.Temat,
                ["kategoria"] = u => u.Kategoria.NazwaWyswietlana(),
                ["autor"] = u => u.Autor?.ImieNazwisko ?? u.Autor?.Email,
                ["status"] = u => (int)u.Status
            }));
        }

        // GET: Uwagi/Create
        public IActionResult Create() => View(new Uwaga());

        // POST: Uwagi/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Kategoria,Temat,Tresc")] Uwaga uwaga)
        {
            if (!ModelState.IsValid) return View(uwaga);

            uwaga.AutorId = UserId!;
            uwaga.DataZgloszenia = DateTime.Now;
            uwaga.Status = StatusUwagi.Nowa;
            _context.Uwagi.Add(uwaga);
            await _context.SaveChangesAsync();
            Komunikat("Dziękujemy! Uwaga została wysłana do administratora. Odpowiedź pojawi się w „Moich zgłoszeniach”.");
            return RedirectToAction(nameof(Details), new { id = uwaga.IdUwagi });
        }

        // GET: Uwagi/Details/5 - autor widzi swoje zgłoszenie, administrator wszystkie (z formularzem odpowiedzi)
        public async Task<IActionResult> Details(int id)
        {
            var uwaga = await _context.Uwagi.Include(u => u.Autor).FirstOrDefaultAsync(u => u.IdUwagi == id);
            if (uwaga == null || (!CzyAdmin && uwaga.AutorId != UserId)) return NotFound();
            return View(uwaga);
        }

        // POST: Uwagi/Odpowiedz/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Odpowiedz(int id, StatusUwagi status, string? odpowiedz)
        {
            var uwaga = await _context.Uwagi.Include(u => u.Autor).FirstOrDefaultAsync(u => u.IdUwagi == id);
            if (uwaga == null) return NotFound();

            odpowiedz = string.IsNullOrWhiteSpace(odpowiedz) ? null : odpowiedz.Trim();
            if (odpowiedz?.Length > 2000)
                ModelState.AddModelError("odpowiedz", "Odpowiedź może mieć najwyżej 2000 znaków.");
            if (status == StatusUwagi.Rozwiazana && odpowiedz == null && uwaga.Odpowiedz == null)
                ModelState.AddModelError("odpowiedz", "Przed oznaczeniem jako rozwiązana napisz odpowiedź dla zgłaszającego.");
            if (!ModelState.IsValid) return View(nameof(Details), uwaga);

            if (odpowiedz != null && odpowiedz != uwaga.Odpowiedz)
            {
                uwaga.Odpowiedz = odpowiedz;
                uwaga.DataOdpowiedzi = DateTime.Now;
            }
            uwaga.Status = status;
            await _context.SaveChangesAsync();
            Komunikat($"Zapisano odpowiedź (status: {status.NazwaWyswietlana()}).");
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Uwagi/Delete/5
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var uwaga = await _context.Uwagi.Include(u => u.Autor).FirstOrDefaultAsync(u => u.IdUwagi == id);
            return uwaga == null ? NotFound() : View(uwaga);
        }

        // POST: Uwagi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uwaga = await _context.Uwagi.FindAsync(id);
            if (uwaga != null)
            {
                _context.Uwagi.Remove(uwaga);
                await _context.SaveChangesAsync();
                Komunikat("Usunięto zgłoszenie.");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
