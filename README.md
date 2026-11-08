# ArenaSync

Hub do obsługi amatorskich turniejów piłkarskich. Aplikacja webowa ASP.NET Core MVC
z bazą danych SQLite (Entity Framework Core) i kontrolą dostępu opartą na rolach (ASP.NET Core Identity).

## Wymagania

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (opcjonalnie) Visual Studio 2026 / VS Code / Rider

Nic więcej nie trzeba instalować: baza `arenasync.db` tworzy się sama przy pierwszym uruchomieniu
(migracje + dane przykładowe), a Bootstrap i jQuery są w `wwwroot/lib`.

## Uruchomienie

- Windows: dwuklik na **`ArenaSync.exe`** (ikona z logo). Program sprawdza SDK, buduje i uruchamia serwer
  i sam otwiera przeglądarkę. Ponowny dwuklik, gdy aplikacja już działa, tylko otwiera przeglądarkę.
  Przy pierwszym uruchomieniu z pobranego zipa Windows może pokazać „System Windows ochronił ten komputer”
  (program nie ma podpisu cyfrowego): kliknij „Więcej informacji” → „Uruchom mimo to”.
  Zapasowo można użyć `uruchom.bat`, który robi to samo, albo
- w terminalu, w tym folderze: `dotnet run --project ArenaSync`, albo
- w Visual Studio: otwórz `ArenaSync.sln` i naciśnij F5.

Aplikacja działa pod adresem http://localhost:5000 tak długo, jak otwarte jest okno konsoli.
Jeśli przeglądarka pokazuje „odmowa połączenia”, serwer jeszcze się buduje albo okno konsoli pokazuje błąd.

### Konta testowe

| Rola          | E-mail                  | Hasło         |
|---------------|-------------------------|---------------|
| Administrator | admin@arenasync.pl      | Admin@2026!   |
| Kapitan       | kapitan@arenasync.pl    | Kapitan@2026! |
| Zawodnik      | zawodnik@arenasync.pl   | Zawodnik@2026! |

Nowe konto zawodnika lub kapitana zakłada się przez „Załóż konto”. Rolę administratora nadaje inny administrator
w „Administracja → Użytkownicy i role”.

Przy pierwszym wejściu pojawia się okno zgody na pliki cookies – bez jej zaakceptowania nie da się zalogować.

### Program startowy ArenaSync.exe

Źródło programu startowego jest w `narzedzia/launcher/` (`ArenaSyncLauncher.cs` i ikona `ArenaSync.ico`).
To mała aplikacja konsolowa .NET Framework 4.8 (wbudowanego w Windows 10/11), więc nie wymaga niczego dodatkowego.
Przebudowanie: `dotnet build -c Release narzedzia/launcher/ArenaSyncLauncher.csproj`, potem skopiuj
`bin/Release/net48/ArenaSync.exe` do głównego folderu.

## Role i uprawnienia (RBAC)

Bez logowania widać tylko stronę powitalną, politykę prywatności, deklarację dostępności i mapę serwisu.

| Funkcja                                              | Zawodnik | Kapitan | Administrator |
|------------------------------------------------------|:--------:|:-------:|:-------------:|
| Panel: najbliższy mecz, terminarz i wyniki drużyny, pozycja w tabeli | ✓ | ✓ | – |
| Turnieje, tabele, terminarz, wyniki, lista drużyn    | ✓        | ✓       | ✓             |
| Pełne nazwiska zawodników                            | swojej drużyny | swojej drużyny | wszystkich |
| Zgłoszenie drużyny (czeka na zatwierdzenie)          |          | ✓       | ✓ (od razu zatwierdzona) |
| Edycja danych drużyny                                |          | swojej  | ✓             |
| Dodawanie / edycja / usuwanie zawodników (numery, pozycje) |    | swojej drużyny | ✓      |
| Odłączanie kont członków od drużyny                  |          | swojej  | ✓             |
| Tworzenie i usuwanie drużyn, zatwierdzanie zgłoszeń  |          |         | ✓             |
| Turnieje i mecze (CRUD), wyniki, generator terminarza |         |         | ✓             |
| Użytkownicy i role                                   |          |         | ✓             |
| Odblokowanie / ręczna blokada konta, reset hasła innym |        |         | ✓             |
| Ustawienia konta (imię) i zmiana własnego hasła      | ✓        | ✓       | ✓             |
| Sortowanie i wyszukiwanie na listach                 | ✓        | ✓       | ✓             |
| Statystyki turniejów                                 | ✓        | ✓       | ✓ (+ konta, zgłoszenia) |
| Zgłaszanie uwag i podgląd odpowiedzi                 | swoje    | swoje   | wszystkie: odpowiedź, status, usuwanie |

### Przeglądanie, sortowanie, wyszukiwanie, statystyki, uwagi

- **Sortowanie:** kliknięcie nagłówka kolumny (Drużyny, Zawodnicy, Użytkownicy, Uwagi) sortuje rosnąco, drugie kliknięcie malejąco;
  Turnieje i Terminarz mają pola „Sortuj według” i „Kierunek”. Kolejność liter polska (Ł po L, Ś po S).
- **Wyszukiwanie:** pole „Szukaj” na każdej liście (bez rozróżniania wielkości liter, także polskich). Po nazwisku zawodnika
  można szukać tylko tam, gdzie nazwisko jest widoczne (RODO).
- **Statystyki:** menu „Statystyki” – mecze rozegrane, bramki, średnia bramek na mecz, najlepszy atak i obrona,
  najwięcej zwycięstw, najwyższe zwycięstwo, zawodnicy wg pozycji, średni wiek; administrator widzi też konta wg ról,
  konta zablokowane, drużyny do zatwierdzenia i nowe uwagi.
- **Zgłaszanie uwag:** menu konta → „Zgłoś uwagę” (kategoria, temat, opis). Odpowiedź administratora widać w „Moich zgłoszeniach”.
  Administrator: „Administracja → Zgłoszone uwagi” – filtr, odpowiedź, status (Nowa / W trakcie / Rozwiązana), usuwanie.
- **Mapa serwisu:** link w stopce – wszystkie strony z podziałem na role.

### Hasła

Hasło musi mieć co najmniej 10 znaków, w tym małą i wielką literę, cyfrę i znak specjalny (`Services/Hasla.cs`;
te same reguły w Identity, w walidacji formularzy i w liście wymagań odhaczanej na bieżąco przy wpisywaniu).
Zalogowany użytkownik zmienia hasło w menu konta → „Zmień hasło” (trzeba podać obecne hasło).
Stara baza: konta testowe ze starymi hasłami (`Admin123!` itd.) dostają przy starcie nowe hasła z tabeli powyżej.

### Blokada konta

Trzy błędne hasła z rzędu blokują konto kapitana lub zawodnika. Strona logowania pokazuje wtedy komunikat
z adresem administratora (link e-mail z gotowym tematem; adres ustawia się w `appsettings.json` → `ArenaSync:KontaktEmail`).
Konto odblokowuje administrator w „Administracja → Użytkownicy i role” (przycisk „Odblokuj” albo „Resetuj hasło”,
które ustawia podane hasło lub generuje tymczasowe i od razu odblokowuje konto). Administrator może też ręcznie
zablokować konto – użytkownik zostaje od razu wylogowany. Konta administratorów nie są blokowane po błędnych hasłach.
Logowanie jest ważne do zamknięcia aplikacji: po ponownym uruchomieniu trzeba zalogować się od nowa.

## Dostępność (WCAG 2.1 AA)

Przycisk **Dostępność** w menu otwiera panel z ustawieniami:
- motyw ciemny, jasny, **wysoki kontrast** (żółte na czarnym) lub automatyczny (wg systemu),
- rozmiar tekstu 100% / 125% / 150%,
- **tryb dla daltonistów** (paleta Okabe-Ito zamiast zieleni i czerwieni),
- podkreślanie linków, większe odstępy w tekście (dysleksja), wyłączenie animacji.

Dla czytników ekranu: link „Przejdź do treści”, regiony i nagłówki, podpisy tabel, wyniki czytane słownie,
etykiety i powiązane komunikaty błędów w formularzach, ogłaszanie komunikatów. Statusy mają zawsze tekst i symbol,
nie tylko kolor. Strony sprawdzono automatycznie narzędziem axe-core (reguły WCAG 2.1 A/AA) we wszystkich motywach.
Opis: strona „Deklaracja dostępności” w stopce.

## Pliki cookies i RODO

- Niezbędne: sesja logowania, ochrona formularzy (antiforgery), komunikaty, zapamiętanie zgody.
- Funkcjonalne (za zgodą): zapamiętanie ustawień dostępności. Bez zgody ustawienia działają do zamknięcia karty.
- Osoby spoza drużyny widzą zawodników jako „Jan K.” (art. 32 RODO), dane kontaktowe drużyny widzi tylko jej kapitan i administrator.
- Okno zgody: „Akceptuję tylko niezbędne”, „Akceptuję wszystkie” albo „Dostosuj wybór” → „Akceptuję zaznaczone”.
  Po odznaczeniu plików funkcjonalnych przycisk „Akceptuję wszystkie” jest wyszarzony, żeby wybór był spójny.
  Pliki funkcjonalne są domyślnie odznaczone (zgoda musi być aktywna, art. 4 pkt 11 RODO).
- Szczegóły: strona „Polityka prywatności i cookies” w stopce.

## Model danych

- **Turniej** – nazwa, dyscyplina, lokalizacja, daty, maks. liczba drużyn, status
- **Druzyna** – nazwa, miasto, trener, kontakt, zatwierdzona, turniej, kapitan (użytkownik Identity)
- **ApplicationUser** – konto Identity rozszerzone o imię i nazwisko oraz drużynę zawodnika
- **Zawodnik** – imię, nazwisko, data urodzenia, pozycja, numer koszulki, drużyna
- **Mecz** – turniej, gospodarz, gość, termin, boisko, wynik
- **Uwaga** – kategoria, temat, opis, data, autor (konto), status, odpowiedź administratora
- tabele ASP.NET Core Identity (użytkownicy, role)

Tabela ligowa liczona jest na bieżąco z wyników (3 pkt za zwycięstwo, 1 za remis;
kolejność: punkty, bilans bramek, mecze bezpośrednie, bramki zdobyte).

## Struktura projektu

```
ArenaSync.sln
ArenaSync/                 aplikacja MVC
  Controllers/             Home, Turnieje, Druzyny, Zawodnicy, Mecze, Account, Uzytkownicy, Statystyki, Uwagi
  Models/                  encje z DataAnnotations + ViewModels
  Data/                    ApplicationDbContext, ArenaSeeder, Role
  Services/                TabelaService, TerminarzGenerator, StatystykiService, Sortowanie, Wyszukiwanie,
                           Maskowanie (RODO, uprawnienia), Hasla, ArenaClaimsFactory
  TagHelpers/              nagłówek kolumny z sortowaniem (<th sortuj="...">)
  wwwroot/js/site.js       ułatwienia dostępu i zgoda na cookies
  Migrations/              migracje EF Core
  Views/                   widoki Razor (Bootstrap 5, mobile-first)
ArenaSync.Tests/           testy jednostkowe xUnit
```

## Testy

```
dotnet test
```

## Przydatne

- Reset bazy: zatrzymaj aplikację i usuń `ArenaSync/arenasync.db*` (utworzy się od nowa z danymi przykładowymi).
  Baza z poprzedniej wersji aktualizuje się sama (migracja `RoleZawodnikIKonta`, rola Organizator → Admin).
- Nowa migracja po zmianie modelu: `dotnet ef migrations add NazwaMigracji --project ArenaSync`
  (narzędzie: `dotnet tool install --global dotnet-ef`).
- Do zipa nie dodawaj folderów `bin`, `obj` ani pliku `arenasync.db`.
