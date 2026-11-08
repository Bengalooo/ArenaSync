using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace ArenaSync.TagHelpers
{
    // Nagłówek kolumny z sortowaniem: <th sortuj="nazwa">Drużyna</th>
    // Tworzy link z parametrami ?sort=nazwa&kier=asc|desc (pozostałe filtry zostają) i ustawia aria-sort.
    [HtmlTargetElement("th", Attributes = "sortuj")]
    public class SortujTagHelper : TagHelper
    {
        [HtmlAttributeName("sortuj")]
        public string Klucz { get; set; } = string.Empty;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var zapytanie = ViewContext.HttpContext.Request.Query;
            string obecny = zapytanie["sort"].ToString();
            bool malejaco = zapytanie["kier"].ToString() == "desc";
            bool aktywny = string.Equals(obecny, Klucz, StringComparison.OrdinalIgnoreCase);

            // Kliknięcie aktywnej kolumny odwraca kierunek
            string nowyKier = aktywny && !malejaco ? "desc" : "asc";
            var parametry = zapytanie
                .Where(p => p.Key != "sort" && p.Key != "kier")
                .SelectMany(p => p.Value.Select(v => new KeyValuePair<string, string>(p.Key, v ?? "")))
                .Append(new("sort", Klucz))
                .Append(new("kier", nowyKier));
            var adres = ViewContext.HttpContext.Request.Path + QueryString.Create(parametry).ToString();

            string tytul = (await output.GetChildContentAsync()).GetContent();
            string strzalka = !aktywny ? "↕" : malejaco ? "▼" : "▲";
            string opis = aktywny
                ? (malejaco ? ", posortowano malejąco" : ", posortowano rosnąco")
                : ", sortuj";

            output.Attributes.SetAttribute("aria-sort", !aktywny ? "none" : malejaco ? "descending" : "ascending");
            if (!output.Attributes.ContainsName("scope")) output.Attributes.SetAttribute("scope", "col");
            output.Content.SetHtmlContent(
                $"<a class=\"sortuj{(aktywny ? " aktywny" : "")}\" href=\"{HtmlEncoder.Default.Encode(adres)}\">{tytul}" +
                $"<span class=\"strzalka\" aria-hidden=\"true\"> {strzalka}</span>" +
                $"<span class=\"visually-hidden\">{opis}</span></a>");
        }
    }
}
