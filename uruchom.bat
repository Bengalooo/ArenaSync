@echo off
REM Uruchamia ArenaSync (wymaga .NET 10 SDK) i otwiera przegladarke, gdy serwer wystartuje
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [BLAD] Nie znaleziono polecenia "dotnet" - zainstaluj .NET 10 SDK:
    echo https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

dotnet --list-sdks | findstr /b "10." >nul
if errorlevel 1 (
    echo [BLAD] Brak .NET 10 SDK. Zainstalowane SDK:
    dotnet --list-sdks
    echo.
    echo Pobierz .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
    pause
    exit /b 1
)

echo Budowanie i uruchamianie ArenaSync (pierwsze uruchomienie moze potrwac ok. minuty)...
echo Przegladarka otworzy sie sama, gdy serwer bedzie gotowy: http://localhost:5000
echo Aby zatrzymac aplikacje, zamknij to okno lub nacisnij Ctrl+C.
echo.

start "" /b powershell -NoProfile -WindowStyle Hidden -Command "for($i=0;$i -lt 180;$i++){try{Invoke-WebRequest 'http://localhost:5000' -UseBasicParsing -TimeoutSec 2 | Out-Null; Start-Process 'http://localhost:5000'; break}catch{Start-Sleep -Seconds 1}}"

dotnet run --project ArenaSync\ArenaSync.csproj --launch-profile http

echo.
echo Aplikacja zostala zatrzymana albo wystapil blad (opis powyzej).
pause
