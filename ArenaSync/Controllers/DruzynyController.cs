using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    public class DruzynyController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public DruzynyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Druzyny?turniejId=1
        // Kibic widzi zatwierdzone drużyny, kapitan dodatkowo swoje zgłoszenia, admin wszystko.
        public async Task<IActionResult> Index(int? turniejId, string? szukaj, string? sort, string? kier)
        {
            var query = _context.Druzyny.Include(d => d.Turniej).Include(d => d.Zawodnicy).AsQueryable();

            if (!CzyAdmin)
                query = query.Where(d => d.Zatwierdzona || (UserId != null && d.KapitanId == UserId));
            if (turniejId != null)
                query = query.Where(d => d.TurniejId == turniejId);

            var druzyny = (await query.OrderBy(d => d.Turniej!.Nazwa).ThenBy(d => d.Nazwa).ToListAsync())
                .Where(d => Wyszukiwanie.KtorekolwiekZawiera(szukaj, d.Nazwa, d.Miasto, d.Trener));

            ViewBag.TurniejId = new SelectList(await _context.Turnieje.OrderBy(t => t.Nazwa).ToListAsync(), "IdTurnieju", "Nazwa", turniejId);
            ViewBag.Szukaj = szukaj;
            return View(Sortowanie.Sortuj(druzyny, sort, kier, new()
            {
                ["nazwa"] = d => d.Nazwa,
                ["miasto"] = d => d.Miasto,
                ["turniej"] = d => d.Turniej?.Nazwa,
                ["zawodnicy"] = d => d.Zawodnicy.Count,
                ["status"] = d => d.Zatwierdzona
            }));
        }

        // GET: Druzyny/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var druzyna = await _context.Druzyny.Include(d => d.Turniej).Include(d => d.Zawodnicy)
                .FirstOrDefaultAsync(d => d.IdDruzyny == id);
            if (druzyna == null) return NotFound();
            if (!druzyna.Zatwierdzona && !MozeZarzadzac(druzyna)) return NotFound();

            ViewBag.MozeZarzadzac = MozeZarzadzac(druzyna);
            ViewBag.PelnyWglad = PelnyWglad(druzyna);
            ViewBag.Kapitan = druzyna.KapitanId == null ? null : await _context.Users.FindAsync(druzyna.KapitanId);
            ViewBag.Czlonkowie = PelnyWglad(druzyna)
                ? await _context.Users.Where(u => u.DruzynaId == id).OrderBy(u => u.ImieNazwisko).ToListAsync()
                : new List<ApplicationUser>();
            ViewBag.Mecze = await _context.Mecze.Include(m => m.Gospodarz).Include(m => m.Gosc)
                .Where(m => m.GospodarzId == id || m.GoscId == id).OrderBy(m => m.Termin).ToListAsync();
            return View(druzyna);
        }

        // GET: Druzyny/Create - zgłoszenie drużyny przez kapitana
        [Authorize(Roles = Role.Admin + "," + Role.Kapitan)]
        public async Task<IActionResult> Create(int? turniejId)
        {
            await WypelnijTurnieje(turniejId);
            await WypelnijKapitanow(null);
            return View(new Druzyna { TurniejId = turniejId ?? 0, EmailKontaktowy = CzyAdmin ? "" : User.Identity?.Name ?? "", Zatwierdzona = CzyAdmin });
        }

        // POST: Druzyny/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin + "," + Role.Kapitan)]
        public async Task<IActionResult> Create([Bind("Nazwa,Miasto,Trener,EmailKontaktowy,Telefon,TurniejId,Zatwierdzona,KapitanId")] Druzyna druzyna)
        {
            await SprawdzTurniej(druzyna, null);
            if (!ModelState.IsValid)
            {
                await WypelnijTurnieje(druzyna.TurniejId);
                await WypelnijKapitanow(druzyna.KapitanId);
                return View(druzyna);
            }

            druzyna.DataZgloszenia = DateTime.Today;
            if (!CzyAdmin)
            {
                druzyna.KapitanId = UserId;
                druzyna.Zatwierdzona = false; // kapitan nie zatwierdza sam siebie
            }
            else if (string.IsNullOrEmpty(druzyna.KapitanId))
            {
                druzyna.KapitanId = null;
            }

            _context.Add(druzyna);
            await _context.SaveChangesAsync();
            Komunikat(druzyna.Zatwierdzona
                ? $"Dodano drużynę „{druzyna.Nazwa}”."
                : $"Zgłoszono drużynę „{druzyna.Nazwa}”. Czeka na zatwierdzenie przez administratora.");
            return RedirectToAction(nameof(Details), new { id = druzyna.IdDruzyny });
        }

        // GET: Druzyny/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var druzyna = await _context.Druzyny.FindAsync(id);
            if (druzyna == null) return NotFound();
            if (!MozeZarzadzac(druzyna)) return Forbid();

            await WypelnijTurnieje(druzyna.TurniejId);
            await WypelnijKapitanow(druzyna.KapitanId);
            return View(druzyna);
        }

        // POST: Druzyny/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("IdDruzyny,Nazwa,Miasto,Trener,EmailKontaktowy,Telefon,TurniejId,Zatwierdzona,KapitanId")] Druzyna dane)
        {
            if (id != dane.IdDruzyny) return NotFound();
            var druzyna = await _context.Druzyny.FindAsync(id);
            if (druzyna == null) return NotFound();
            if (!MozeZarzadzac(druzyna)) return Forbid();

            if (!CzyAdmin)
            {
                // Kapitan nie zmienia turnieju ani statusu zatwierdzenia
                dane.TurniejId = druzyna.TurniejId;
                dane.Zatwierdzona = druzyna.Zatwierdzona;
            }

            await SprawdzTurniej(dane, id);
            if (!ModelState.IsValid)
            {
                await WypelnijTurnieje(dane.TurniejId);
                await WypelnijKapitanow(dane.KapitanId);
                return View(dane);
            }

            druzyna.Nazwa = dane.Nazwa;
            druzyna.Miasto = dane.Miasto;
            druzyna.Trener = dane.Trener;
            druzyna.EmailKontaktowy = dane.EmailKontaktowy;
            druzyna.Telefon = dane.Telefon;
            if (CzyAdmin)
            {
                druzyna.TurniejId = dane.TurniejId;
                druzyna.Zatwierdzona = dane.Zatwierdzona;
                druzyna.KapitanId = string.IsNullOrEmpty(dane.KapitanId) ? null : dane.KapitanId;
            }

            await _context.SaveChangesAsync();
            Komunikat("Zapisano zmiany drużyny.");
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Druzyny/Zatwierdz/5 - szybkie zatwierdzenie zgłoszenia
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Zatwierdz(int id, string? returnUrl)
        {
            var druzyna = await _context.Druzyny.FindAsync(id);
            if (druzyna == null) return NotFound();
            druzyna.Zatwierdzona = true;
            await _context.SaveChangesAsync();
            Komunikat($"Zatwierdzono drużynę „{druzyna.Nazwa}”.");
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Index));
        }

        // GET: Druzyny/Delete/5 - usuwanie drużyn tylko przez administratora
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var druzyna = await _context.Druzyny.Include(d => d.Turniej).Include(d => d.Zawodnicy)
                .FirstOrDefaultAsync(m => m.IdDruzyny == id);
            if (druzyna == null) return NotFound();
            ViewBag.LiczbaMeczow = await _context.Mecze.CountAsync(m => m.GospodarzId == id || m.GoscId == id);
            return View(druzyna);
        }

        // POST: Druzyny/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var druzyna = await _context.Druzyny.FindAsync(id);
            if (druzyna == null) return RedirectToAction(nameof(Index));

            // Mecze drużyny usuwamy ręcznie (relacja bez kaskady), zawodnicy usuną się kaskadowo
            _context.Mecze.RemoveRange(_context.Mecze.Where(m => m.GospodarzId == id || m.GoscId == id));
            _context.Druzyny.Remove(druzyna);
            await _context.SaveChangesAsync();
            Komunikat($"Usunięto drużynę „{druzyna.Nazwa}”.");
            return RedirectToAction(nameof(Index));
        }

        // POST: Druzyny/UsunCzlonka/5?userId=... - kapitan lub admin odpina konto zawodnika od drużyny
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Role.Admin + "," + Role.Kapitan)]
        public async Task<IActionResult> UsunCzlonka(int id, string userId)
        {
            var druzyna = await _context.Druzyny.FindAsync(id);
            if (druzyna == null) return NotFound();
            if (!MozeZarzadzac(druzyna)) return Forbid();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DruzynaId == id);
            if (user != null)
            {
                user.DruzynaId = null;
                user.SecurityStamp = Guid.NewGuid().ToString(); // wyloguje go przy najbliższej weryfikacji
                await _context.SaveChangesAsync();
                Komunikat($"Odłączono konto {user.ImieNazwisko ?? user.Email} od drużyny.");
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // Lista kont z rolą Kapitan do przypisania drużynie (tylko admin)
        private async Task WypelnijKapitanow(string? wybrany)
        {
            if (!CzyAdmin) return;
            var rolaId = await _context.Roles.Where(r => r.Name == Role.Kapitan).Select(r => r.Id).FirstOrDefaultAsync();
            var kapitanowie = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == rolaId))
                .OrderBy(u => u.Email).ToListAsync();
            ViewData["KapitanId"] = kapitanowie.Select(u => new SelectListItem($"{u.ImieNazwisko} ({u.Email})", u.Id, u.Id == wybrany));
        }

        private async Task WypelnijTurnieje(int? wybrany)
        {
            var turnieje = await _context.Turnieje
                .Where(t => CzyAdmin || t.Status == StatusTurnieju.ZapisyOtwarte || t.IdTurnieju == wybrany)
                .OrderBy(t => t.DataRozpoczecia).ToListAsync();
            ViewData["TurniejId"] = new SelectList(turnieje, "IdTurnieju", "Nazwa", wybrany);
        }

        // Kapitan może zgłosić się tylko do turnieju z otwartymi zapisami i wolnymi miejscami
        private async Task SprawdzTurniej(Druzyna druzyna, int? idDruzyny)
        {
            var turniej = await _context.Turnieje.Include(t => t.Druzyny).FirstOrDefaultAsync(t => t.IdTurnieju == druzyna.TurniejId);
            if (turniej == null)
            {
                ModelState.AddModelError(nameof(Druzyna.TurniejId), "Wybierz turniej.");
                return;
            }
            if (CzyAdmin) return;

            if (idDruzyny == null && turniej.Status != StatusTurnieju.ZapisyOtwarte)
                ModelState.AddModelError(nameof(Druzyna.TurniejId), "Zapisy do tego turnieju są zamknięte.");
            if (turniej.Druzyny.Count(d => d.IdDruzyny != idDruzyny) >= turniej.MaksLiczbaDruzyn)
                ModelState.AddModelError(nameof(Druzyna.TurniejId), "Brak wolnych miejsc w tym turnieju.");
        }
    }
}
