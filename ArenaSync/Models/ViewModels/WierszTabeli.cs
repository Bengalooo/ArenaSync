using System.ComponentModel;

namespace ArenaSync.Models.ViewModels
{
    // Jeden wiersz tabeli ligowej turnieju (liczony z rozegranych meczów).
    public class WierszTabeli
    {
        public int DruzynaId { get; set; }
        [DisplayName("Drużyna")] public string Druzyna { get; set; } = string.Empty;
        [DisplayName("M")] public int Mecze { get; set; }
        [DisplayName("W")] public int Wygrane { get; set; }
        [DisplayName("R")] public int Remisy { get; set; }
        [DisplayName("P")] public int Porazki { get; set; }
        [DisplayName("BZ")] public int BramkiZdobyte { get; set; }
        [DisplayName("BS")] public int BramkiStracone { get; set; }
        [DisplayName("+/-")] public int Bilans => BramkiZdobyte - BramkiStracone;
        [DisplayName("Pkt")] public int Punkty => Wygrane * 3 + Remisy;
    }
}
