(function () {
    const kontener = document.getElementById('quiz');
    if (!kontener) return;

    const $ = id => document.getElementById(id);
    const wszystkie = JSON.parse($('dane-quiz').textContent);
    const lekcjaId = parseInt(kontener.dataset.lekcja, 10);
    const wybor = kontener.dataset.trudnosci === '1';
    const LITERY = ['A', 'B', 'C', 'D'];
    const PROG_SUKCESU = 60;
    const sekundy = od => Math.round((Date.now() - od) / 1000);

    let trudnosc = parseInt(kontener.dataset.polecany, 10) || 1;
    let pytania = [], numer = 0, odpowiedzi = [], zablokowane = false;
    let startPytania = 0, startCalosc = 0, licznik = null;

    function przycisk(tekst, cls = 'przycisk') {
        const b = document.createElement('button');
        b.type = 'button';
        b.className = cls;
        b.textContent = tekst;
        return b;
    }

    const lista = () => wszystkie.filter(p => !wybor || p.trudnosc === trudnosc);
    const pokazEkran = nazwa => ['start', 'gra', 'koniec'].forEach(n => { $('quiz-' + n).hidden = n !== nazwa; });

    function odswiezInfo() {
        const ile = lista().length;
        $('quiz-info').textContent = ile ? `Liczba pytań: ${ile}` : 'Brak pytań na tym poziomie.';
        $('quiz-zacznij').disabled = !ile;
    }

    function mieszaj(tab) {
        for (let i = tab.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [tab[i], tab[j]] = [tab[j], tab[i]];
        }
        return tab;
    }

    const przyciskiTrudnosci = document.querySelectorAll('#quiz .trud');
    przyciskiTrudnosci.forEach(b => b.addEventListener('click', () => {
        trudnosc = parseInt(b.dataset.t, 10);
        przyciskiTrudnosci.forEach(x => x.classList.toggle('aktywny', x === b));
        odswiezInfo();
    }));

    function zacznij() {
        pytania = mieszaj(lista());
        if (!pytania.length) return;
        numer = 0;
        odpowiedzi = [];
        startCalosc = Date.now();
        $('quiz-czas').textContent = '0';
        $('quiz-calosc').textContent = '0';
        clearInterval(licznik);
        licznik = setInterval(() => {
            $('quiz-czas').textContent = sekundy(startPytania);
            $('quiz-calosc').textContent = sekundy(startCalosc);
        }, 1000);
        pokazEkran('gra');
        pokaz();
    }

    function pokaz() {
        const p = pytania[numer];
        const wiele = p.poprawna.length > 1;
        zablokowane = false;
        startPytania = Date.now();
        $('quiz-nr').textContent = `${numer + 1} z ${pytania.length}`;
        $('quiz-pasek').style.width = `${100 * numer / pytania.length}%`;
        $('quiz-tresc').textContent = p.tresc;
        $('quiz-dalej').hidden = true;

        const okno = $('quiz-odp');
        okno.innerHTML = '';
        if (wiele) {
            const info = document.createElement('p');
            info.className = 'meta';
            info.textContent = 'Możesz zaznaczyć więcej niż jedną odpowiedź.';
            okno.append(info);
        }
        p.odp.forEach((tekst, i) => {
            const b = przycisk(`${LITERY[i]}. ${tekst}`, 'odp');
            b.dataset.litera = LITERY[i];
            b.addEventListener('click', () => {
                if (zablokowane) return;
                if (wiele) b.classList.toggle('wybrana'); else wybierz([LITERY[i]], p);
            });
            okno.append(b);
        });
        if (wiele) {
            const zatwierdz = przycisk('Zatwierdź odpowiedź');
            zatwierdz.addEventListener('click', () => {
                const wybrane = [...okno.querySelectorAll('.odp.wybrana')].map(x => x.dataset.litera);
                if (!wybrane.length) return;
                zatwierdz.hidden = true;
                wybierz(wybrane, p);
            });
            okno.append(zatwierdz);
        }
    }

    function wybierz(wybrane, p) {
        if (zablokowane) return;
        zablokowane = true;
        odpowiedzi.push({ pytanieId: p.id, wybrana: [...wybrane].sort().join(''), czas: sekundy(startPytania) });
        document.querySelectorAll('#quiz-odp .odp').forEach(b => {
            const litera = b.dataset.litera;
            b.disabled = true;
            b.classList.remove('wybrana');
            if (p.poprawna.includes(litera)) b.classList.add('dobra');
            else if (wybrane.includes(litera)) b.classList.add('zla');
        });
        $('quiz-dalej').hidden = false;
        $('quiz-dalej').textContent = numer + 1 < pytania.length ? 'Dalej' : 'Zakończ quiz';
    }

    $('quiz-dalej').addEventListener('click', () => {
        numer++;
        if (numer < pytania.length) pokaz(); else zakoncz();
    });

    function opisWyniku(w) {
        const czesci = [`Wynik: ${w.punkty} z ${w.razem}.`];
        if (lekcjaId > 0) {
            if (typeof w.nowePunkty === 'number') {
                czesci.push(`Zdobyte punkty: ${w.nowePunkty}` +
                    (w.nowePunkty === 0 ? ' (punkty dostajesz tylko za pytania, na które odpowiadasz poprawnie po raz pierwszy).' : '.'));
            }
            czesci.push(w.zaliczona ? 'Ten poziom trudności jest zaliczony.' : 'Poziom niezaliczony, spróbuj ponownie.');
        }
        if (w.awans) czesci.push(`Awansujesz na level: ${w.level}!`);
        if (w.odznaki?.length) czesci.push(`Nowa odznaka: ${w.odznaki.join(', ')}!`);
        return czesci.join(' ');
    }

    function zakoncz() {
        clearInterval(licznik);
        $('quiz-pasek').style.width = '100%';
        pokazEkran('koniec');
        $('quiz-zmien').hidden = !wybor;
        $('quiz-ponow').disabled = true;
        $('quiz-wynik').textContent = 'Zapisywanie wyniku...';
        fetch('/Nauka/Zakoncz', {
            method: 'POST',
            headers: window.naglowkiJson(),
            body: JSON.stringify({ lekcjaId, trudnosc: wybor ? trudnosc : 1, odpowiedzi })
        })
            .then(r => { if (!r.ok) throw new Error(); return r.json(); })
            .then(w => {
                $('quiz-wynik').textContent = opisWyniku(w);
                const dobryWynik = w.razem > 0 && w.punkty * 100 >= w.razem * PROG_SUKCESU;
                if (w.awans || w.odznaki?.length || dobryWynik) window.konfetti?.();
            })
            .catch(() => { $('quiz-wynik').textContent = 'Nie udało się zapisać wyniku.'; })
            .finally(() => { $('quiz-ponow').disabled = false; });
    }

    $('quiz-zacznij').addEventListener('click', zacznij);
    $('quiz-ponow').addEventListener('click', zacznij);
    $('quiz-zmien').addEventListener('click', () => { pokazEkran('start'); odswiezInfo(); });

    odswiezInfo();
    pokazEkran('start');
})();
