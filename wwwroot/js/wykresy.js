(function () {
    const kontLinia = document.getElementById('wykres-skutecznosc');
    const kontSlupki = document.getElementById('wykres-liczba');
    const info = document.getElementById('wykres-info');
    if (!kontLinia || !kontSlupki || !info) return;

    const NS = 'http://www.w3.org/2000/svg';
    const W = 640, H = 260, LEWY = 44, PRAWY = 14, GORA = 14, DOL = 36;
    const SZER = W - LEWY - PRAWY, WYS = H - GORA - DOL, PODSTAWA = H - DOL;
    const KOLOR = '#6d28d9', DOBRY = '#2f9e63', ZLY = '#d6336c', SIATKA = '#e5defa', TEKST = '#6b7280';

    function el(rodzic, nazwa, atrybuty, tekst) {
        const e = document.createElementNS(NS, nazwa);
        Object.entries(atrybuty).forEach(([klucz, wartosc]) => e.setAttribute(klucz, wartosc));
        if (tekst !== undefined) e.textContent = tekst;
        rodzic.append(e);
        return e;
    }

    function zPodpowiedzia(rodzic, nazwa, atrybuty, podpowiedz) {
        const e = el(rodzic, nazwa, atrybuty);
        el(e, 'title', {}, podpowiedz);
        return e;
    }

    function nowySvg(opis) {
        const svg = document.createElementNS(NS, 'svg');
        svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
        svg.setAttribute('role', 'img');
        svg.setAttribute('aria-label', opis);
        return svg;
    }

    function siatka(svg, max, sufiks = '') {
        for (let i = 0; i <= 4; i++) {
            const y = PODSTAWA - WYS * i / 4;
            el(svg, 'line', { x1: LEWY, x2: W - PRAWY, y1: y, y2: y, stroke: SIATKA, 'stroke-width': 1 });
            el(svg, 'text', { x: LEWY - 6, y: y + 4, 'text-anchor': 'end', fill: TEKST, 'font-size': 11 }, Math.round(max * i / 4) + sufiks);
        }
    }

    function etykietaX(svg, i, x, tekst) {
        if (i % 2 === 0) el(svg, 'text', { x, y: PODSTAWA + 16, 'text-anchor': 'middle', fill: TEKST, 'font-size': 11 }, tekst);
    }

    function rysujLinie(dni) {
        const svg = nowySvg('Wykres skuteczności odpowiedzi w ostatnich dniach');
        siatka(svg, 100, '%');
        const xDla = i => LEWY + SZER * (i + 0.5) / dni.length;
        const yDla = procent => PODSTAWA - WYS * procent / 100;

        let sciezka = '', ciagla = false;
        dni.forEach((dzien, i) => {
            if (dzien.procent === null) { ciagla = false; return; }
            sciezka += `${ciagla ? ' L ' : ' M '}${xDla(i)} ${yDla(dzien.procent)}`;
            ciagla = true;
        });
        if (sciezka) el(svg, 'path', { d: sciezka, fill: 'none', stroke: KOLOR, 'stroke-width': 3, 'stroke-linejoin': 'round' });

        dni.forEach((dzien, i) => {
            etykietaX(svg, i, xDla(i), dzien.etykieta);
            if (dzien.procent === null) return;
            zPodpowiedzia(svg, 'circle', { cx: xDla(i), cy: yDla(dzien.procent), r: 5, fill: KOLOR },
                `${dzien.etykieta}: ${dzien.procent}% (${dzien.poprawne} z ${dzien.razem})`);
        });
        kontLinia.append(svg);
    }

    function rysujSlupki(dni) {
        const svg = nowySvg('Wykres liczby rozwiązanych pytań i zadań w ostatnich dniach');
        const max = Math.max(4, Math.ceil(Math.max(...dni.map(dzien => dzien.razem)) / 4) * 4);
        siatka(svg, max);
        const krok = SZER / dni.length, grubosc = krok * 0.6;

        dni.forEach((dzien, i) => {
            const x = LEWY + krok * i + (krok - grubosc) / 2;
            const bledne = dzien.razem - dzien.poprawne;
            const podpowiedz = `${dzien.etykieta}: ${dzien.razem} (poprawne ${dzien.poprawne}, błędne ${bledne})`;

            let gora = PODSTAWA;
            [[dzien.poprawne, DOBRY], [bledne, ZLY]].forEach(([liczba, kolor]) => {
                const wysokosc = WYS * liczba / max;
                if (wysokosc <= 0) return;
                gora -= wysokosc;
                zPodpowiedzia(svg, 'rect', { x, y: gora, width: grubosc, height: wysokosc, fill: kolor, rx: 2 }, podpowiedz);
            });
            etykietaX(svg, i, x + grubosc / 2, dzien.etykieta);
        });
        kontSlupki.append(svg);
    }

    fetch(kontLinia.dataset.url)
        .then(r => { if (!r.ok) throw new Error(); return r.json(); })
        .then(dni => {
            if (dni.every(dzien => dzien.razem === 0)) {
                info.textContent = 'Brak danych z ostatnich 14 dni. Rozwiąż quiz lub zadania, a wykresy pojawią się tutaj.';
                return;
            }
            rysujLinie(dni);
            rysujSlupki(dni);
            info.textContent = 'Na drugim wykresie kolor zielony oznacza poprawne odpowiedzi, a różowy błędne. Najedź kursorem na punkt lub słupek, aby zobaczyć szczegóły.';
        })
        .catch(() => { info.textContent = 'Nie udało się wczytać danych do wykresów.'; });
})();
