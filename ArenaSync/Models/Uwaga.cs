using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ArenaSync.Models
{
    public enum KategoriaUwagi
    {
        [Display(Name = "Błąd w aplikacji")] Blad,
        [Display(Name = "Propozycja zmiany")] Propozycja,
        [Display(Name = "Pytanie")] Pytanie,
        [Display(Name = "Dane drużyny lub zawodnika")] Dane,
        [Display(Name = "Inne")] Inne
    }

    public enum StatusUwagi
    {
        [Display(Name = "Nowa")] Nowa,
        [Display(Name = "W trakcie")] WTrakcie,
        [Display(Name = "Rozwiązana")] Rozwiazana
    }

    // Uwaga (zgłoszenie) użytkownika do administratora: błąd, propozycja, pytanie
    public class Uwaga
    {
        [Key]
        public int IdUwagi { get; set; }

        [Required(ErrorMessage = "Wybierz kategorię.")]
        [DisplayName("Kategoria")]
        public KategoriaUwagi Kategoria { get; set; }

        [Required(ErrorMessage = "Podaj temat.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Temat musi mieć od 5 do 100 znaków.")]
        [DisplayName("Temat")]
        public string Temat { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opisz swoją uwagę.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Opis musi mieć od 10 do 2000 znaków.")]
        [DataType(DataType.MultilineText)]
        [DisplayName("Opis")]
        public string Tresc { get; set; } = string.Empty;

        [DisplayName("Zgłoszono")]
        public DateTime DataZgloszenia { get; set; } = DateTime.Now;

        public string AutorId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser? Autor { get; set; }

        [DisplayName("Status")]
        public StatusUwagi Status { get; set; } = StatusUwagi.Nowa;

        [StringLength(2000, ErrorMessage = "Odpowiedź może mieć najwyżej 2000 znaków.")]
        [DataType(DataType.MultilineText)]
        [DisplayName("Odpowiedź administratora")]
        public string? Odpowiedz { get; set; }

        [DisplayName("Data odpowiedzi")]
        public DateTime? DataOdpowiedzi { get; set; }
    }
}
