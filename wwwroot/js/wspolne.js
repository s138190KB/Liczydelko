window.naglowkiJson = () => {
    const naglowki = { 'Content-Type': 'application/json' };
    const token = document.querySelector('meta[name="csrf-token"]')?.content;
    if (token) naglowki.RequestVerificationToken = token;
    return naglowki;
};

window.konfetti = () => {
    const kolory = ['#ffd43b', '#fab005', '#6d28d9', '#a78bfa', '#c4b5fd', '#fff3bf'];
    const warstwa = document.createElement('div');
    warstwa.className = 'konfetti';
    for (let i = 0; i < 48; i++) {
        const kawalek = document.createElement('i');
        Object.assign(kawalek.style, {
            left: `${Math.random() * 100}%`,
            background: kolory[Math.floor(Math.random() * kolory.length)],
            animationDelay: `${Math.random() * 0.6}s`,
            animationDuration: `${1.8 + Math.random() * 1.4}s`
        });
        warstwa.append(kawalek);
    }
    document.body.append(warstwa);
    setTimeout(() => warstwa.remove(), 4200);
};

(function () {
    const $ = id => document.getElementById(id);

    const menu = $('menu'), hamb = $('hamb');
    if (menu && hamb) {
        hamb.addEventListener('click', () => {
            const otwarte = menu.classList.toggle('otwarte');
            hamb.classList.toggle('otwarte', otwarte);
            hamb.setAttribute('aria-expanded', otwarte);
        });
    }

    const przyciskiKart = document.querySelectorAll('[data-karta]');
    przyciskiKart.forEach(b => b.addEventListener('click', () => {
        przyciskiKart.forEach(x => x.classList.toggle('aktywny', x === b));
        document.querySelectorAll('.karta-tresc').forEach(t => { t.hidden = t.id !== `karta-${b.dataset.karta}`; });
    }));

    const kontener = $('liczydlo');
    if (!kontener) return;

    const NS = 'http://www.w3.org/2000/svg';
    const KORALIKI_NA_PRECIE = 9, SZEROKOSC_KORALIKA = 21, PRETY = 5, W = 360, H = 200;

    const svg = document.createElementNS(NS, 'svg');
    svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
    svg.setAttribute('role', 'img');
    svg.setAttribute('aria-label', 'Interaktywne liczydło');

    const dodaj = (tag, atrybuty) => {
        const e = document.createElementNS(NS, tag);
        Object.entries(atrybuty).forEach(([nazwa, wartosc]) => e.setAttribute(nazwa, wartosc));
        svg.append(e);
        return e;
    };

    dodaj('rect', { x: 4, y: 4, width: W - 8, height: H - 8, rx: 16, fill: '#4c1d95', stroke: '#ffd43b', 'stroke-width': 4 });

    const stan = Array(PRETY).fill(0);
    const koraliki = [];
    const opis = document.createElement('div');
    opis.className = 'liczydlo-liczba';

    const pozycja = (j, ile) => j < KORALIKI_NA_PRECIE - ile
        ? 20 + j * SZEROKOSC_KORALIKA
        : W - 20 - (KORALIKI_NA_PRECIE - j) * SZEROKOSC_KORALIKA;

    function odswiez() {
        koraliki.forEach((pret, r) => pret.forEach((koralik, j) => {
            koralik.style.transform = `translateX(${pozycja(j, stan[r])}px)`;
        }));
        opis.textContent = `Liczba na liczydle: ${stan.join('')}`;
    }

    for (let r = 0; r < PRETY; r++) {
        const y = 36 + r * 34;
        dodaj('line', { x1: 16, x2: W - 16, y1: y, y2: y, stroke: '#c4b5fd', 'stroke-width': 3, 'stroke-linecap': 'round' });
        koraliki.push(Array.from({ length: KORALIKI_NA_PRECIE }, (_, j) => {
            const koralik = dodaj('rect', {
                x: 0, y: y - 13, width: SZEROKOSC_KORALIKA - 2, height: 26, rx: 11,
                fill: j % 2 ? '#ffd43b' : '#fab005', class: 'koralik'
            });
            koralik.addEventListener('click', () => {
                stan[r] = j < KORALIKI_NA_PRECIE - stan[r] ? KORALIKI_NA_PRECIE - j : KORALIKI_NA_PRECIE - j - 1;
                odswiez();
            });
            return koralik;
        }));
    }

    kontener.append(svg, opis);
    odswiez();
})();
