using ArenaSync.Models;
using ArenaSync.Models.ViewModels;

namespace ArenaSync.Services
{
    public static class StatystykiService
    {
        // Liczy statystyki turnieju z zatwierdzonych drużyn, ich zawodników i meczów
        public static StatystykiTurnieju Oblicz(IEnumerable<Druzyna> druzyny, IEnumerable<Mecz> mecze, IEnumerable<Zawodnik> zawodnicy, DateTime dzis)
        {
            var listaDruzyn = druzyny.ToList();
            var wszystkie = mecze.ToList();
            var rozegrane = wszystkie.Where(m => m.Rozegrany).ToList();
            var kadra = zawodnicy.ToList();
            var tabela = TabelaService.Oblicz(listaDruzyn, rozegrane).Where(w => w.Mecze > 0).ToList();

            int bramki = rozegrane.Sum(m => m.BramkiGospodarza!.Value + m.BramkiGoscia!.Value);
            return new StatystykiTurnieju
            {
                Druzyny = listaDruzyn.Count,
                Zawodnicy = kadra.Count,
                MeczeRozegrane = rozegrane.Count,
                MeczeWszystkie = wszystkie.Count,
                Bramki = bramki,
                SredniaBramek = rozegrane.Count == 0 ? 0 : Math.Round((double)bramki / rozegrane.Count, 2),
                Remisy = rozegrane.Count(m => m.BramkiGospodarza == m.BramkiGoscia),
                WygraneGospodarzy = rozegrane.Count(m => m.BramkiGospodarza > m.BramkiGoscia),
                WygraneGosci = rozegrane.Count(m => m.BramkiGospodarza < m.BramkiGoscia),
                SredniWiek = kadra.Count == 0 ? null : Math.Round(kadra.Average(z => Wiek(z.DataUrodzenia, dzis)), 1),
                NajlepszyAtak = Top(tabela.OrderByDescending(w => w.BramkiZdobyte).ThenBy(w => w.Druzyna), w => w.BramkiZdobyte),
                NajlepszaObrona = Top(tabela.OrderBy(w => w.BramkiStracone).ThenBy(w => w.Druzyna), w => w.BramkiStracone),
                NajwiecejZwyciestw = Top(tabela.Where(w => w.Wygrane > 0).OrderByDescending(w => w.Wygrane).ThenBy(w => w.Druzyna), w => w.Wygrane),
                NajwyzszeZwyciestwo = rozegrane.Where(m => m.BramkiGospodarza != m.BramkiGoscia)
                    .OrderByDescending(m => Math.Abs(m.BramkiGospodarza!.Value - m.BramkiGoscia!.Value)).ThenBy(m => m.Termin).FirstOrDefault(),
                NajwiecejBramek = rozegrane.OrderByDescending(m => m.BramkiGospodarza + m.BramkiGoscia).ThenBy(m => m.Termin).FirstOrDefault(),
                ZawodnicyWgPozycji = Enum.GetValues<Pozycja>()
                    .Select(p => (p.NazwaWyswietlana(), kadra.Count(z => z.Pozycja == p))).ToList()
            };
        }

        public static int Wiek(DateTime urodzony, DateTime dzis)
        {
            int wiek = dzis.Year - urodzony.Year;
            if (urodzony.Date > dzis.AddYears(-wiek)) wiek--;
            return wiek;
        }

        private static List<(string, int)> Top(IEnumerable<WierszTabeli> wiersze, Func<WierszTabeli, int> wartosc) =>
            wiersze.Take(3).Select(w => (w.Druzyna, wartosc(w))).ToList();
    }
}
