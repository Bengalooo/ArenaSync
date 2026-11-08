using System.Security.Claims;
using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;

namespace ArenaSync.Tests
{
    public class UprawnieniaTests
    {
        private static readonly Druzyna Druzyna = new() { IdDruzyny = 1, Nazwa = "Alfa", KapitanId = "kap-1" };
        private static readonly Zawodnik Gracz = new() { Imie = "Jan", Nazwisko = "Kowalski", Druzyna = Druzyna, DruzynaId = 1 };

        private static ClaimsPrincipal Uzytkownik(string id, string rola, int? druzynaId = null)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Role, rola) };
            if (druzynaId != null) claims.Add(new Claim(ArenaClaimsFactory.DruzynaClaim, druzynaId.Value.ToString()));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        [Fact]
        public void Admin_widzi_pelne_dane_i_zarzadza_kazda_druzyna()
        {
            var admin = Uzytkownik("adm", Role.Admin);
            Assert.True(Maskowanie.PelnyWglad(admin, Druzyna));
            Assert.True(Maskowanie.MozeZarzadzac(admin, Druzyna));
        }

        [Fact]
        public void Kapitan_zarzadza_tylko_swoja_druzyna()
        {
            Assert.True(Maskowanie.MozeZarzadzac(Uzytkownik("kap-1", Role.Kapitan), Druzyna));
            Assert.False(Maskowanie.MozeZarzadzac(Uzytkownik("kap-2", Role.Kapitan), Druzyna));
        }

        [Fact]
        public void Zawodnik_widzi_pelne_dane_swojej_druzyny_ale_nie_zarzadza()
        {
            var zawodnik = Uzytkownik("z-1", Role.Zawodnik, druzynaId: 1);
            Assert.True(Maskowanie.PelnyWglad(zawodnik, Druzyna));
            Assert.False(Maskowanie.MozeZarzadzac(zawodnik, Druzyna));
            Assert.Equal("Jan Kowalski", Maskowanie.Zawodnik(zawodnik, Gracz));
        }

        [Fact]
        public void Zawodnik_innej_druzyny_widzi_zamaskowane_nazwisko()
        {
            var obcy = Uzytkownik("z-2", Role.Zawodnik, druzynaId: 2);
            Assert.False(Maskowanie.PelnyWglad(obcy, Druzyna));
            Assert.Equal("Jan K.", Maskowanie.Zawodnik(obcy, Gracz));
        }
    }
}
