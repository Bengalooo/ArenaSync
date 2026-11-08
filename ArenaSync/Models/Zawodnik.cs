using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ArenaSync.Models
{
    public enum Pozycja
    {
        [Display(Name = "Bramkarz")] Bramkarz,
        [Display(Name = "Obrońca")] Obronca,
        [Display(Name = "Pomocnik")] Pomocnik,
        [Display(Name = "Napastnik")] Napastnik
    }

    public class Zawodnik
    {
        [Key]
        public int IdZawodnika { get; set; }

        [Required(ErrorMessage = "Podaj imię.")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Imię musi mieć od 2 do 40 znaków.")]
        [DisplayName("Imię")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj nazwisko.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Nazwisko musi mieć od 2 do 60 znaków.")]
        [DisplayName("Nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj datę urodzenia.")]
        [DataType(DataType.Date)]
        [DisplayName("Data urodzenia")]
        public DateTime DataUrodzenia { get; set; } = new DateTime(2000, 1, 1);

        [Required(ErrorMessage = "Wybierz pozycję.")]
        [DisplayName("Pozycja")]
        public Pozycja Pozycja { get; set; }

        [Range(1, 99, ErrorMessage = "Numer koszulki musi być z przedziału 1–99.")]
        [DisplayName("Nr koszulki")]
        public int NumerKoszulki { get; set; }

        [Required(ErrorMessage = "Wybierz drużynę.")]
        [DisplayName("Drużyna")]
        public int DruzynaId { get; set; }

        [ValidateNever]
        public Druzyna? Druzyna { get; set; }

        [DisplayName("Zawodnik")]
        public string PelneImie => $"{Imie} {Nazwisko}";
    }
}
