using ArenaSync.Models;
using ArenaSync.Models.ViewModels;

namespace ArenaSync.Services
{
    public static class TabelaService
    {
        // Liczy tabelę: 3 pkt za zwycięstwo, 1 za remis.
        // Kolejność: punkty, bilans bramek, mecze bezpośrednie (mała tabela drużyn z równymi punktami i bilansem),
        // bramki zdobyte, nazwa.
        public static List<WierszTabeli> Oblicz(IEnumerable<Druzyna> druzyny, IEnumerable<Mecz> mecze) =>
            Oblicz(druzyny, mecze, bezMeczowBezposrednich: false);

        private static List<WierszTabeli> Oblicz(IEnumerable<Druzyna> druzyny, IEnumerable<Mecz> mecze, bool bezMeczowBezposrednich)
        {
            var tabela = druzyny.ToDictionary(
                d => d.IdDruzyny,
                d => new WierszTabeli { DruzynaId = d.IdDruzyny, Druzyna = d.Nazwa });

            foreach (var m in mecze.Where(m => m.Rozegrany))
            {
                if (!tabela.TryGetValue(m.GospodarzId, out var gosp) || !tabela.TryGetValue(m.GoscId, out var gosc))
                    continue;

                int bg = m.BramkiGospodarza!.Value, bs = m.BramkiGoscia!.Value;
                gosp.Mecze++; gosc.Mecze++;
                gosp.BramkiZdobyte += bg; gosp.BramkiStracone += bs;
                gosc.BramkiZdobyte += bs; gosc.BramkiStracone += bg;

                if (bg > bs) { gosp.Wygrane++; gosc.Porazki++; }
                else if (bg < bs) { gosc.Wygrane++; gosp.Porazki++; }
                else { gosp.Remisy++; gosc.Remisy++; }
            }

            var rozegrane = mecze.Where(m => m.Rozegrany).ToList();
            return tabela.Values
                .GroupBy(w => (w.Punkty, w.Bilans))
                .OrderByDescending(g => g.Key.Punkty)
                .ThenByDescending(g => g.Key.Bilans)
                .SelectMany(g => g.Count() == 1 || bezMeczowBezposrednich ? g.OrderByDescending(w => w.BramkiZdobyte).ThenBy(w => w.Druzyna) : MeczeBezposrednie(g.ToList(), rozegrane))
                .ToList();
        }

        // Rozstrzyga remis w tabeli wynikami meczów rozegranych tylko między zainteresowanymi drużynami
        private static IEnumerable<WierszTabeli> MeczeBezposrednie(List<WierszTabeli> remis, List<Mecz> mecze)
        {
            var ids = remis.Select(w => w.DruzynaId).ToHashSet();
            var mala = Oblicz(
                remis.Select(w => new Druzyna { IdDruzyny = w.DruzynaId, Nazwa = w.Druzyna }),
                mecze.Where(m => ids.Contains(m.GospodarzId) && ids.Contains(m.GoscId)), bezMeczowBezposrednich: true)
                .ToDictionary(w => w.DruzynaId);

            return remis
                .OrderByDescending(w => mala[w.DruzynaId].Punkty)
                .ThenByDescending(w => mala[w.DruzynaId].Bilans)
                .ThenByDescending(w => mala[w.DruzynaId].BramkiZdobyte)
                .ThenByDescending(w => w.BramkiZdobyte)
                .ThenBy(w => w.Druzyna);
        }
    }
}
