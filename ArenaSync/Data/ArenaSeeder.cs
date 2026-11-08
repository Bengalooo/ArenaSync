using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArenaSync.Data
{
    // Wypełnia bazę przykładowymi danymi przy pierwszym uruchomieniu.
    public class ArenaSeeder
    {
        public const string AdminEmail = "admin@arenasync.pl";
        public const string AdminHaslo = "Admin@2026!";
        public const string KapitanEmail = "kapitan@arenasync.pl";
        public const string KapitanHaslo = "Kapitan@2026!";
        public const string ZawodnikEmail = "zawodnik@arenasync.pl";
        public const string ZawodnikHaslo = "Zawodnik@2026!";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ArenaSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            _context.Database.Migrate();

            await SeedRole();
            var kapitan = await SeedUzytkownicy();
            SeedTurnieje();
            SeedDruzyny(kapitan.Id);
            SeedZawodnicy();
            SeedMecze();
            await SeedKontoZawodnika();
            await UsunStareKonta();
            await ZmienStareHasla();
            await UstawBlokady();
        }

        // Konto "organizator@arenasync.pl" z pierwszej wersji aplikacji (rola Organizator) zastąpiło admin@arenasync.pl
        private async Task UsunStareKonta()
        {
            var stare = await _userManager.FindByEmailAsync("organizator@arenasync.pl");
            if (stare == null || await _userManager.FindByEmailAsync(AdminEmail) == null) return;

            foreach (var d in _context.Druzyny.Where(d => d.KapitanId == stare.Id))
                d.KapitanId = null;
            await _context.SaveChangesAsync();
            await _userManager.DeleteAsync(stare);
        }

        // Baza z poprzedniej wersji: konta testowe ze starymi hasłami (niespełniającymi nowych wymagań) dostają nowe hasła
        private async Task ZmienStareHasla()
        {
            var stare = new[]
            {
                (AdminEmail, "Admin123!", AdminHaslo),
                (KapitanEmail, "Kapitan123!", KapitanHaslo),
                (ZawodnikEmail, "Zawodnik123!", ZawodnikHaslo)
            };
            foreach (var (email, stareHaslo, noweHaslo) in stare)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || !await _userManager.CheckPasswordAsync(user, stareHaslo)) continue;
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, noweHaslo);
            }
        }

        // Blokada po 3 błędnych hasłach dotyczy kapitanów i zawodników, administratorów nie
        private async Task UstawBlokady()
        {
            foreach (var user in _userManager.Users.ToList())
            {
                bool blokowalne = !await _userManager.IsInRoleAsync(user, Role.Admin);
                if (user.LockoutEnabled != blokowalne)
                    await _userManager.SetLockoutEnabledAsync(user, blokowalne);
            }
        }

        private async Task SeedRole()
        {
            // Baza z poprzedniej wersji: rola "Organizator" zostaje przemianowana na "Admin"
            var stara = await _roleManager.FindByNameAsync("Organizator");
            if (stara != null && !await _roleManager.RoleExistsAsync(Role.Admin))
            {
                stara.Name = Role.Admin;
                await _roleManager.UpdateAsync(stara);
            }

            foreach (var rola in Role.Wszystkie)
                if (!await _roleManager.RoleExistsAsync(rola))
                    await _roleManager.CreateAsync(new IdentityRole(rola));
        }

        private async Task<ApplicationUser> SeedUzytkownicy()
        {
            await UtworzUzytkownika(AdminEmail, AdminHaslo, Role.Admin, "Administrator ArenaSync");
            return await UtworzUzytkownika(KapitanEmail, KapitanHaslo, Role.Kapitan, "Marek Nowak");
        }

        private async Task<ApplicationUser> UtworzUzytkownika(string email, string haslo, string rola, string imieNazwisko, int? druzynaId = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, ImieNazwisko = imieNazwisko, DruzynaId = druzynaId };
                await _userManager.CreateAsync(user, haslo);
                await _userManager.AddToRoleAsync(user, rola);
            }
            else if (string.IsNullOrEmpty(user.ImieNazwisko))
            {
                // konto z poprzedniej wersji bazy - uzupełniamy imię i nazwisko
                user.ImieNazwisko = imieNazwisko;
                await _userManager.UpdateAsync(user);
            }
            return user;
        }

        // Konto zawodnika przypisane do drużyny kapitana testowego
        private async Task SeedKontoZawodnika()
        {
            var druzyna = _context.Druzyny.OrderBy(d => d.IdDruzyny).First(d => d.Zatwierdzona);
            await UtworzUzytkownika(ZawodnikEmail, ZawodnikHaslo, Role.Zawodnik, "Krzysztof Dąbrowski", druzyna.IdDruzyny);
        }

        private void SeedTurnieje()
        {
            if (_context.Turnieje.Any()) return;

            _context.Turnieje.AddRange(
                new Turniej
                {
                    Nazwa = "Orlik Cup Płock 2026",
                    Dyscyplina = "Piłka nożna 6-osobowa",
                    Lokalizacja = "Orlik, ul. Kobylińskiego, Płock",
                    DataRozpoczecia = new DateTime(2026, 11, 7),
                    DataZakonczenia = new DateTime(2026, 11, 8),
                    MaksLiczbaDruzyn = 8,
                    Opis = "Jesienny turniej amatorskich drużyn z Płocka i okolic. System każdy z każdym.",
                    Status = StatusTurnieju.WTrakcie
                },
                new Turniej
                {
                    Nazwa = "Liga Zimowa Mazowsze",
                    Dyscyplina = "Futsal",
                    Lokalizacja = "Hala sportowa Orlen Arena, Płock",
                    DataRozpoczecia = new DateTime(2026, 12, 5),
                    DataZakonczenia = new DateTime(2027, 2, 20),
                    MaksLiczbaDruzyn = 10,
                    Opis = "Halowa liga futsalu rozgrywana w weekendy.",
                    Status = StatusTurnieju.ZapisyOtwarte
                });
            _context.SaveChanges();
        }

        private void SeedDruzyny(string kapitanId)
        {
            if (_context.Druzyny.Any()) return;

            var orlik = _context.Turnieje.First(t => t.Nazwa.StartsWith("Orlik"));
            var liga = _context.Turnieje.First(t => t.Nazwa.StartsWith("Liga"));

            _context.Druzyny.AddRange(
                new Druzyna { Nazwa = "FC Wisła Płock Amatorzy", Miasto = "Płock", Trener = "Marek Nowak", EmailKontaktowy = "kapitan@arenasync.pl", Telefon = "600100200", Zatwierdzona = true, TurniejId = orlik.IdTurnieju, KapitanId = kapitanId },
                new Druzyna { Nazwa = "Orły Radziwie", Miasto = "Płock", Trener = "Piotr Zieliński", EmailKontaktowy = "orly@example.com", Telefon = "600200300", Zatwierdzona = true, TurniejId = orlik.IdTurnieju },
                new Druzyna { Nazwa = "Sparta Gostynin", Miasto = "Gostynin", Trener = "Tomasz Wójcik", EmailKontaktowy = "sparta@example.com", Zatwierdzona = true, TurniejId = orlik.IdTurnieju },
                new Druzyna { Nazwa = "Mazovia Team", Miasto = "Sierpc", Trener = "Adam Kamiński", EmailKontaktowy = "mazovia@example.com", Zatwierdzona = true, TurniejId = orlik.IdTurnieju },
                new Druzyna { Nazwa = "Futsal Kings", Miasto = "Płock", Trener = "Kamil Lewandowski", EmailKontaktowy = "kings@example.com", Zatwierdzona = false, TurniejId = liga.IdTurnieju });
            _context.SaveChanges();
        }

        private void SeedZawodnicy()
        {
            if (_context.Zawodnicy.Any()) return;

            var imiona = new[] { "Jan", "Krzysztof", "Michał", "Paweł", "Łukasz", "Jakub", "Mateusz", "Bartosz" };
            var nazwiska = new[] { "Kowalski", "Wiśniewski", "Dąbrowski", "Lewandowski", "Zając", "Szymański", "Woźniak", "Kozłowski" };
            var rnd = new Random(2026);

            foreach (var d in _context.Druzyny.ToList())
            {
                for (int i = 0; i < 6; i++)
                {
                    _context.Zawodnicy.Add(new Zawodnik
                    {
                        Imie = imiona[rnd.Next(imiona.Length)],
                        Nazwisko = nazwiska[rnd.Next(nazwiska.Length)],
                        DataUrodzenia = new DateTime(1990 + rnd.Next(15), rnd.Next(1, 13), rnd.Next(1, 28)),
                        Pozycja = i == 0 ? Pozycja.Bramkarz : (Pozycja)(1 + (i % 3)),
                        NumerKoszulki = i == 0 ? 1 : i * 2 + 3,
                        DruzynaId = d.IdDruzyny
                    });
                }
            }
            _context.SaveChanges();
        }

        private void SeedMecze()
        {
            if (_context.Mecze.Any()) return;

            var orlik = _context.Turnieje.First(t => t.Nazwa.StartsWith("Orlik"));
            var ids = _context.Druzyny.Where(d => d.TurniejId == orlik.IdTurnieju && d.Zatwierdzona)
                .OrderBy(d => d.IdDruzyny).Select(d => d.IdDruzyny).ToList();

            var mecze = TerminarzGenerator.Generuj(orlik.IdTurnieju, ids, orlik.DataRozpoczecia.AddHours(10), 0, 45, "Boisko A");

            // Kilka pierwszych meczów ma już wynik, żeby tabela nie była pusta
            var wyniki = new (int, int)[] { (3, 1), (2, 2), (0, 1), (4, 2) };
            for (int i = 0; i < wyniki.Length && i < mecze.Count; i++)
            {
                mecze[i].BramkiGospodarza = wyniki[i].Item1;
                mecze[i].BramkiGoscia = wyniki[i].Item2;
            }

            _context.Mecze.AddRange(mecze);
            _context.SaveChanges();
        }
    }
}
