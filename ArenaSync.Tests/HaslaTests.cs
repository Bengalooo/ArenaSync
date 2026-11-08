using System.Text.RegularExpressions;
using ArenaSync.Data;
using ArenaSync.Services;

namespace ArenaSync.Tests
{
    public class HaslaTests
    {
        // Tak samo jak RegularExpressionAttribute: wzorzec musi pasować do całego hasła
        private static bool Spelnia(string haslo) => Regex.IsMatch(haslo, "^(?:" + Hasla.Wzorzec + ")$");

        [Theory]
        [InlineData("Admin@2026!")]
        [InlineData("Żółć#Gęśla9")]
        [InlineData("abcDEF123$x")]
        public void PoprawneHasla_SpelniajaWymagania(string haslo) => Assert.True(Spelnia(haslo));

        [Theory]
        [InlineData("Ab1!xyz")]        // za krótkie
        [InlineData("admin@2026!")]    // brak wielkiej litery
        [InlineData("ADMIN@2026!")]    // brak małej litery
        [InlineData("Admin@Arena!")]   // brak cyfry
        [InlineData("Admin2026xyz")]   // brak znaku specjalnego
        [InlineData("Admin123!")]      // stare hasło testowe
        public void NiepoprawneHasla_SaOdrzucane(string haslo) => Assert.False(Spelnia(haslo));

        [Fact]
        public void HaslaKontTestowych_SpelniajaWymagania()
        {
            Assert.True(Spelnia(ArenaSeeder.AdminHaslo));
            Assert.True(Spelnia(ArenaSeeder.KapitanHaslo));
            Assert.True(Spelnia(ArenaSeeder.ZawodnikHaslo));
        }

        [Fact]
        public void HasloTymczasowe_ZawszeSpelniaWymagania()
        {
            for (int i = 0; i < 200; i++)
            {
                var haslo = Hasla.Tymczasowe();
                Assert.Equal(12, haslo.Length);
                Assert.True(Spelnia(haslo), haslo);
            }
        }
    }
}
