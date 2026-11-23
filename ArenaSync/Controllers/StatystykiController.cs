using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Models.ViewModels;
using ArenaSync.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    // Statystyki turniejów dla wszystkich zalogowanych; administrator widzi też dane o kontach i zgłoszeniach
    public class StatystykiController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public StatystykiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Statystyki?turniejId=1
        public async Task<IActionResult> Index(int? turniejId)
        {
            var turnieje = await _context.Turnieje.OrderByDescending(t => t.DataRozpoczecia).ToListAsync();
            // Domyślnie turniej w trakcie, potem ten z rozegranymi meczami, na końcu najnowszy
            var zMeczami = await _context.Mecze.Where(m => m.BramkiGospodarza != null).Select(m => m.TurniejId).Distinct().ToListAsync();
            var turniej = turnieje.FirstOrDefault(t => t.IdTurnieju == turniejId)
                ?? turnieje.FirstOrDefault(t => t.Status == StatusTurnieju.WTrakcie)
                ?? turnieje.FirstOrDefault(t => zMeczami.Contains(t.IdTurnieju))
                ?? turnieje.FirstOrDefault();

            var model = new StatystykiViewModel
            {
                TurniejId = turniej?.IdTurnieju,
                NazwaTurnieju = turniej?.Nazwa ?? "",
                Turnieje = new SelectList(turnieje, "IdTurnieju", "Nazwa", turniej?.IdTurnieju)
            };

            if (turniej != null)
            {
                var druzyny = await _context.Druzyny.Where(d => d.TurniejId == turniej.IdTurnieju && d.Zatwierdzona).ToListAsync();
                var ids = druzyny.Select(d => d.IdDruzyny).ToList();
                var mecze = await _context.Mecze.Include(m => m.Gospodarz).Include(m => m.Gosc)
                    .Where(m => m.TurniejId == turniej.IdTurnieju).ToListAsync();
                var zawodnicy = await _context.Zawodnicy.Where(z => ids.Contains(z.DruzynaId)).ToListAsync();
                model.Turniej = StatystykiService.Oblicz(druzyny, mecze, zawodnicy, DateTime.Today);
            }

            if (CzyAdmin)
            {
                var role = await (from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  group ur by r.Name into g
                                  select new { Rola = g.Key, Liczba = g.Count() }).ToListAsync();
                model.KontaWgRoli = Role.Wszystkie
                    .Select(r => (Role.Nazwa(r), role.FirstOrDefault(x => x.Rola == r)?.Liczba ?? 0)).ToList();
                var teraz = DateTimeOffset.UtcNow;
                model.KontaZablokowane = (await _context.Users.Where(u => u.LockoutEnabled && u.LockoutEnd != null).ToListAsync())
                    .Count(u => u.LockoutEnd > teraz);
                model.DruzynyOczekujace = await _context.Druzyny.CountAsync(d => !d.Zatwierdzona);
                model.UwagiNowe = await _context.Uwagi.CountAsync(u => u.Status == StatusUwagi.Nowa);
            }
            return View(model);
        }
    }
}
