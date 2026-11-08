using ArenaSync.Services;

namespace ArenaSync.Tests
{
    public class TerminarzGeneratorTests
    {
        private static readonly DateTime Start = new(2026, 11, 7, 10, 0, 0);

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(8)]
        public void Kazda_para_gra_dokladnie_raz(int liczbaDruzyn)
        {
            var ids = Enumerable.Range(1, liczbaDruzyn).ToList();

            var mecze = TerminarzGenerator.Generuj(1, ids, Start);

            Assert.Equal(liczbaDruzyn * (liczbaDruzyn - 1) / 2, mecze.Count);
            var pary = mecze.Select(m => (Math.Min(m.GospodarzId, m.GoscId), Math.Max(m.GospodarzId, m.GoscId))).ToList();
            Assert.Equal(pary.Count, pary.Distinct().Count());
            Assert.All(mecze, m => Assert.NotEqual(m.GospodarzId, m.GoscId));
        }

        [Fact]
        public void Druzyna_nie_gra_dwoch_meczow_w_tym_samym_czasie()
        {
            var mecze = TerminarzGenerator.Generuj(1, Enumerable.Range(1, 6).ToList(), Start, odstepDni: 0, odstepMinut: 30);

            foreach (var grupa in mecze.GroupBy(m => m.Termin))
            {
                var druzyny = grupa.SelectMany(m => new[] { m.GospodarzId, m.GoscId }).ToList();
                Assert.Equal(druzyny.Count, druzyny.Distinct().Count());
            }
        }

        [Fact]
        public void Mniej_niz_dwie_druzyny_daje_pusty_terminarz()
        {
            Assert.Empty(TerminarzGenerator.Generuj(1, new List<int> { 1 }, Start));
        }

        [Fact]
        public void Mecze_maja_przypisany_turniej_i_boisko()
        {
            var mecze = TerminarzGenerator.Generuj(7, new List<int> { 1, 2, 3 }, Start, boisko: "Boisko B");

            Assert.All(mecze, m => Assert.Equal(7, m.TurniejId));
            Assert.All(mecze, m => Assert.Equal("Boisko B", m.Boisko));
        }
    }
}
