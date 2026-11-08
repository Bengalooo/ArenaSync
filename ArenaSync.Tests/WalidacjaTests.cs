using System.ComponentModel.DataAnnotations;
using ArenaSync.Models;
using ArenaSync.Services;

namespace ArenaSync.Tests
{
    public class WalidacjaTests
    {
        private static List<ValidationResult> Waliduj(object model)
        {
            var wyniki = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), wyniki, validateAllProperties: true);
            return wyniki;
        }

        [Fact]
        public void Mecz_druzyny_z_sama_soba_jest_niepoprawny()
        {
            var mecz = new Mecz { TurniejId = 1, GospodarzId = 3, GoscId = 3 };

            Assert.Contains(Waliduj(mecz), w => w.MemberNames.Contains(nameof(Mecz.GoscId)));
        }

        [Fact]
        public void Wynik_tylko_jednej_druzyny_jest_niepoprawny()
        {
            var mecz = new Mecz { TurniejId = 1, GospodarzId = 1, GoscId = 2, BramkiGospodarza = 2 };

            Assert.NotEmpty(Waliduj(mecz));
        }

        [Fact]
        public void Numer_koszulki_poza_zakresem_jest_niepoprawny()
        {
            var z = new Zawodnik { Imie = "Jan", Nazwisko = "Kowalski", NumerKoszulki = 100, DruzynaId = 1 };

            Assert.Contains(Waliduj(z), w => w.MemberNames.Contains(nameof(Zawodnik.NumerKoszulki)));
        }

        [Fact]
        public void Niepoprawny_email_druzyny_jest_odrzucany()
        {
            var d = new Druzyna { Nazwa = "Test", Miasto = "Płock", Trener = "Jan", EmailKontaktowy = "brak-maila", TurniejId = 1 };

            Assert.Contains(Waliduj(d), w => w.MemberNames.Contains(nameof(Druzyna.EmailKontaktowy)));
        }

        [Theory]
        [InlineData("Jan", "Kowalski", "Jan K.")]
        [InlineData("Łukasz", "żak", "Łukasz Ż.")]
        public void Maskowanie_pokazuje_imie_i_inicjal_nazwiska(string imie, string nazwisko, string oczekiwane)
        {
            Assert.Equal(oczekiwane, Maskowanie.Nazwisko(imie, nazwisko));
        }
    }
}
