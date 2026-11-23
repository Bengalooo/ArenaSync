using ArenaSync.Models;
using ArenaSync.Services;

namespace ArenaSync.Tests
{
    public class StatystykiTests
    {
        private static readonly List<Druzyna> Druzyny = new()
        {
            new Druzyna { IdDruzyny = 1, Nazwa = "Alfa" },
            new Druzyna { IdDruzyny = 2, Nazwa = "Beta" },
            new Druzyna { IdDruzyny = 3, Nazwa = "Gamma" }
        };

        private static Mecz M(int gosp, int gosc, int? bg, int? bs) =>
            new Mecz { GospodarzId = gosp, GoscId = gosc, BramkiGospodarza = bg, BramkiGoscia = bs,
                       Gospodarz = Druzyny[gosp - 1], Gosc = Druzyny[gosc - 1] };

        [Fact]
        public void Liczy_bramki_srednia_i_wyniki_meczow()
        {
            var mecze = new[] { M(1, 2, 4, 0), M(2, 3, 1, 1), M(3, 1, 2, 1), M(1, 3, null, null) };
            var s = StatystykiService.Oblicz(Druzyny, mecze, Array.Empty<Zawodnik>(), new DateTime(2026, 11, 8));

            Assert.Equal(3, s.MeczeRozegrane);
            Assert.Equal(4, s.MeczeWszystkie);
            Assert.Equal(9, s.Bramki);
            Assert.Equal(3.0, s.SredniaBramek);
            Assert.Equal((2, 1, 0), (s.WygraneGospodarzy, s.Remisy, s.WygraneGosci));
            Assert.Equal(("Alfa", 5), s.NajlepszyAtak[0]);
            Assert.Equal(4, Math.Abs(s.NajwyzszeZwyciestwo!.BramkiGospodarza!.Value - s.NajwyzszeZwyciestwo.BramkiGoscia!.Value));
        }

        [Fact]
        public void Bez_meczow_nie_dzieli_przez_zero()
        {
            var s = StatystykiService.Oblicz(Druzyny, Array.Empty<Mecz>(), Array.Empty<Zawodnik>(), DateTime.Today);

            Assert.Equal(0, s.SredniaBramek);
            Assert.Empty(s.NajlepszyAtak);
            Assert.Null(s.NajwyzszeZwyciestwo);
            Assert.Null(s.SredniWiek);
        }

        [Theory]
        [InlineData("2000-11-08", 26)]
        [InlineData("2000-11-09", 25)]
        public void Wiek_liczony_wedlug_daty_urodzenia(string urodzony, int wiek) =>
            Assert.Equal(wiek, StatystykiService.Wiek(DateTime.Parse(urodzony), new DateTime(2026, 11, 8)));

        [Fact]
        public void Sortowanie_uzywa_polskiej_kolejnosci_liter()
        {
            var nazwy = new[] { "Zawisza", "Łady", "Lechia", "Śniadowo", "Sparta" };
            var wynik = Sortowanie.Sortuj(nazwy, "nazwa", "asc", new() { ["nazwa"] = n => n });
            Assert.Equal(new[] { "Lechia", "Łady", "Sparta", "Śniadowo", "Zawisza" }, wynik);
        }

        [Fact]
        public void Wyszukiwanie_ignoruje_wielkosc_polskich_liter() =>
            Assert.True(Wyszukiwanie.Zawiera("ŁÓDŹ Kaliska", "łódź"));
    }
}
