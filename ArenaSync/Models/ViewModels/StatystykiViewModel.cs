using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArenaSync.Models.ViewModels
{
    // Statystyki jednego turnieju (Services/StatystykiService) + dane ogólne dla administratora
    public class StatystykiTurnieju
    {
        public int Druzyny { get; set; }
        public int Zawodnicy { get; set; }
        public int MeczeRozegrane { get; set; }
        public int MeczeWszystkie { get; set; }
        public int Bramki { get; set; }
        public double SredniaBramek { get; set; }
        public int Remisy { get; set; }
        public int WygraneGospodarzy { get; set; }
        public int WygraneGosci { get; set; }
        public double? SredniWiek { get; set; }
        public List<(string Nazwa, int Wartosc)> NajlepszyAtak { get; set; } = new();
        public List<(string Nazwa, int Wartosc)> NajlepszaObrona { get; set; } = new();
        public List<(string Nazwa, int Wartosc)> NajwiecejZwyciestw { get; set; } = new();
        public Mecz? NajwyzszeZwyciestwo { get; set; }
        public Mecz? NajwiecejBramek { get; set; }
        public List<(string Pozycja, int Liczba)> ZawodnicyWgPozycji { get; set; } = new();
    }

    public class StatystykiViewModel
    {
        public int? TurniejId { get; set; }
        public string NazwaTurnieju { get; set; } = string.Empty;
        public SelectList Turnieje { get; set; } = default!;
        public StatystykiTurnieju Turniej { get; set; } = new();

        // Tylko dla administratora
        public List<(string Rola, int Liczba)> KontaWgRoli { get; set; } = new();
        public int KontaZablokowane { get; set; }
        public int DruzynyOczekujace { get; set; }
        public int UwagiNowe { get; set; }
    }
}
