// Œuf Frais : code navigateur, chargé avant .NET.
// - applique la palette et le thème mémorisés avant le premier affichage ;
// - enregistre le service worker et suit l'état hors ligne, les mises à jour et l'installation ;
// - fournit à .NET quelques fonctions (localStorage, calendrier, animation de la poule).
(() => {
    'use strict';

    const racine = document.documentElement;

    // localStorage peut être indisponible (navigation privée, stockage bloqué) : l'app doit
    // fonctionner quand même, simplement sans mémoriser les réglages.
    const lire = cle => { try { return localStorage.getItem(cle); } catch { return null; } };
    const ecrire = (cle, valeur) => { try { localStorage.setItem(cle, valeur); } catch { /* rien à faire */ } };

    // ===== Thème =====
    // Mêmes clés et valeurs que l'ancienne app MAUI (theme_palette = Miel|Lagon|Corail, theme_mode = Systeme|Clair|Sombre).

    const palettes = { Miel: 'miel', Lagon: 'lagon', Corail: 'corail' };
    const modes = { Clair: 'clair', Sombre: 'sombre' };

    // La barre d'état (Android) et la barre d'onglets de Safari prennent la couleur de fond de l'app.
    function majCouleurBarre() {
        const fond = getComputedStyle(racine).getPropertyValue('--couleur-fond').trim();
        const meta = document.querySelector('meta[name="theme-color"]');
        if (fond && meta) meta.content = fond;
    }

    function appliquerTheme(palette, mode) {
        racine.dataset.palette = palettes[palette] ?? 'miel';
        if (modes[mode]) racine.dataset.theme = modes[mode];
        else delete racine.dataset.theme;
        majCouleurBarre();
    }

    appliquerTheme(lire('theme_palette'), lire('theme_mode'));
    matchMedia('(prefers-color-scheme: dark)').addEventListener('change', majCouleurBarre);

    // ===== Navigateur trop ancien =====
    // .NET 10 exige WebAssembly SIMD (Safari 16.4 et plus, soit iPhone 8 et suivants). Sans lui,
    // l'écran de chargement tournerait indéfiniment : on l'explique plutôt.
    const simd = typeof WebAssembly === 'object' && WebAssembly.validate(new Uint8Array(
        [0, 97, 115, 109, 1, 0, 0, 0, 1, 5, 1, 96, 0, 1, 123, 3, 2, 1, 0, 10, 10, 1, 8, 0, 65, 0, 253, 15, 253, 98, 11]));
    if (!simd) {
        addEventListener('DOMContentLoaded', () => {
            document.getElementById('app').innerHTML =
                '<div class="page"><section class="carte"><h1 class="titre">🥚 Œuf Frais</h1>' +
                '<p>Ce navigateur est trop ancien pour Œuf Frais. Mettez à jour votre téléphone ' +
                '(iOS 16.4 ou plus récent) ou votre navigateur.</p></section></div>';
        });
    }

    // ===== Application installable =====

    const etat = {
        horsLigne: 'preparation',
        miseAJour: false,
        installable: false,
        aideIos: false,
        bandeauMasque: lire('pwa_bandeau_masque') === '1',
    };

    let abonne = null;            // DotNetObjectReference vers EtatPwa
    let invite = null;            // événement beforeinstallprompt mis de côté
    let enregistrement = null;    // ServiceWorkerRegistration
    let miseAJourDemandee = false;

    function publier() {
        abonne?.invokeMethodAsync('MettreAJourEtat',
            etat.horsLigne, etat.miseAJour, etat.installable, etat.aideIos, etat.bandeauMasque);
    }

    function changer(modifications) {
        Object.assign(etat, modifications);
        publier();
    }

    const estInstallee = () => matchMedia('(display-mode: standalone)').matches || navigator.standalone === true;
    const estIos = /iphone|ipad|ipod/i.test(navigator.userAgent)
        || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);   // iPad en mode « ordinateur »
    etat.aideIos = estIos && !estInstallee();

    // Android, Chrome, Edge : on garde l'invite du navigateur pour la déclencher depuis notre bouton.
    addEventListener('beforeinstallprompt', e => {
        e.preventDefault();
        invite = e;
        changer({ installable: true });
    });

    addEventListener('appinstalled', () => {
        invite = null;
        changer({ installable: false, aideIos: false });
    });

    // Un nouveau service worker installé alors qu'un autre contrôle déjà la page : c'est une
    // nouvelle version, qui attend que l'utilisateur accepte de recharger.
    function surveiller(worker) {
        worker?.addEventListener('statechange', () => {
            if (worker.state === 'installed' && navigator.serviceWorker.controller) changer({ miseAJour: true });
        });
    }

    async function enregistrer() {
        if (!('serviceWorker' in navigator) || !isSecureContext) {
            changer({ horsLigne: 'indisponible' });
            return;
        }

        try {
            // Chemin relatif : il suit la balise <base>, donc le sous-dossier de GitHub Pages.
            enregistrement = await navigator.serviceWorker.register('service-worker.js', { updateViaCache: 'none' });
        } catch {
            changer({ horsLigne: 'indisponible' });
            return;
        }

        if (enregistrement.waiting && navigator.serviceWorker.controller) changer({ miseAJour: true });
        surveiller(enregistrement.installing);
        enregistrement.addEventListener('updatefound', () => surveiller(enregistrement.installing));

        navigator.serviceWorker.addEventListener('controllerchange', () => {
            if (miseAJourDemandee) location.reload();
        });

        // Une PWA installée peut rester des jours en mémoire : on cherche une nouvelle version
        // à chaque retour au premier plan.
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') enregistrement.update().catch(() => { });
        });

        // Un service worker ne devient actif qu'une fois tous les fichiers de l'app en cache.
        await navigator.serviceWorker.ready;
        changer({ horsLigne: 'prete' });
        navigator.storage?.persist?.().catch(() => { });
    }

    // Après le chargement de la page : le préchargement ne doit pas ralentir le premier affichage.
    addEventListener('load', () => { enregistrer(); });

    async function installer() {
        if (!invite) return;
        const e = invite;
        invite = null;
        e.prompt();
        await e.userChoice.catch(() => { });
        changer({ installable: false });
    }

    function mettreAJour() {
        const enAttente = enregistrement?.waiting;
        if (!enAttente) {
            location.reload();
            return;
        }
        miseAJourDemandee = true;
        enAttente.postMessage('SKIP_WAITING');
    }

    function masquerBandeau() {
        ecrire('pwa_bandeau_masque', '1');
        changer({ bandeauMasque: true });
    }

    function abonner(reference) {
        abonne = reference;
        publier();
    }

    // ===== Calendrier =====
    // Le champ date est transparent, posé sur la date affichée dans la langue de l'app.
    // Sur ordinateur, un clic ne l'ouvre pas tout seul : showPicker() s'en charge.
    function ouvrirCalendrier(champ) {
        try { champ.showPicker?.(); } catch { /* le navigateur ouvre son propre sélecteur */ }
    }

    // ===== La poule qui traverse l'écran =====
    // Port fidèle de l'animation de l'ancienne app MAUI : petits pas dandinés, regroupés
    // en tronçons, avec une pause où elle regarde autour d'elle, puis demi-tour.

    const poules = new WeakMap();

    const pause = (ms, signal) => new Promise((resolve, reject) => {
        signal.throwIfAborted();
        const surAbandon = () => { clearTimeout(minuteur); reject(signal.reason); };
        const minuteur = setTimeout(() => { signal.removeEventListener('abort', surAbandon); resolve(); }, ms);
        signal.addEventListener('abort', surAbandon, { once: true });
    });

    async function marcher(poule, piste, signal) {
        const sinus = 'cubic-bezier(0.37, 0, 0.63, 1)';    // Easing.SinInOut
        const cubique = 'cubic-bezier(0.33, 1, 0.68, 1)';  // Easing.CubicOut
        let x = 0, y = 0, r = 0, sens = 1;
        const transformation = () => `translate(${x}px, ${y}px) rotate(${r}deg) scaleX(${sens})`;

        async function aller(nx, ny, nr, duree, easing) {
            const depart = transformation();
            x = nx; y = ny; r = nr;
            const animation = poule.animate([{ transform: depart }, { transform: transformation() }], { duration: duree, easing });
            poule.style.transform = transformation();
            await animation.finished;
            signal.throwIfAborted();
        }

        await pause(350, signal);

        const pas = 14;
        const troncons = [4, 4, 3, 3];
        let versLaDroite = true;

        for (;;) {
            // Juste après l'ouverture de la page, la piste n'a pas encore de largeur.
            for (let essai = 0; essai < 10 && piste.clientWidth <= 0; essai++) await pause(50, signal);
            if (piste.clientWidth <= 0) return;

            const largeurPoule = poule.offsetWidth || 40;
            const depart = versLaDroite ? -largeurPoule : piste.clientWidth;
            const arrivee = versLaDroite ? piste.clientWidth : -largeurPoule;

            sens = versLaDroite ? -1 : 1;   // l'emoji regarde à gauche : on le retourne pour aller à droite
            x = depart; y = 0; r = 0;
            poule.style.transform = transformation();

            let i = 0;
            for (const longueur of troncons) {
                for (let j = 0; j < longueur; j++) {
                    i++;
                    const enLAir = i % 2 === 0;
                    await aller(depart + (arrivee - depart) * i / pas, enLAir ? -7 : 0, enLAir ? 6 : -6, 320, sinus);
                }

                if (i >= pas) break;

                // Pause : elle se pose, regarde de part et d'autre, puis repart.
                await aller(x, 0, 0, 120, cubique);
                await pause(280, signal);
                await aller(x, 0, -14, 160, sinus);
                await aller(x, 0, 14, 220, sinus);
                await aller(x, 0, 0, 140, sinus);
                await pause(200, signal);
            }

            await aller(x, 0, 0, 100, cubique);
            versLaDroite = !versLaDroite;
            await pause(500, signal);
        }
    }

    function arreterPoule(poule) {
        poules.get(poule)?.abort();
        poules.delete(poule);
        poule.getAnimations().forEach(a => a.cancel());
    }

    function demarrerPoule(poule) {
        arreterPoule(poule);
        if (matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        const controle = new AbortController();
        poules.set(poule, controle);
        marcher(poule, poule.parentElement, controle.signal).catch(() => { /* arrêtée */ });
    }

    window.oeufFrais = {
        lire,
        ecrire,
        appliquerTheme,
        definirLangue: code => { racine.lang = code; },
        ouvrirCalendrier,
        poule: { demarrer: demarrerPoule, arreter: arreterPoule },
        pwa: { abonner, installer, mettreAJour, masquerBandeau },
    };
})();
