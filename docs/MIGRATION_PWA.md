# Plan de migration : Œuf Frais en PWA Angular 22, 100 % hors ligne

> Branche de travail : `claude/migration-pwa` · Point de départ : `main` @ `4746061`
> (application .NET MAUI 10 : Android, iOS, Windows)

## 1. Objectif et contraintes

- Distribuer l'application **sans compte Apple Developer (99 €/an) ni Google Play Console (25 $)** :
  une PWA qu'on installe depuis le navigateur, sur iPhone comme sur Android (et sur ordinateur).
- **Toutes les fonctionnalités disponibles hors ligne** après la première visite : calcul de
  fraîcheur, décodage du code coquille, conseils, réglages, 7 langues, et lecture photo (OCR)
  de la DCR et du code coquille.
- **Coût d'exploitation : 0 €.** Le dépôt devient public, il est donc hébergé sur GitHub Pages.
- Aucune régression sur les règles métier (règlement (CE) n° 589/2008, ANSES). La logique étant
  réécrite en TypeScript, des **tests de parité générés depuis le code C# actuel** le garantissent
  (phase 2).

## 2. Socle technique

| Sujet | Choix | Pourquoi |
|---|---|---|
| Framework | **Angular 22** (22.2 à ce jour), composants standalone, signals, sans zone.js, OnPush (défauts d'un nouveau projet v22), nouveau control flow `@if` / `@for` | Demandé ; signals = rafraîchissement fin et automatique (langue, thème) |
| Service worker | `@angular/service-worker` (ngsw) via `ng add @angular/pwa` | Officiel, précharge les fichiers listés avec contrôle par empreinte, gère les mises à jour (`SwUpdate`) |
| Traductions | Service maison à base de signals + 7 fichiers JSON convertis depuis les `.resx` | Changement de langue **instantané, sans rechargement**, comme aujourd'hui. `@angular/localize` produit un build par langue : 7 applications à précharger et un changement de langue = changement d'URL |
| Formatage des dates | `Intl.DateTimeFormat` du navigateur | Toutes les langues sans données à embarquer, hors ligne |
| Sélecteur de date | Datepicker **Angular Material** (`NativeDateAdapter`, mode `touchUi` sur mobile) | Calendrier dans la **langue de l'app** (`DateAdapter.setLocale`) : on garde le comportement obtenu sous MAUI en forçant la locale native, ce qu'un `<input type="date">` ne permet pas |
| OCR | **Tesseract.js 7** auto-hébergé (WebAssembly), modèle `fra` | Seul moteur OCR utilisable hors ligne sur iOS comme sur Android |
| Tests | **Vitest** (unitaires, runner par défaut d'Angular) + **Playwright** (bout en bout, dont hors ligne) | |
| Node | **Node 24 LTS** (Angular 22 exige Node ^22.22.3 ou ^24.15) | |

## 3. Correspondance fonctionnelle MAUI → PWA

| Fonction | Aujourd'hui (MAUI) | Demain (Angular) | Hors ligne |
|---|---|---|---|
| Calcul de fraîcheur (ponte / DCR) | `EggCalculator.cs` | `domaine/egg-calculator.ts`, porté ligne à ligne | ✅ |
| Décodage du code coquille | `ShellCodeParser.cs` | `domaine/shell-code-parser.ts` | ✅ |
| Extraction de date OCR | `DateOcrParser.cs` | `domaine/date-ocr-parser.ts` | ✅ |
| Onglet Conserver | XAML | composant | ✅ |
| 7 langues, changement en direct | `.resx` + `LocalizationResourceManager` | JSON + `I18nService` (signals) | ✅ |
| Palettes Miel / Lagon / Corail, clair / sombre / système | `ThemeService` → `DynamicResource` | variables CSS + `prefers-color-scheme`, `ThemeService` (signals) | ✅ |
| Préférences | `Preferences` MAUI | `localStorage` | ✅ |
| Navigation 4 onglets | `Shell` + `TabBar` | barre d'onglets en bas, routes `''`, `code`, `conserver`, `reglages` | ✅ |
| Sélecteur de date | `DatePicker` + forçage de la locale native par plateforme | Material datepicker, locale = langue de l'app | ✅ |
| Choix entre plusieurs dates lues | `DisplayActionSheetAsync` | `<dialog>` en feuille du bas | ✅ |
| Alertes | `DisplayAlertAsync` | `<dialog>` | ✅ |
| Poule qui marche | animations MAUI en C# | animation CSS, coupée si `prefers-reduced-motion` | ✅ |
| Photo | `MediaPicker` | `<input type="file" accept="image/*" capture="environment">` : appareil photo direct sur iOS et Android, sélecteur de fichier sur PC | ✅ |
| OCR | `Plugin.Maui.OCR` (Vision / ML Kit / Windows.Media.Ocr) | Tesseract.js auto-hébergé | ✅ |
| Icône, écran de démarrage | `MauiIcon`, `MauiSplashScreen` | `manifest.webmanifest`, icônes PNG 192/512 + maskable, `apple-touch-icon` | ✅ |
| Mises à jour | stores | automatiques, avec bandeau « Nouvelle version — Mettre à jour » | nécessite le réseau, par nature |

### Pistes d'évolution du README, revues pour le web

- **Suivi de plusieurs boîtes** : `localStorage` ou IndexedDB → fonctionne hors ligne.
- **Notification avant la DCR** : le web ne sait pas programmer une notification locale hors
  ligne (Web Push exige un serveur et le réseau ; sur iOS, uniquement pour une PWA installée,
  iOS 16.4+). Remplacement proposé : bouton « Ajouter au calendrier » qui génère un fichier
  `.ics` côté client (rappel à DCR − 2 jours). 100 % hors ligne, rappel géré par le calendrier
  du téléphone.
- **Scan du code-barres de la boîte** : `BarcodeDetector` n'existe pas dans Safari ; passer par
  une bibliothèque WASM auto-hébergée (zxing-wasm) → hors ligne.

## 4. Architecture cible

```
(racine du dépôt)
angular.json
package.json, package-lock.json
tsconfig.json, tsconfig.app.json, tsconfig.spec.json
ngsw-config.json                 ce que le service worker précharge
playwright.config.ts
public/                          copié tel quel dans le build
  manifest.webmanifest
  icons/                         192, 512, 512 maskable, apple-touch-icon 180, icônes d'onglets (svg)
  fonts/                         Open Sans en woff2
  i18n/                          fr.json, en.json, de.json, es.json, it.json, pt.json, nl.json
src/
  index.html
  main.ts
  styles.css                     jetons de couleur : 3 palettes × clair / sombre
  app/
    app.config.ts                provideServiceWorker, provideRouter, adaptateur de dates Material
    app.routes.ts                4 routes en chargement différé
    app.ts                       mise en page, barre d'onglets, bandeaux
    domaine/                     TypeScript pur, sans Angular : port de Services/*.cs
      jours.ts                   arithmétique en jours calendaires
      egg-calculator.ts          + .spec.ts
      shell-code-parser.ts       + .spec.ts
      date-ocr-parser.ts         + .spec.ts
      parite.spec.ts             compare au jeu de référence produit par le C#
    i18n/i18n.service.ts
    theme/theme.service.ts
    preferences/preferences.service.ts
    pwa/                         mise à jour, installation, « prête hors ligne »
    ocr/                         ocr.service.ts, pretraitement.ts
    pages/                       fraicheur/, code-coquille/, conserver/, reglages/
    partage/                     carte, dialogue, poule-qui-marche
e2e/                             Playwright, dont le parcours hors ligne
  fixtures/                      photos de référence (boîtes, coquilles)
tools/
  resx-vers-json.mjs             conversion unique des .resx
  parite/                        mini-programme C# qui produit parite.json à partir du code MAUI
.github/workflows/
  ci.yml                         tests à chaque push et pull request
  pages.yml                      build + déploiement GitHub Pages
maui/                            ancienne app : référence pendant le portage, supprimée en phase 6
```

Les fichiers Tesseract ne sont pas versionnés : `angular.json` les copie depuis `node_modules`
au build (section `assets`), la version est donc figée par `package-lock.json`.

## 5. Phases

Chaque phase = un ou plusieurs commits sur `claude/migration-pwa`, qui compilent et passent
les tests. Taille indicative : **S** (≤ ½ jour), **M** (~1 jour), **L** (2–3 jours).

### Phase 0 — Préparation et passage en public (S)

- [ ] Taguer l'état actuel : `git tag v1-maui` sur `main` (point de retour garanti).
- [ ] Avant de rendre le dépôt public :
  - [x] historique parcouru : aucun secret (clé, jeton, mot de passe) ;
  - [ ] retirer `.claude/settings.local.json` du suivi et l'ajouter au `.gitignore` (réglages
        locaux de Claude Code, avec des chemins de votre poste) ;
  - [ ] vérifier les adresses e-mail des commits : elles deviennent publiques ;
  - [ ] choisir une licence (sans fichier `LICENSE`, le code est visible mais pas réutilisable).
- [ ] `git mv` de l'app MAUI dans `maui/` : l'application Angular occupe la racine.

### Phase 1 — Squelette Angular, PWA et mise en ligne (M)

La mise en ligne arrive tôt exprès : un service worker n'est actif qu'en HTTPS (ou sur
`localhost`), donc tester sur un vrai téléphone demande une URL publique dès le début.

- [ ] `ng new` (Angular 22, CSS, sans SSR), puis `ng add @angular/pwa`.
- [ ] `ngsw-config.json` : passer **tous** les groupes en `installMode: "prefetch"`. Le
      schematic met les ressources en `lazy` par défaut ; elles ne seraient alors en cache
      qu'après avoir été affichées une fois en ligne.
- [ ] `index.html` :
  - écran de chargement aux couleurs de l'app (l'œuf de `appiconfg.svg`) ;
  - `viewport-fit=cover`, `theme-color`, `apple-touch-icon`, `apple-mobile-web-app-capable`,
    `apple-mobile-web-app-status-bar-style` ;
  - petit script inline qui pose palette et thème depuis `localStorage` avant le démarrage
    d'Angular, pour éviter un flash de mauvaises couleurs.
- [ ] `manifest.webmanifest` : nom « Œuf Frais », `short_name`, `lang`, `display: standalone`,
      `start_url` et `scope` relatifs (`./`), `background_color` et `theme_color` (#FF6F59),
      icônes 192, 512 et 512 maskable générées depuis `maui/Resources/AppIcon/*.svg`.
- [ ] `styles.css` : les 7 jetons (`--couleur-fond`, `--couleur-accent`, …) × 3 palettes ×
      clair/sombre, recopiés de `Palettes.cs`, choisis par `data-palette` / `data-theme` sur
      `<html>` ; mode « Système » via `@media (prefers-color-scheme: dark)`.
- [ ] Mise en page : barre d'onglets en bas avec les icônes `icon_*.svg` existantes, marges
      `env(safe-area-inset-*)` pour l'encoche et la barre d'accueil de l'iPhone.
- [ ] Workflows `ci.yml` et `pages.yml` (voir section 6) ; première version en ligne.

### Phase 2 — Cœur métier en TypeScript et tests de parité (L)

- [ ] `tools/parite/` : mini-programme console C# qui compile les fichiers de
      `maui/Services/` (`EggCalculator`, `ShellCodeParser`, `DateOcrParser`) avec un faux
      `Preferences` en mémoire, et écrit `parite.json` : pour une date « aujourd'hui » fixée,
      toutes les dates de ponte de J−60 à J+5, les codes coquille valides et invalides, et des
      textes OCR d'exemple, avec les résultats attendus (statut, dates, âge, jours avant DCR,
      clés des conseils, code trouvé, dates candidates et scores). Le JSON est versionné : il
      reste après la suppression de `maui/`.
- [ ] Port en TypeScript, en gardant les noms du C# (`depuisPonte`, `depuisDcr`, `trouverCode`,
      `extraire`…) pour qu'on puisse relire les deux côte à côte.
- [ ] Points de vigilance du portage :
  - **Dates** : JavaScript n'a pas de type « date sans heure ». Tout calcul d'écart se fait en
    jours calendaires via `Date.UTC(a, m, j)` ; une soustraction de deux minuits locaux donne
    un jour faux autour des changements d'heure (fin mars, fin octobre). Tests dédiés sur ces
    deux dates.
  - **Expressions régulières** : les lookbehind `(?<!\d)` passent en JavaScript (Safari 16.4+) ;
    `Normalize(FormD)` + suppression des `NonSpacingMark` devient
    `normalize('NFD').replace(/\p{Mn}/gu, '')` ; `ToUpperInvariant()` devient `toUpperCase()`.
  - **Couleurs** : `Color` MAUI → classes CSS par statut (`statut-extra-frais`, …), les
    teintes claires à 16 % via `color-mix()`.
- [ ] `tools/resx-vers-json.mjs` : conversion unique des 7 `.resx` vers `public/i18n/*.json`
      (le `.resx` neutre donne `fr.json`). Les gabarits `{0}` restent tels quels.
- [ ] `I18nService` :
  - langue courante en signal ; `t(cle, ...args)` remplace `GetString` ; `pluriel(...)` reprend
    la règle actuelle (n ≤ 1 → singulier) ;
  - mode « Système » : première langue de `navigator.languages` parmi les 7, sinon français
    (comme le repli actuel sur le `.resx` neutre) ;
  - langue active chargée avant le premier affichage (`provideAppInitializer`), sinon les clés
    brutes apparaîtraient une fraction de seconde.
- [ ] Tests Vitest :
  - `parite.spec.ts` : chaque cas de `parite.json` doit donner le même résultat ;
  - traductions : chaque clé de `fr.json` existe dans les 6 autres langues, avec les mêmes
    `{n}` ; une chaîne vide fait échouer le test.
- Écart assumé : les dates affichées suivent les formats natifs de chaque langue fournis par
  `Intl` (« Thursday, August 6, 2026 » en anglais) au lieu du gabarit unique `d MMMM yyyy`
  appliqué aujourd'hui à toutes les langues. La parité porte donc sur les dates elles-mêmes,
  pas sur leur libellé.

### Phase 3 — Portage des écrans (L)

- [ ] **Fraîcheur** : puces Ponte / DCR, texte d'aide, datepicker avec les mêmes bornes que
      `MainPage.AppliquerMode` (−120 j / aujourd'hui en mode ponte, −120 j / +60 j en mode DCR),
      champ en lecture seule qui ouvre le calendrier (la saisie clavier de `NativeDateAdapter`
      interprète mal les dates au format jour/mois), carte statut, dates clés, usages ✓ / ✕,
      test du verre d'eau si recommandé, avertissement.
- [ ] **Code coquille** : saisie (Entrée = décoder), résultat ou carte d'erreur, encart
      « Bon à savoir ».
- [ ] **Conserver** : règles, test du verre d'eau, DCR vs DLC, avertissement.
- [ ] **Réglages** : langue (Système + 7), palette, thème ; tout s'applique immédiatement,
      y compris au calendrier et au résultat déjà affiché (signals, plus besoin des contournements
      de `MainPage` pour rafraîchir le `DatePicker` et la carte résultat).
- [ ] `<html lang>` suit la langue choisie (lecteurs d'écran, césure).
- [ ] Thème Material branché sur nos jetons (`--mat-sys-primary: var(--couleur-accent)`, …)
      pour que le calendrier change avec la palette.
- [ ] Poule qui marche : animation CSS reprenant le trajet actuel (aller-retour, dandinement,
      pauses où elle regarde autour d'elle), arrêtée si `prefers-reduced-motion`.
- [ ] Composant `dialogue` (remplace `DisplayAlertAsync` et `DisplayActionSheetAsync`).

### Phase 4 — Hors ligne et installation (M)

- [ ] Polices Open Sans converties en `.woff2` et auto-hébergées. Aucune ressource externe :
      ni Google Fonts, ni CDN (le schematic Material ajoute des liens Google Fonts : à retirer).
- [ ] Mises à jour : `SwUpdate.versionUpdates` (`VERSION_READY`) → bandeau « Nouvelle version
      disponible — Mettre à jour » → `activateUpdate()` puis rechargement. `SwUpdate.unrecoverable`
      → rechargement forcé. Vérification à chaque retour au premier plan
      (`visibilitychange`) : une PWA installée reste parfois des jours en mémoire.
- [ ] Indicateur « ✓ Prête hors ligne » dans Réglages quand le service worker a tout
      préchargé : l'utilisateur sait quand il peut se passer du réseau.
- [ ] `navigator.storage.persist()` pour limiter l'éviction du cache.
- [ ] Installation :
  - Android, Chrome, Edge : intercepter `beforeinstallprompt` → bouton « Installer
    l'application » ;
  - iOS (pas d'invite automatique) : bandeau « Partager → Sur l'écran d'accueil », affiché
    seulement hors mode installé, et masquable.
- [ ] Mesurer le poids total préchargé et le temps de démarrage sur un téléphone d'entrée de
      gamme ; reporter les chiffres dans le README.

### Phase 5 — OCR dans le navigateur (L)

- [ ] `npm i tesseract.js @tesseract.js-data/fra` ; `angular.json` copie dans `tesseract/` le
      worker, **seulement les cœurs WASM LSTM** (variantes SIMD et non SIMD, choisies à
      l'exécution ; le paquet `tesseract.js-core` complet pèse ~30 Mo) et `fra.traineddata`.
- [ ] `createWorker('fra', …)` avec `workerPath`, `corePath` et `langPath` locaux (aucun appel
      réseau) et le cache IndexedDB de Tesseract désactivé (le service worker s'en charge déjà ;
      sinon le modèle serait stocké deux fois).
- [ ] `pretraitement.ts` : redimensionnement vers ~1600 px, niveaux de gris, contraste,
      binarisation, dans un `OffscreenCanvas`.
- [ ] `OcrService` reprend la logique de `ScanService.ScannerAsync` : première passe, puis
      seconde passe avec un autre prétraitement / mode de segmentation si `DateOcrParser.meilleur`
      ou `ShellCodeParser.trouverCode` ne trouvent rien.
- [ ] Code coquille : liste blanche de caractères `0-9A-Z` pour limiter les confusions.
- [ ] Le bouton de scan est un `<label>` lié à l'`<input type="file" capture>` : le geste de
      l'utilisateur ouvre directement l'appareil photo, sans demande de permission caméra.
- [ ] Worker Tesseract créé au premier scan, puis gardé en mémoire.
- [ ] Tests Playwright sur quelques photos de référence (boîtes, coquilles) dans
      `e2e/fixtures/`.

⚠️ À prévoir : Tesseract est moins bon que ML Kit ou Apple Vision, surtout sur l'impression
jet d'encre d'une coquille courbe. Le résultat reste toujours proposé en saisie modifiable
(c'est déjà le cas). Si la précision ne suffit pas, le levier le plus efficace est un viseur
caméra (`getUserMedia`) avec un cadre de recadrage autour du code.

### Phase 6 — Recette, bascule et nettoyage (M)

- [ ] Test Playwright « hors ligne » : première visite, attente de l'activation du service
      worker, `context.setOffline(true)`, rechargement, parcours des 4 onglets, calcul,
      décodage, changement de langue et de thème, OCR sur photo de référence.
- [ ] Recette sur **iPhone réel** : Safari → Sur l'écran d'accueil → mode avion → tous les
      écrans, scan DCR, scan coquille, changement de langue et de thème.
- [ ] Recette sur **Android réel** : Chrome → Installer → mode avion → idem.
- [ ] Supprimer `maui/` (l'application reste récupérable via le tag `v1-maui`) ;
      `tools/parite/` peut partir avec, `parite.json` reste.
- [ ] Réécrire le README : URL de l'app, installation iOS et Android, développement local
      (`ng serve` n'active pas le service worker ; tester le hors ligne demande `ng build` + un
      serveur statique sur `localhost`), déploiement, mise à jour annuelle d'Angular.
- [ ] Pull request de `claude/migration-pwa` vers `main`.

## 6. Hébergement : GitHub Pages

Dépôt public → GitHub Pages et GitHub Actions sont gratuits. Adresse :
**`https://oliviermct.github.io/OeufFrais/`**.

- Réglage unique : *Settings → Pages → Source : GitHub Actions*.
- `pages.yml` : Node 24, `npm ci`, `npx vitest run`, `ng build --base-href /OeufFrais/`, copie de
  `index.html` en `404.html`, puis `actions/upload-pages-artifact` + `actions/deploy-pages`.
- **Liens profonds** : GitHub Pages ne sait pas réécrire les URL. La copie en `404.html` fait
  servir l'application pour `/OeufFrais/code` ou `/OeufFrais/reglages` à la première visite ;
  ensuite le service worker répond lui-même à toutes les navigations, en ligne comme hors ligne.
- **Cache HTTP** : GitHub Pages impose 10 minutes et ne permet pas d'en-têtes personnalisés.
  Sans conséquence : les fichiers JS/CSS d'Angular portent une empreinte dans leur nom, et ngsw
  interroge son manifeste `ngsw.json` en contournant le cache.
- **Pendant la migration** : rien n'est encore publié depuis `main`, donc on déploie la branche
  `claude/migration-pwa` pour tester sur téléphone. L'environnement `github-pages` n'autorise
  par défaut que la branche principale : ajouter la branche dans *Settings → Environments →
  github-pages → Deployment branches*, puis la retirer après la fusion.
- Optionnel : domaine personnalisé (un enregistrement CNAME + fichier `CNAME`) ; la
  `--base-href` repasse alors à `/`.

**Coût total : 0 €**, contre 99 €/an pour Apple et 25 $ pour Google.

## 7. Risques et parades

| Risque | Impact | Parade |
|---|---|---|
| Erreur de portage C# → TypeScript | Mauvais conseil sanitaire | Tests de parité générés depuis le C#, relus avant suppression de `maui/` |
| Décalage d'un jour autour des changements d'heure | Statut faux deux jours par an | Calculs en jours calendaires UTC, tests fin mars / fin octobre |
| OCR moins précis qu'avec ML Kit / Vision | Scan de coquille parfois raté | Prétraitement, liste blanche, double passe, résultat toujours modifiable ; viseur avec recadrage si besoin |
| Poids du préchargement (surtout Tesseract) | Installation lente sur un réseau mobile faible | Préchargement en arrière-plan sans bloquer l'affichage, cœurs LSTM seuls, indicateur « Prête hors ligne », mesure en phase 4 |
| Version obsolète servie par le cache | Corrections qui n'arrivent pas | Bandeau de mise à jour, vérification au retour au premier plan |
| iOS : pas d'invite d'installation, purge possible du stockage | Utilisateurs qui n'installent pas ; préférences perdues | Bandeau d'aide iOS ; les web apps ajoutées à l'écran d'accueil échappent à la purge des 7 jours de Safari ; `storage.persist()` ; seules des préférences sont stockées |
| Une version majeure d'Angular tous les 6 mois | Maintenance | `ng update` une ou deux fois par an ; chaque version majeure est maintenue 18 mois ; Dependabot pour les correctifs |
| Politique d'Apple envers les PWA | Faible (le retrait envisagé dans l'UE en 2024 a été annulé) | Aucun code spécifique à Apple ; l'app reste utilisable dans Safari même non installée |

## 8. Décisions par défaut (à contester si besoin)

1. **Traductions** : service maison (signals + JSON), ni `@angular/localize`, ni bibliothèque
   tierce : 146 clés et une règle de pluriel ne justifient pas une dépendance de plus à faire
   suivre à chaque version d'Angular.
2. **Sélecteur de date** : Angular Material, pour un calendrier dans la langue de l'app.
   Alternative plus légère : `<input type="date">` natif, dont le calendrier suit la langue du
   téléphone.
3. **Application MAUI** : supprimée en fin de migration, conservée dans le tag `v1-maui`.
4. **OCR** : Tesseract.js avec le seul modèle français. Les dates et les codes n'utilisent que
   des chiffres et des lettres latines ; ajouter l'anglais alourdirait le cache sans gain.
5. **Adresse** : `oliviermct.github.io/OeufFrais`, domaine personnalisé possible plus tard.
