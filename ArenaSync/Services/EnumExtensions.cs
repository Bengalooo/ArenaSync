using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ArenaSync.Services
{
    public static class EnumExtensions
    {
        // Zwraca nazwę z atrybutu [Display(Name = ...)] zamiast nazwy w kodzie
        public static string NazwaWyswietlana(this Enum wartosc) =>
            wartosc.GetType().GetMember(wartosc.ToString()).FirstOrDefault()?
                .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? wartosc.ToString();
    }
}
