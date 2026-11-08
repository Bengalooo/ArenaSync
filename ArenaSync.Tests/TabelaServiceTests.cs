using ArenaSync.Models;
using ArenaSync.Services;

namespace ArenaSync.Tests
{
    public class TabelaServiceTests
    {
        private static List<Druzyna> Druzyny() => new()
        {
            new Druzyna { IdDruzyny = 1, Nazwa = "Alfa" },
            new Druzyna { IdDruzyny = 2, Nazwa = "Beta" },
            new Druzyna { IdDruzyny = 3, Nazwa = "Gamma" }
        };

        private static Mecz M(int gosp, int gosc, int? bg, int? bs) =>
            new Mecz { GospodarzId = gosp, GoscId = gosc, BramkiGospodarza = bg, BramkiGoscia = bs };

        [Fact]
        public void Zwyciestwo_daje_3_punkty_a_remis_1()
        {
            var tabela = TabelaService.Oblicz(Druzyny(), new[] { M(1, 2, 2, 0), M(2, 3, 1, 1) });

            Assert.Equal(3, tabela.Single(w => w.DruzynaId == 1).Punkty);
            Assert.Equal(1, tabela.Single(w => w.DruzynaId == 2).Punkty);
            Assert.Equal(1, tabela.Single(w => w.DruzynaId == 3).Punkty);
        }

        [Fact]
        public void Mecze_bez_wyniku_sa_pomijane()
        {
            var tabela = TabelaService.Oblicz(Druzyny(), new[] { M(1, 2, null, null) });

            Assert.All(tabela, w => Assert.Equal(0, w.Mecze));
        }

        [Fact]
        public void Bramki_i_bilans_sa_liczone_dla_obu_druzyn()
        {
            var tabela = TabelaService.Oblicz(Druzyny(), new[] { M(1, 2, 4, 1) });
            var alfa = tabela.Single(w => w.DruzynaId == 1);
            var beta = tabela.Single(w => w.DruzynaId == 2);

            Assert.Equal((4, 1, 3), (alfa.BramkiZdobyte, alfa.BramkiStracone, alfa.Bilans));
            Assert.Equal((1, 4, -3), (beta.BramkiZdobyte, beta.BramkiStracone, beta.Bilans));
            Assert.Equal(1, alfa.Wygrane);
            Assert.Equal(1, beta.Porazki);
        }

        [Fact]
        public void Przy_rownych_punktach_decyduje_bilans_bramek()
        {
            // Alfa i Beta po 3 pkt, Beta ma lepszy bilans
            var tabela = TabelaService.Oblicz(Druzyny(), new[] { M(1, 3, 1, 0), M(2, 3, 5, 0) });

            Assert.Equal(new[] { "Beta", "Alfa", "Gamma" }, tabela.Select(w => w.Druzyna));
        }

        [Fact]
        public void Przy_rownych_punktach_i_bilansie_decyduja_mecze_bezposrednie()
        {
            var druzyny = Druzyny();
            druzyny.Add(new Druzyna { IdDruzyny = 4, Nazwa = "Delta" });
            // Alfa i Beta: po 4 pkt i bilansie +1, Alfa ma więcej bramek, ale Beta wygrała mecz bezpośredni
            var mecze = new[] { M(2, 1, 1, 0), M(1, 3, 3, 1), M(1, 4, 1, 1), M(2, 4, 0, 0) };
            var tabela = TabelaService.Oblicz(druzyny, mecze);

            var alfa = tabela.Single(w => w.DruzynaId == 1);
            var beta = tabela.Single(w => w.DruzynaId == 2);
            Assert.Equal((alfa.Punkty, alfa.Bilans), (beta.Punkty, beta.Bilans));
            Assert.True(alfa.BramkiZdobyte > beta.BramkiZdobyte);
            Assert.True(tabela.IndexOf(beta) < tabela.IndexOf(alfa));
        }
    }
}
