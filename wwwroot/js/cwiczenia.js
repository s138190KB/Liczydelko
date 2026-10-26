(function () {
    const $ = id => document.getElementById(id);
    const LIMIT_CZASU = 30;
    let timer = null;

    function el(tag, cls, txt) {
        const e = document.createElement(tag);
        if (cls) e.className = cls;
        if (txt !== undefined) e.textContent = txt;
        return e;
    }
    function przycisk(tekst, cls) {
        const b = el('button', cls || 'przycisk', tekst);
        b.type = 'button';
        return b;
    }
    function poleTekstowe(cls) {
        const i = el('input', cls);
        i.type = 'text';
        i.autocomplete = 'off';
        return i;
    }
    function przyEnter(kontrolki, akcja) {
        kontrolki.forEach(k => k.addEventListener('keydown', e => { if (e.key === 'Enter') akcja(); }));
    }

    document.querySelectorAll('[data-odkryj]').forEach(odkrywacz => odkrywacz.addEventListener('click', () => {
        const lista = document.querySelector(odkrywacz.dataset.odkryj);
        lista.querySelector('li.ukryty')?.classList.remove('ukryty');
        odkrywacz.hidden = !lista.querySelector('li.ukryty');
    }));

    const dane = $('dane-zadania');
    if (!dane) return;
    const wszystkie = JSON.parse(dane.textContent);
    const zadania = rodzaj => wszystkie.filter(z => z.rodzaj === rodzaj);

    function pola(tresc) {
        const wezel = el('p', 'tresc-zadania');
        const inputs = [];
        tresc.split('[]').forEach((czesc, i, czesci) => {
            wezel.append(czesc);
            if (i < czesci.length - 1) {
                const wpis = poleTekstowe('okienko');
                wezel.append(wpis);
                inputs.push(wpis);
            }
        });
        return { wezel, pola: inputs };
    }
    const odpowiedz = p => p.pola.map(x => x.value).join(';');

    function wyslij(id, odp) {
        return fetch('/Nauka/SprawdzZadanie', {
            method: 'POST',
            headers: window.naglowkiJson(),
            body: JSON.stringify({ zadanieId: id, odpowiedz: odp })
        }).then(r => { if (!r.ok) throw new Error(); return r.json(); });
    }

    function pokazWynik(miejsce, w, komunikat) {
        miejsce.className = 'wynik-zadania ' + (w.poprawna ? 'dobra' : 'zla');
        const czesci = [w.poprawna ? 'Poprawnie.' : `${komunikat || 'Błędnie.'} Poprawna odpowiedź: ${w.wzorcowa}.`];
        if (w.punkty) czesci.push(`Zdobywasz ${w.punkty} pkt.`);
        if (w.wyjasnienie) czesci.push(w.wyjasnienie);
        if (w.awans) czesci.push(`Awansujesz na level: ${w.level}!`);
        if (w.odznaki?.length) czesci.push(`Nowa odznaka: ${w.odznaki.join(', ')}!`);
        miejsce.textContent = czesci.join(' ');
        if (w.awans || w.odznaki?.length) window.konfetti?.();
    }

    function pasekPostepu(i, n) {
        const blok = el('div');
        const pasek = el('div', 'pasek'), wypelnienie = el('div');
        wypelnienie.style.width = `${100 * i / n}%`;
        pasek.append(wypelnienie);
        blok.append(el('p', 'meta', `Zadanie ${i + 1} z ${n}`), pasek);
        return blok;
    }

    function formularz(zadanieId, kontrolki, pobierzOdpowiedz, gotowe) {
        const sprawdzBtn = przycisk('Sprawdź');
        const wynik = el('div', 'wynik-zadania');
        const wlacz = wlaczone => kontrolki.forEach(k => { k.disabled = !wlaczone; });

        function sprawdz() {
            if (sprawdzBtn.disabled) return;
            sprawdzBtn.disabled = true;
            wlacz(false);
            wyslij(zadanieId, pobierzOdpowiedz())
                .then(w => { pokazWynik(wynik, w); sprawdzBtn.hidden = true; gotowe(w.poprawna); })
                .catch(() => {
                    wynik.textContent = 'Błąd połączenia. Spróbuj ponownie.';
                    sprawdzBtn.disabled = false;
                    wlacz(true);
                });
        }
        sprawdzBtn.addEventListener('click', sprawdz);
        przyEnter(kontrolki, sprawdz);
        return { przycisk: sprawdzBtn, wynik };
    }

    const ETYKIETY = {
        wynik: (ok, razem) => `Ukończono! Poprawne odpowiedzi: ${ok} z ${razem}.`,
        ponownie: 'Zacznij od nowa',
        ostatnie: 'Zobacz podsumowanie'
    };
    const ETYKIETY_CZASU = {
        wynik: (ok, razem) => `Wynik: ${ok} z ${razem}.`,
        ponownie: 'Spróbuj ponownie',
        ostatnie: 'Zobacz wynik'
    };

    function sekwencja(kontener, lista, rysuj, od, etykiety = ETYKIETY) {
        let i = 0, ok = 0;
        nastepne();

        function nastepne() {
            kontener.innerHTML = '';
            if (i >= lista.length) {
                kontener.append(el('p', 'wynik', etykiety.wynik(ok, lista.length)));
                if (ok === lista.length) window.konfetti?.();
                const jeszczeRaz = przycisk(etykiety.ponownie);
                jeszczeRaz.addEventListener('click', od);
                kontener.append(jeszczeRaz);
                return;
            }
            kontener.append(pasekPostepu(i, lista.length));
            const karta = rysuj(lista[i], poprawna => {
                if (poprawna) ok++;
                const dalej = przycisk(i + 1 < lista.length ? 'Następne zadanie' : etykiety.ostatnie);
                dalej.addEventListener('click', () => { i++; nastepne(); });
                karta.append(dalej);
            });
            kontener.append(karta);
        }
    }

    function panel(idKontenera, rodzaj, wstep, zbuduj) {
        const k = $(idKontenera);
        k.innerHTML = '';
        const lista = zadania(rodzaj);
        if (!lista.length) { k.append(el('p', 'meta', 'Brak zadań.')); return; }
        if (wstep) k.append(el('p', 'meta', wstep));
        zbuduj(k, lista);
    }

    function wypelnij() {
        panel('cw-wypelnij', 'Wypelnij', null, (k, lista) => {
            const miejsce = el('div');
            k.append(miejsce);
            sekwencja(miejsce, lista, (z, gotowe) => {
                const karta = el('div', 'zadanie-karta');
                const p = pola(z.tresc);
                const sprawdzanie = formularz(z.id, p.pola, () => odpowiedz(p), gotowe);
                karta.append(p.wezel, sprawdzanie.przycisk, sprawdzanie.wynik);
                setTimeout(() => p.pola[0]?.focus(), 0);
                return karta;
            }, wypelnij);
        });
    }

    function blad() {
        panel('cw-blad', 'Blad', 'Poniżej jest rozwiązanie z błędem. Wpisz całe poprawne równanie.', (k, lista) => {
            const miejsce = el('div');
            k.append(miejsce);
            sekwencja(miejsce, lista, (z, gotowe) => {
                const karta = el('div', 'zadanie-karta');
                const kroki = el('ol', 'bledne');
                (z.bledne || '').split('|').forEach(linia => kroki.append(el('li', null, linia)));
                const wpis = poleTekstowe('szerokie');
                wpis.placeholder = 'Wpisz poprawne równanie, np. 2x + 1 = 7';
                const sprawdzanie = formularz(z.id, [wpis], () => wpis.value, gotowe);
                karta.append(el('p', 'tresc-zadania', z.tresc), el('p', 'meta', 'Błędne rozwiązanie:'), kroki,
                    wpis, sprawdzanie.przycisk, sprawdzanie.wynik);
                return karta;
            }, blad);
        });
    }

    function zadanieNaCzas(z, gotowe) {
        let pozostalo = LIMIT_CZASU, koniec = false;
        const karta = el('div');
        const zegar = el('p', 'zegar', `Pozostało: ${pozostalo} s`);
        const pasek = el('div', 'pasek czas'), wypelnienie = el('div');
        wypelnienie.style.width = '100%';
        pasek.append(wypelnienie);
        const p = pola(z.tresc);
        const sprawdzBtn = przycisk('Sprawdź');
        const wynik = el('div', 'wynik-zadania');
        karta.append(zegar, pasek, p.wezel, sprawdzBtn, wynik);

        timer = setInterval(() => {
            pozostalo--;
            zegar.textContent = `Pozostało: ${pozostalo} s`;
            wypelnienie.style.width = `${100 * pozostalo / LIMIT_CZASU}%`;
            zegar.classList.toggle('pilne', pozostalo <= 10);
            if (pozostalo <= 0) zakoncz(true);
        }, 1000);

        function zakoncz(przekroczony) {
            if (koniec) return;
            koniec = true;
            clearInterval(timer);
            sprawdzBtn.hidden = true;
            p.pola.forEach(x => { x.disabled = true; });
            wyslij(z.id, przekroczony ? '' : odpowiedz(p))
                .then(w => {
                    if (przekroczony) w.poprawna = false;
                    pokazWynik(wynik, w, przekroczony ? 'Czas minął.' : 'Błędnie.');
                    return w.poprawna;
                })
                .catch(() => { wynik.textContent = 'Błąd połączenia.'; return false; })
                .then(gotowe);
        }
        sprawdzBtn.addEventListener('click', () => zakoncz(false));
        przyEnter(p.pola, () => zakoncz(false));
        setTimeout(() => p.pola[0]?.focus(), 0);
        return karta;
    }

    function czas() {
        const wstep = `Na każde zadanie masz ${LIMIT_CZASU} sekund. Po upływie czasu zadanie liczy się jako błędne.`;
        panel('cw-czas', 'Czas', wstep, (k, lista) => {
            const start = przycisk('Zacznij');
            start.addEventListener('click', () => sekwencja(k, lista, zadanieNaCzas, czas, ETYKIETY_CZASU));
            k.append(start);
        });
    }

    const uruchom = { 'panel-wypelnij': wypelnij, 'panel-czas': czas, 'panel-blad': blad };
    const kafle = document.querySelectorAll('.kafel-cw');
    kafle.forEach(kafel => kafel.addEventListener('click', () => {
        const id = kafel.dataset.panel;
        clearInterval(timer);
        kafle.forEach(x => x.classList.toggle('aktywny', x === kafel));
        document.querySelectorAll('.cw-panel').forEach(p => { p.hidden = p.id !== id; });
        uruchom[id]?.();
        $(id).scrollIntoView({ behavior: 'smooth', block: 'start' });
    }));
})();
