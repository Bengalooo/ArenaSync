using ArenaSync.Data;
using ArenaSync.Models;
using System.Security.Claims;

namespace ArenaSync.Services
{
    public static class Maskowanie
    {
        // RODO art. 32 - osoby spoza drużyny widzą tylko imię i pierwszą literę nazwiska ("Jan K.")
        public static string Nazwisko(string imie, string nazwisko) =>
            string.IsNullOrWhiteSpace(nazwisko) ? imie : $"{imie} {char.ToUpper(nazwisko.Trim()[0])}.";

        public static string? UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier);

        // Drużyna zawodnika zapisana w ciasteczku logowania
        public static int? DruzynaZawodnika(ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ArenaClaimsFactory.DruzynaClaim), out var id) ? id : null;

        public static bool CzyKapitan(ClaimsPrincipal user, Druzyna? druzyna) =>
            druzyna?.KapitanId != null && druzyna.KapitanId == UserId(user);

        // Pełne dane widzi admin, kapitan drużyny i jej zawodnicy
        public static bool PelnyWglad(ClaimsPrincipal user, Druzyna? druzyna) =>
            druzyna != null && (user.IsInRole(Role.Admin) || CzyKapitan(user, druzyna) || DruzynaZawodnika(user) == druzyna.IdDruzyny);

        // Zarządzać kadrą może admin i kapitan drużyny
        public static bool MozeZarzadzac(ClaimsPrincipal user, Druzyna? druzyna) =>
            druzyna != null && (user.IsInRole(Role.Admin) || CzyKapitan(user, druzyna));

        public static string Zawodnik(ClaimsPrincipal user, Zawodnik z) =>
            PelnyWglad(user, z.Druzyna) ? z.PelneImie : Nazwisko(z.Imie, z.Nazwisko);
    }
}
