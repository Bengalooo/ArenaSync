using ArenaSync.Data;
using ArenaSync.Models;
using ArenaSync.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArenaSync.Controllers
{
    // Wspólne pomocnicze metody uprawnień dla kontrolerów
    public abstract class BaseController : Controller
    {
        protected bool CzyAdmin => User.IsInRole(Role.Admin);

        protected string? UserId => Maskowanie.UserId(User);

        // Admin może wszystko, kapitan tylko swoją drużynę
        protected bool MozeZarzadzac(Druzyna druzyna) => Maskowanie.MozeZarzadzac(User, druzyna);

        // Członek drużyny (admin, kapitan, zawodnik) widzi pełne dane kadry
        protected bool PelnyWglad(Druzyna druzyna) => Maskowanie.PelnyWglad(User, druzyna);

        protected void Komunikat(string tekst) => TempData["Komunikat"] = tekst;
    }
}
