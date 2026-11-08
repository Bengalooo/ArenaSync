using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ArenaSync.Models
{
    // Użytkownik systemu rozszerzony o dane drużyny (ASP.NET Core Identity)
    public class ApplicationUser : IdentityUser
    {
        [StringLength(80)]
        [DisplayName("Imię i nazwisko")]
        public string? ImieNazwisko { get; set; }

        // Drużyna, do której należy zawodnik (dla kapitana drużynę wskazuje Druzyna.KapitanId)
        [DisplayName("Drużyna")]
        public int? DruzynaId { get; set; }

        public Druzyna? Druzyna { get; set; }
    }
}
