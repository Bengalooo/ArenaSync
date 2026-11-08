using ArenaSync.Models;

namespace ArenaSync.Services
{
    public static class TerminarzGenerator
    {
        // Generuje terminarz "każdy z każdym" metodą kołową (round-robin).
        // Przy nieparzystej liczbie drużyn jedna pauzuje w każdej kolejce.
        // Mecze w kolejce co 'odstepMinut' minut. Kolejne kolejki są co 'odstepDni' dni,
        // a przy odstepDni = 0 gra się jedna po drugiej tego samego dnia.
        public static List<Mecz> Generuj(int turniejId, IList<int> druzynyIds, DateTime start,
            int odstepDni = 1, int odstepMinut = 60, string? boisko = null)
        {
            var mecze = new List<Mecz>();
            if (druzynyIds.Count < 2) return mecze;

            var lista = new List<int?>(druzynyIds.Select(id => (int?)id));
            if (lista.Count % 2 == 1) lista.Add(null); // pauza

            int n = lista.Count;
            int kolejki = n - 1;

            int meczeWKolejce = druzynyIds.Count / 2;
            for (int k = 0; k < kolejki; k++)
            {
                var dzien = odstepDni > 0
                    ? start.AddDays(k * odstepDni)
                    : start.AddMinutes(k * meczeWKolejce * odstepMinut);
                int nrMeczu = 0;
                for (int i = 0; i < n / 2; i++)
                {
                    var a = lista[i];
                    var b = lista[n - 1 - i];
                    if (a == null || b == null) continue;

                    // Naprzemienne gospodarstwo, żeby pierwsza drużyna nie grała zawsze u siebie
                    bool zamien = (k % 2 == 1) && i == 0;
                    mecze.Add(new Mecz
                    {
                        TurniejId = turniejId,
                        GospodarzId = zamien ? b.Value : a.Value,
                        GoscId = zamien ? a.Value : b.Value,
                        Termin = dzien.AddMinutes(nrMeczu * odstepMinut),
                        Boisko = boisko
                    });
                    nrMeczu++;
                }

                // Obrót: pierwszy element stały, reszta przesuwa się o jedną pozycję
                var ostatni = lista[n - 1];
                lista.RemoveAt(n - 1);
                lista.Insert(1, ostatni);
            }

            return mecze;
        }
    }
}
