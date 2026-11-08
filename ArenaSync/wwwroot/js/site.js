// ArenaSync - ułatwienia dostępu (WCAG 2.1) i zgoda na pliki cookies
(function () {
    'use strict';

    var KLUCZ = 'arenasync-dostepnosc';
    var ZGODA = 'ArenaSync.Zgoda';
    var html = document.documentElement;

    // ---------- Zgoda na cookies ----------
    function czytajZgode() {
        var m = document.cookie.match(new RegExp('(?:^|; )' + ZGODA.replace('.', '\\.') + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : null;
    }

    function zapiszZgode(wartosc) {
        // Ciasteczko niezbędne: pamięta wybór przez rok (art. 173 Prawa telekomunikacyjnego)
        document.cookie = ZGODA + '=' + wartosc + '; max-age=31536000; path=/; SameSite=Lax';
        if (wartosc !== 'wszystkie') {
            // Bez zgody na pliki funkcjonalne ustawienia dostępności żyją tylko do zamknięcia karty
            try {
                var u = localStorage.getItem(KLUCZ);
                if (u) sessionStorage.setItem(KLUCZ, u);
                localStorage.removeItem(KLUCZ);
            } catch (e) { }
        } else {
            try {
                var s = sessionStorage.getItem(KLUCZ);
                if (s) localStorage.setItem(KLUCZ, s);
            } catch (e) { }
        }
    }

    function magazyn() {
        try { return czytajZgode() === 'wszystkie' ? localStorage : sessionStorage; } catch (e) { return null; }
    }

    // ---------- Ustawienia dostępności ----------
    var domyslne = { motyw: 'auto', tekst: '100', daltonizm: false, linki: false, odstepy: false, animacje: 'auto' };

    function wczytaj() {
        var u = {};
        try {
            var zapis = localStorage.getItem(KLUCZ) || sessionStorage.getItem(KLUCZ);
            if (zapis) u = JSON.parse(zapis);
        } catch (e) { }
        for (var k in domyslne) if (!(k in u)) u[k] = domyslne[k];
        return u;
    }

    function zastosuj(u) {
        var motyw = u.motyw;
        if (motyw === 'auto') {
            motyw = window.matchMedia && window.matchMedia('(prefers-contrast: more)').matches ? 'kontrast'
                : window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches ? 'jasny' : 'ciemny';
        }
        html.setAttribute('data-bs-theme', motyw === 'jasny' ? 'light' : 'dark');
        if (motyw === 'kontrast') html.setAttribute('data-kontrast', 'wysoki'); else html.removeAttribute('data-kontrast');

        if (u.tekst !== '100') html.setAttribute('data-tekst', u.tekst); else html.removeAttribute('data-tekst');
        ['daltonizm', 'linki', 'odstepy'].forEach(function (k) {
            if (u[k]) html.setAttribute('data-' + k, ''); else html.removeAttribute('data-' + k);
        });

        var bezAnimacji = u.animacje === 'wylaczone' ||
            (u.animacje === 'auto' && window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
        if (bezAnimacji) html.setAttribute('data-animacje', 'wylaczone'); else html.removeAttribute('data-animacje');
        ukladMenu();
    }

    // Przy powiększonym tekście menu zwija się do przycisku wcześniej, żeby strona nie wychodziła poza ekran (WCAG 1.4.10)
    // Menu rozwinięte w jednym wierszu tylko wtedy, gdy się mieści (zależy od szerokości, rozmiaru tekstu i roli);
    // inaczej zostaje przycisk „Pokaż menu”, żeby nic nie wychodziło poza ekran (WCAG 1.4.10)
    function ukladMenu() {
        var nav = document.querySelector('.arena-nav');
        var kontener = nav && nav.querySelector('.container');
        if (!kontener) return;
        nav.classList.remove('menu-zwiniete', 'navbar-expand-xxl');
        nav.classList.add('navbar-expand-xl');
        if (kontener.scrollWidth > kontener.clientWidth + 1) {
            nav.classList.remove('navbar-expand-xl');
            nav.classList.add('menu-zwiniete');
        }
    }

    function zapisz(u) {
        var m = magazyn();
        try { if (m) m.setItem(KLUCZ, JSON.stringify(u)); } catch (e) { }
    }

    // Ustawienia nakładane od razu (skrypt w <head>), żeby strona nie "mrugała" innym motywem
    var ustawienia = wczytaj();
    zastosuj(ustawienia);

    window.ArenaSync = {
        ustawienia: ustawienia,
        zastosuj: zastosuj,
        zapisz: zapisz,
        czytajZgode: czytajZgode,
        zapiszZgode: zapiszZgode
    };

    document.addEventListener('DOMContentLoaded', function () {
        var komunikat = document.getElementById('a11y-komunikat');
        function ogloś(tekst) { if (komunikat) { komunikat.textContent = ''; setTimeout(function () { komunikat.textContent = tekst; }, 50); } }

        // ---------- Panel "Dostępność" ----------
        var panel = document.getElementById('panelDostepnosci');
        if (panel) {
            function odswiezPanel() {
                panel.querySelectorAll('input[data-ustawienie]').forEach(function (el) {
                    var k = el.getAttribute('data-ustawienie');
                    if (el.type === 'radio') el.checked = String(ustawienia[k]) === el.value;
                    else el.checked = !!ustawienia[k] && ustawienia[k] !== 'auto';
                });
            }
            odswiezPanel();

            panel.addEventListener('change', function (e) {
                var el = e.target;
                var k = el.getAttribute('data-ustawienie');
                if (!k) return;
                if (el.type === 'radio') ustawienia[k] = el.value;
                else if (k === 'animacje') ustawienia[k] = el.checked ? 'wylaczone' : 'auto';
                else ustawienia[k] = el.checked;
                zastosuj(ustawienia);
                zapisz(ustawienia);
                var etykieta = el.closest('.form-check') ? el.closest('.form-check').querySelector('label').textContent.trim() : '';
                ogloś('Zmieniono: ' + etykieta + (el.type === 'checkbox' ? (el.checked ? ', włączone' : ', wyłączone') : ''));
            });

            var reset = document.getElementById('a11y-reset');
            if (reset) reset.addEventListener('click', function () {
                for (var k in domyslne) ustawienia[k] = domyslne[k];
                zastosuj(ustawienia);
                zapisz(ustawienia);
                odswiezPanel();
                ogloś('Przywrócono domyślne ustawienia dostępności');
            });
        }

        // ---------- Okno zgody na cookies ----------
        var okno = document.getElementById('oknoCookies');
        if (okno && window.bootstrap) {
            var modal = new bootstrap.Modal(okno, { backdrop: 'static', keyboard: false });
            if (!czytajZgode()) modal.show();

            okno.querySelectorAll('[data-zgoda]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var wybor = btn.getAttribute('data-zgoda');
                    if (wybor === 'wybrane') wybor = document.getElementById('zgodaFunkcjonalne').checked ? 'wszystkie' : 'niezbedne';
                    zapiszZgode(wybor);
                    zapisz(ustawienia);
                    modal.hide();
                    ogloś('Zapisano wybór dotyczący plików cookies');
                });
            });

            // Po otwarciu „Dostosuj wybór”: przycisk „Akceptuję zaznaczone”, a „Akceptuję wszystkie”
            // jest nieaktywny, dopóki pliki funkcjonalne są odznaczone (wybór musi być spójny)
            var szczegoly = document.getElementById('zgodaSzczegoly');
            var funkcjonalne = document.getElementById('zgodaFunkcjonalne');
            var btnWszystkie = okno.querySelector('[data-zgoda="wszystkie"]');
            var btnWybrane = okno.querySelector('[data-zgoda="wybrane"]');
            var podpowiedz = document.getElementById('zgodaPodpowiedz');
            function odswiezPrzyciski() {
                var dostosowanie = szczegoly.open;
                var zablokuj = dostosowanie && !funkcjonalne.checked;
                btnWybrane.hidden = !dostosowanie;
                btnWszystkie.disabled = zablokuj;
                podpowiedz.hidden = !zablokuj;
                if (zablokuj) btnWszystkie.setAttribute('aria-describedby', 'zgodaPodpowiedz');
                else btnWszystkie.removeAttribute('aria-describedby');
            }
            szczegoly.addEventListener('toggle', odswiezPrzyciski);
            funkcjonalne.addEventListener('change', odswiezPrzyciski);

            document.querySelectorAll('[data-pokaz-cookies]').forEach(function (el) {
                el.addEventListener('click', function (e) {
                    e.preventDefault();
                    funkcjonalne.checked = czytajZgode() === 'wszystkie';
                    szczegoly.open = true; // ktoś chce zmienić ustawienia - od razu pokazujemy wybór
                    odswiezPrzyciski();
                    modal.show();
                });
            });
        }

        ukladMenu();
        var czasMenu;
        window.addEventListener('resize', function () { clearTimeout(czasMenu); czasMenu = setTimeout(ukladMenu, 100); });
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(ukladMenu);

        // ---------- Wymagania hasła odhaczane na bieżąco (te same reguły co Services/Hasla.cs) ----------
        var REGULY = {
            dlugosc: function (h) { return h.length >= 10; },
            mala: function (h) { return /[a-ząćęłńóśźż]/.test(h); },
            wielka: function (h) { return /[A-ZĄĆĘŁŃÓŚŹŻ]/.test(h); },
            cyfra: function (h) { return /\d/.test(h); },
            specjalny: function (h) { return /[^A-Za-z0-9ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]/.test(h); }
        };
        document.querySelectorAll('[data-haslo-dla]').forEach(function (lista) {
            var pole = document.getElementById(lista.getAttribute('data-haslo-dla'));
            if (!pole) return;
            function sprawdz() {
                lista.querySelectorAll('[data-regula]').forEach(function (li) {
                    var ok = REGULY[li.getAttribute('data-regula')](pole.value);
                    li.classList.toggle('spelnione', ok);
                    li.querySelector('.ikona').textContent = ok ? '✓' : '○';
                    li.querySelector('.stan').textContent = ok ? ' – spełnione' : ' – niespełnione';
                });
            }
            pole.addEventListener('input', sprawdz);
            sprawdz();
        });

        // ---------- Pomoc dla czytników ekranu ----------
        // Przewijane tabele dostępne z klawiatury (WCAG 2.1.1)
        document.querySelectorAll('.table-responsive').forEach(function (div) {
            var podpis = div.querySelector('caption');
            div.setAttribute('tabindex', '0');
            div.setAttribute('role', 'region');
            div.setAttribute('aria-label', podpis ? podpis.textContent.trim() : 'Tabela');
        });

        // aria-current dla aktywnej pozycji menu
        var sciezka = location.pathname.toLowerCase().replace(/\/index$/, '');
        document.querySelectorAll('.arena-nav .nav-link[href]').forEach(function (a) {
            var href = a.getAttribute('href').toLowerCase();
            if (href !== '/' && (sciezka === href || sciezka.indexOf(href + '/') === 0)) a.setAttribute('aria-current', 'page');
        });

        // Powiązanie pól formularza z komunikatami walidacji (aria-describedby, aria-invalid)
        document.querySelectorAll('span[data-valmsg-for]').forEach(function (span, i) {
            var nazwa = span.getAttribute('data-valmsg-for');
            var pole = document.querySelector('[name="' + nazwa + '"]');
            if (!pole) return;
            if (!span.id) span.id = 'blad-' + nazwa.replace(/\W/g, '') + '-' + i;
            span.setAttribute('aria-live', 'polite');
            var opis = pole.getAttribute('aria-describedby');
            pole.setAttribute('aria-describedby', (opis ? opis + ' ' : '') + span.id);
            var oznacz = function () {
                if (pole.classList.contains('input-validation-error')) pole.setAttribute('aria-invalid', 'true');
                else pole.removeAttribute('aria-invalid');
            };
            oznacz();
            new MutationObserver(oznacz).observe(pole, { attributes: true, attributeFilter: ['class'] });
        });

        // Po błędzie walidacji serwera fokus na pierwsze błędne pole
        var pierwszyBlad = document.querySelector('.input-validation-error');
        if (pierwszyBlad) pierwszyBlad.focus();
    });
})();
