using System.Diagnostics;
using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Models.ViewModels;
using ArenaSync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Gość widzi stronę powitalną, zalogowany - panel dopasowany do roli
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
                return View("Powitanie");

            var panel = new PanelViewModel
            {
                Rola = CzyAdmin ? Role.Admin : User.IsInRole(Role.Kapitan) ? Role.Kapitan : Role.Zawodnik,
                ImieNazwisko = User.FindFirst(ArenaClaimsFactory.ImieClaim)?.Value ?? User.Identity.Name
            };

            var mecze = await _context.Mecze
                .Include(m => m.Gospodarz).Include(m => m.Gosc).Include(m => m.Turniej)
                .ToListAsync();

            if (CzyAdmin)
            {
                panel.LiczbaTurniejow = await _context.Turnieje.CountAsync();
                panel.LiczbaDruzyn = await _context.Druzyny.CountAsync(d => d.Zatwierdzona);
                panel.LiczbaZawodnikow = await _context.Zawodnicy.CountAsync();
                panel.LiczbaUzytkownikow = await _context.Users.CountAsync();
                panel.UwagiNowe = await _context.Uwagi.CountAsync(u => u.Status == StatusUwagi.Nowa);
                panel.Zgloszenia = await _context.Druzyny.Include(d => d.Turniej).Where(d => !d.Zatwierdzona).ToListAsync();
                panel.DoUzupelnienia = mecze.Where(m => !m.Rozegrany && m.Termin <= DateTime.Now.AddDays(1))
                    .OrderBy(m => m.Termin).Take(8).ToList();
                panel.Nadchodzace = mecze.Where(m => !m.Rozegrany).OrderBy(m => m.Termin).Take(5).ToList();
                panel.OstatnieWyniki = mecze.Where(m => m.Rozegrany).OrderByDescending(m => m.Termin).Take(5).ToList();
                return View("PanelAdmina", panel);
            }

            var druzynaZawodnika = Maskowanie.DruzynaZawodnika(User);
            panel.MojeDruzyny = await _context.Druzyny.Include(d => d.Turniej).Include(d => d.Zawodnicy)
                .Where(d => d.KapitanId == UserId || d.IdDruzyny == druzynaZawodnika)
                .OrderBy(d => d.Nazwa).ToListAsync();

            var ids = panel.MojeDruzyny.Select(d => d.IdDruzyny).ToHashSet();
            var moje = mecze.Where(m => ids.Contains(m.GospodarzId) || ids.Contains(m.GoscId)).ToList();
            panel.Nadchodzace = moje.Where(m => !m.Rozegrany).OrderBy(m => m.Termin).Take(6).ToList();
            panel.NastepnyMecz = panel.Nadchodzace.FirstOrDefault();
            panel.OstatnieWyniki = moje.Where(m => m.Rozegrany).OrderByDescending(m => m.Termin).Take(6).ToList();

            // Tabela turnieju pierwszej (zatwierdzonej) drużyny
            var glowna = panel.MojeDruzyny.FirstOrDefault(d => d.Zatwierdzona) ?? panel.MojeDruzyny.FirstOrDefault();
            if (glowna != null)
            {
                panel.TurniejDruzyny = glowna.Turniej;
                var druzyny = await _context.Druzyny.Where(d => d.TurniejId == glowna.TurniejId && d.Zatwierdzona).ToListAsync();
                panel.Tabela = TabelaService.Oblicz(druzyny, mecze.Where(m => m.TurniejId == glowna.TurniejId));
            }

            return View("PanelDruzyny", panel);
        }

        // Polityka prywatności i plików cookies
        [AllowAnonymous]
        public IActionResult Prywatnosc() => View();

        // Opis ułatwień dostępu (WCAG 2.1)
        [AllowAnonymous]
        public IActionResult Dostepnosc() => View();

        // Mapa serwisu: wszystkie strony z podziałem na role
        [AllowAnonymous]
        public IActionResult Mapa() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
