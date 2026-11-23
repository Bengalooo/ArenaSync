// Program startowy ArenaSync: sprawdza .NET 10 SDK, uruchamia serwer (dotnet run)
// i otwiera przeglądarkę, gdy aplikacja odpowie pod http://localhost:5000.
// Kompilacja: dotnet build -c Release narzedzia/launcher/ArenaSyncLauncher.csproj (cel: .NET Framework 4.8)
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

[assembly: AssemblyTitle("ArenaSync")]
[assembly: AssemblyProduct("ArenaSync")]
[assembly: AssemblyDescription("ArenaSync - hub amatorskich turniejów piłkarskich")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

static class Program
{
    const string Adres = "http://localhost:5000";
    const string PobierzSdk = "https://dotnet.microsoft.com/download/dotnet/10.0";

    static int Main()
    {
        try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
        Console.Title = "ArenaSync";

        string katalog = AppDomain.CurrentDomain.BaseDirectory;
        string projekt = Path.Combine(katalog, "ArenaSync", "ArenaSync.csproj");

        Naglowek();

        if (!File.Exists(projekt))
            return Blad("Nie znaleziono projektu: " + projekt +
                        "\nUruchom ArenaSync.exe z rozpakowanego folderu ArenaSync (obok podfolderu ArenaSync).");

        // Aplikacja już działa (np. drugie kliknięcie) - wystarczy otworzyć przeglądarkę
        if (Odpowiada())
        {
            Console.WriteLine("ArenaSync już działa - otwieram przeglądarkę: " + Adres);
            OtworzPrzegladarke();
            Thread.Sleep(1500);
            return 0;
        }

        string sdk;
        try
        {
            sdk = Uruchom("dotnet", "--list-sdks");
        }
        catch (Win32Exception)
        {
            return Blad("Nie znaleziono polecenia \"dotnet\" - zainstaluj .NET 10 SDK:\n" + PobierzSdk);
        }
        if (!MaSdk10(sdk))
            return Blad("Brak .NET 10 SDK. Zainstalowane SDK:\n" + (sdk.Trim().Length > 0 ? sdk : "(brak)") +
                        "\nPobierz .NET 10 SDK: " + PobierzSdk);

        Console.WriteLine("Uruchamianie ArenaSync (pierwsze uruchomienie może potrwać ok. minuty)...");
        Console.WriteLine("Przeglądarka otworzy się sama, gdy serwer będzie gotowy: " + Adres);
        Console.WriteLine("Aby zatrzymać aplikację, zamknij to okno lub naciśnij Ctrl+C.");
        Console.WriteLine();

        var start = new ProcessStartInfo("dotnet",
            "run --project \"" + projekt + "\" --launch-profile http")
        {
            UseShellExecute = false,
            WorkingDirectory = katalog
        };

        Process serwer;
        try { serwer = Process.Start(start); }
        catch (Exception ex) { return Blad("Nie udało się uruchomić serwera: " + ex.Message); }

        // Ctrl+C trafia też do serwera; launcher czeka na jego zakończenie
        Console.CancelKeyPress += (s, e) => e.Cancel = true;

        var czekaj = new Thread(() =>
        {
            for (int i = 0; i < 180 && !serwer.HasExited; i++)
            {
                if (Odpowiada()) { OtworzPrzegladarke(); return; }
                Thread.Sleep(1000);
            }
        }) { IsBackground = true };
        czekaj.Start();

        serwer.WaitForExit();
        Console.WriteLine();
        Console.WriteLine("Aplikacja została zatrzymana albo wystąpił błąd (opis powyżej).");
        Pauza();
        return serwer.ExitCode;
    }

    static void Naglowek()
    {
        var kolor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ===================================");
        Console.WriteLine("    ArenaSync - turnieje piłkarskie");
        Console.WriteLine("  ===================================");
        Console.ForegroundColor = kolor;
        Console.WriteLine();
    }

    static bool MaSdk10(string lista)
    {
        foreach (var linia in lista.Split('\n'))
            if (linia.TrimStart().StartsWith("10.")) return true;
        return false;
    }

    static string Uruchom(string plik, string argumenty)
    {
        var psi = new ProcessStartInfo(plik, argumenty)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using (var p = Process.Start(psi))
        {
            string wynik = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return wynik;
        }
    }

    static bool Odpowiada()
    {
        try
        {
            var zadanie = (HttpWebRequest)WebRequest.Create(Adres);
            zadanie.Timeout = 2000;
            zadanie.AllowAutoRedirect = false;
            zadanie.Proxy = null;
            using (zadanie.GetResponse()) return true;
        }
        catch (WebException ex)
        {
            // Każda odpowiedź HTTP (np. 302, 404) oznacza, że serwer działa
            return ex.Response != null;
        }
        catch { return false; }
    }

    static void OtworzPrzegladarke()
    {
        try { Process.Start(new ProcessStartInfo(Adres) { UseShellExecute = true }); }
        catch { Console.WriteLine("Otwórz przeglądarkę ręcznie: " + Adres); }
    }

    static int Blad(string tresc)
    {
        var kolor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[BŁĄD] " + tresc);
        Console.ForegroundColor = kolor;
        Pauza();
        return 1;
    }

    static void Pauza()
    {
        Console.WriteLine();
        Console.WriteLine("Naciśnij dowolny klawisz, aby zamknąć okno...");
        try { Console.ReadKey(true); } catch { }
    }
}
