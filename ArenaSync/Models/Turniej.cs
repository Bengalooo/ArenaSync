using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ArenaSync.Models
{
    public enum StatusTurnieju
    {
        [Display(Name = "Zapisy otwarte")] ZapisyOtwarte,
        [Display(Name = "W trakcie")] WTrakcie,
        [Display(Name = "Zakończony")] Zakonczony
    }

    public class Turniej
    {
        [Key]
        public int IdTurnieju { get; set; }

        [Required(ErrorMessage = "Podaj nazwę turnieju.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Nazwa musi mieć od 3 do 100 znaków.")]
        [DisplayName("Nazwa turnieju")]
        public string Nazwa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj dyscyplinę.")]
        [StringLength(50)]
        [DisplayName("Dyscyplina")]
        public string Dyscyplina { get; set; } = "Piłka nożna";

        [Required(ErrorMessage = "Podaj lokalizację.")]
        [StringLength(100)]
        [DisplayName("Lokalizacja")]
        public string Lokalizacja { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj datę rozpoczęcia.")]
        [DataType(DataType.Date)]
        [DisplayName("Data rozpoczęcia")]
        public DateTime DataRozpoczecia { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Podaj datę zakończenia.")]
        [DataType(DataType.Date)]
        [DisplayName("Data zakończenia")]
        public DateTime DataZakonczenia { get; set; } = DateTime.Today.AddDays(1);

        [Range(2, 64, ErrorMessage = "Liczba drużyn musi być z przedziału 2–64.")]
        [DisplayName("Maks. liczba drużyn")]
        public int MaksLiczbaDruzyn { get; set; } = 8;

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [DisplayName("Opis")]
        public string? Opis { get; set; }

        [DisplayName("Status")]
        public StatusTurnieju Status { get; set; } = StatusTurnieju.ZapisyOtwarte;

        public ICollection<Druzyna> Druzyny { get; set; } = new List<Druzyna>();
        public ICollection<Mecz> Mecze { get; set; } = new List<Mecz>();
    }
}
