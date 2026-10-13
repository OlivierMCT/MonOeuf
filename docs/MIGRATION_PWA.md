# Plan de migration : Œuf Frais en PWA 100 % hors ligne

> Branche de travail : `claude/migration-pwa` · Point de départ : `main` @ `4746061`
> (application .NET MAUI 10 : Android, iOS, Windows)

## 1. Objectif et contraintes

- Distribuer l'application **sans compte Apple Developer (99 €/an) ni Google Play Console (25 $)** :
  une PWA qu'on installe depuis le navigateur, sur iPhone comme sur Android (et sur ordinateur).
- **Toutes les fonctionnalités disponibles hors ligne** après la première visite : calcul de
  fraîcheur, décodage du code coquille, conseils, réglages, 7 langues, et lecture photo (OCR)
  de la DCR et du code coquille.
- Coût d'exploitation visé : **0 €**.
- Aucune régression sur les règles métier (règlement (CE) n° 589/2008, ANSES) : la logique
  existante est réutilisée telle quelle, pas réécrite.

## 2. Choix technique : Blazor WebAssembly PWA

Recommandation : **Blazor WebAssembly autonome, modèle PWA** (`dotnet new blazorwasm --pwa`),
.NET 10.

Pourquoi :

- La logique métier est déjà du C# pur à 95 % (`EggCalculator`, `ShellCodeParser`,
  `DateOcrParser`, `LocalizationResourceManager`). Elle s'exécute telle quelle dans le
  navigateur : aucune règle réglementaire à réécrire, donc aucun risque de divergence.
- Les 7 fichiers `.resx` (146 clés × 7 langues) sont réutilisés sans conversion.
- Le modèle PWA de Blazor fournit un service worker qui précharge **tous** les fichiers publiés
  (assemblies, runtime .NET, données ICU, CSS, JS) avec contrôle d'intégrité : l'application
  entière tourne hors ligne dès que l'installation est terminée.
- Même langage, mêmes outils (Visual Studio / `dotnet`), pas de nouvel écosystème.

Contrepartie : un premier téléchargement plus lourd qu'une application JavaScript (le runtime
.NET pèse quelques Mo compressés) et environ une seconde de démarrage du runtime à chaque
lancement. C'est acceptable pour une application installée et en cache ; à mesurer en phase 4.

Alternative écartée : réécriture en TypeScript (Vite + Workbox). Plus léger, mais il faudrait
porter et re-tester ~600 lignes de règles métier et convertir les traductions, sans gain
visible pour l'utilisateur.

## 3. Correspondance fonctionnelle MAUI → PWA

| Fonction | Aujourd'hui (MAUI) | Demain (PWA) | Hors ligne |
|---|---|---|---|
| Calcul de fraîcheur (ponte / DCR) | `EggCalculator` | `EggCalculator` inchangé (projet Core) | ✅ |
| Décodage du code coquille | `ShellCodeParser` | inchangé | ✅ |
| Onglet Conserver | XAML + `EggCalculator.Conservation` | composant Razor | ✅ |
| 7 langues | `.resx` + `LocalizationResourceManager`, changement en direct | mêmes `.resx` ; changement = enregistrement puis rechargement de la page (motif standard Blazor) | ✅ assemblies satellites en cache |
| Palettes Miel / Lagon / Corail, clair / sombre / système | `ThemeService` → `DynamicResource` | variables CSS + `prefers-color-scheme` | ✅ |
| Préférences | `Preferences` MAUI | `localStorage`, derrière une interface | ✅ |
| Navigation 4 onglets | `Shell` + `TabBar` | barre d'onglets en bas, routes `/`, `/code`, `/conserver`, `/reglages` | ✅ |
| Sélecteur de date | `DatePicker` + forçage de la locale native | `<input type="date">` natif + date reformulée dans la langue de l'app | ✅ |
| Choix entre plusieurs dates lues | `DisplayActionSheetAsync` | `<dialog>` en feuille du bas | ✅ |
| Alertes | `DisplayAlertAsync` | `<dialog>` | ✅ |
| Poule qui marche | animations MAUI en C# | animation CSS, coupée si `prefers-reduced-motion` | ✅ |
| Photo | `MediaPicker` | `<input type="file" accept="image/*" capture="environment">` : appareil photo direct sur iOS et Android, sélecteur de fichier sur PC | ✅ |
| OCR | `Plugin.Maui.OCR` (Vision / ML Kit / Windows.Media.Ocr) | **Tesseract.js** (WebAssembly) auto-hébergé, modèle `fra` en cache | ✅ |
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
OeufFrais.slnx
global.json                    SDK .NET 10 épinglé
src/
  OeufFrais.Core/              net10.0, aucune dépendance MAUI, RootNamespace = OeufFrais
    Services/
      EggCalculator.cs
      ShellCodeParser.cs
      DateOcrParser.cs
      LocalizationResourceManager.cs
      IPreferences.cs          remplace Microsoft.Maui.Storage.Preferences
    Resources/Strings/
      AppStrings*.resx         déplacés tels quels
  OeufFrais.Web/               Blazor WebAssembly PWA
    Program.cs                 préférences + culture avant RunAsync
    App.razor
    Layout/                    MainLayout, BarreOnglets
    Pages/                     Fraicheur, CodeCoquille, Conserver, Reglages
    Components/                Carte, Dialogue, PouleQuiMarche, BandeauInstallation, BandeauMiseAJour
    Services/                  PreferencesNavigateur, ScanWeb, ThemeWeb
    wwwroot/
      index.html
      manifest.webmanifest
      service-worker.js        (développement : ne met rien en cache)
      service-worker.published.js
      _headers                 en-têtes de cache pour l'hébergeur
      css/app.css, css/theme.css
      js/pwa.js, js/ocr.js
      lib/tesseract/           worker, cœurs WASM, fra.traineddata
      icons/, fonts/
  OeufFrais.Maui/              application actuelle, gardée pendant la migration puis supprimée
tests/
  OeufFrais.Core.Tests/        xUnit
  OeufFrais.Web.E2E/           Playwright (.NET), dont le scénario hors ligne
  fixtures/                    photos de référence (boîtes, coquilles)
.github/workflows/
  ci.yml                       build + tests à chaque push
  deploy.yml                   prévisualisation par branche, production sur main
```

Le projet MAUI doit quitter la racine : un `.csproj` SDK inclut tous les `**/*.cs` situés sous
lui et compilerait le code des projets Core et Web.

En gardant `RootNamespace = OeufFrais` et le dossier `Resources/Strings/` dans le projet Core,
le nom de base du `ResourceManager` (`OeufFrais.Resources.Strings.AppStrings`) et l'espace de
noms `OeufFrais.Services` restent identiques : le déplacement ne change aucune ligne de code
appelant.

## 5. Phases

Chaque phase = un ou plusieurs commits sur `claude/migration-pwa`, qui compilent et passent
les tests. Taille indicative : **S** (≤ ½ jour), **M** (~1 jour), **L** (2–3 jours).

### Phase 0 — Préparation (S)

- [ ] Taguer l'état actuel : `git tag v1-maui` sur `main` (point de retour garanti).
- [ ] `git mv` du projet MAUI vers `src/OeufFrais.Maui/`, chemins du `.slnx` mis à jour ;
      vérifier qu'il compile (`dotnet build -f net10.0-android`).
- [ ] Ajouter `global.json`.

### Phase 1 — Extraire le cœur métier et le tester (M)

- [ ] Créer `OeufFrais.Core` (net10.0) et y déplacer `EggCalculator`, `ShellCodeParser`,
      `DateOcrParser`, `LocalizationResourceManager` et les `.resx`.
      `TranslateExtension`, `ThemeService`, `ScanService` et `Palettes` restent côté MAUI
      (ils dépendent de XAML, de `Application.Current` ou du plugin OCR).
- [ ] Retirer les dépendances MAUI du cœur :
  - `EggAssessment.CouleurStatut` / `CouleurStatutClaire` et `UsageConseil.Couleur` /
    `CouleurClaire` (type `Microsoft.Maui.Graphics.Color`) : le cœur expose `Statut`,
    `Autorise` et `CouleurHex` ; les propriétés `Color` passent dans un convertisseur côté MAUI.
  - `Preferences` → `IPreferences { string Get(string cle, string defaut); void Set(string cle, string valeur); }`,
    passée à `LocalizationResourceManager.Initialiser(...)`.
- [ ] MAUI référence `OeufFrais.Core`. Dans les XAML, `EggAssessment`, `UsageConseil` et
      `ShellCode` vivent désormais dans une autre assembly :
      `xmlns:core="clr-namespace:OeufFrais.Services;assembly=OeufFrais.Core"` pour les
      `x:DataType`. L'application doit se comporter exactement comme avant : c'est la preuve
      que l'extraction n'a rien cassé.
- [ ] Créer `OeufFrais.Core.Tests` (le dépôt n'a aucun test aujourd'hui) :
  - bornes de `EggCalculator` : J0, J9/J10, J28/J29, J42/J43, date future, conversion
    DCR ↔ ponte ;
  - `ShellCodeParser.Parse` : chiffres 0 à 3, espaces et tirets, formats invalides ;
    `TrouverCode` : confusions OCR (O → 0, B → 3), code coupé en plusieurs mots, bruit rejeté ;
  - `DateOcrParser` : `20/08/2026`, `20.08.26`, `20 AOUT 2026`, forme compacte avec mot-clé,
    inversion jour/mois, rejet hors fenêtre, séparateurs différents rejetés ;
  - traductions : chaque clé du `.resx` neutre existe dans les 6 autres langues, avec le même
    nombre d'arguments `{n}` ; une chaîne qui revient égale à sa clé fait échouer le test.

### Phase 2 — Squelette PWA et déploiement continu (M)

Le déploiement arrive tôt exprès : un service worker n'est actif qu'en HTTPS (ou sur
`localhost`), donc tester sur un vrai téléphone demande une URL publique dès le début.

- [ ] `dotnet new blazorwasm --pwa -o src/OeufFrais.Web`, référence vers `OeufFrais.Core`.
- [ ] `.csproj` :
  - `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` :
    sans ça, le portugais et le néerlandais n'ont pas leurs noms de jours et de mois (le
    découpage ICU par défaut ne les embarque pas) ;
  - `InvariantGlobalization` et `InvariantTimezone` laissés à `false` : le fuseau du navigateur
    est nécessaire pour que « aujourd'hui » soit juste.
- [ ] `Program.cs` : créer `PreferencesNavigateur` (`localStorage` via `IJSInProcessRuntime`,
      appel synchrone), appeler `LocalizationResourceManager.Instance.Initialiser(...)` **avant**
      `RunAsync`. C'est la culture en place à ce moment-là qui détermine les assemblies
      satellites chargées.
- [ ] `index.html` :
  - écran de chargement aux couleurs de l'app (l'œuf de `appiconfg.svg`) à la place de
    « Loading… » ;
  - `viewport-fit=cover`, `theme-color`, `apple-touch-icon` 180×180,
    `apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style` ;
  - petit script inline qui pose palette et thème depuis `localStorage` avant le démarrage de
    .NET, pour éviter un flash de mauvaises couleurs.
- [ ] `manifest.webmanifest` : nom « Œuf Frais », `short_name`, `lang`, `display: standalone`,
      `start_url` et `scope`, `background_color` et `theme_color` (#FF6F59), icônes 192, 512 et
      512 maskable générées depuis `Resources/AppIcon/appicon.svg` + `appiconfg.svg`.
- [ ] Layout : barre d'onglets en bas avec les icônes `icon_*.svg` existantes, marges
      `env(safe-area-inset-*)` pour l'encoche et la barre d'accueil de l'iPhone.
- [ ] `theme.css` : les 7 jetons (`--couleur-fond`, `--couleur-accent`, …) × 3 palettes ×
      clair/sombre, recopiés de `Palettes.cs`, choisis par `data-palette` / `data-theme` sur
      `<html>` ; mode « Système » via `@media (prefers-color-scheme: dark)`.
- [ ] Hébergement (voir section 6) : workflow `deploy.yml` qui publie chaque push de branche en
      prévisualisation et `main` en production.

### Phase 3 — Portage des écrans, sans OCR (L)

- [ ] **Fraîcheur** : puces Ponte / DCR, texte d'aide, `<input type="date">` avec les mêmes
      bornes que `MainPage.AppliquerMode` (−120 j / aujourd'hui en mode ponte, −120 j / +60 j en
      mode DCR), carte statut (couleur par classe CSS selon `Freshness`), dates clés, usages
      ✓ / ✕, test du verre d'eau si recommandé, avertissement.
- [ ] **Code coquille** : saisie (Entrée = décoder), résultat ou carte d'erreur, encart
      « Bon à savoir ».
- [ ] **Conserver** : règles, test du verre d'eau, DCR vs DLC, avertissement.
- [ ] **Réglages** : langue (Système + 7), palette, thème. Changement de langue →
      `Loc.Definir(code)` puis `NavigationManager.NavigateTo(uri, forceLoad: true)`.
- [ ] `<html lang>` suit la langue choisie (lecteurs d'écran, césure).
- [ ] Poule qui marche : animation CSS reprenant le trajet actuel (aller-retour, dandinement,
      pauses où elle regarde autour d'elle), arrêtée si `prefers-reduced-motion`.
- [ ] Composant `Dialogue` (remplace `DisplayAlertAsync` et `DisplayActionSheetAsync`).
- [ ] Ce qui disparaît : le forçage de locale du calendrier natif (`MauiProgram`,
      `MainActivity.AttachBaseContext`). Le web ne permet pas de forcer la langue du sélecteur
      de date natif, il suit celle du téléphone. La date choisie est reformulée en toutes
      lettres dans la langue de l'app juste en dessous (« jeudi 6 août 2026 »).

### Phase 4 — Hors ligne et installation (M)

- [ ] `service-worker.published.js` : compléter `offlineAssetsInclude` avec `.svg`, `.woff2` et
      `.traineddata`, absents de la liste par défaut (ils ne seraient sinon jamais mis en cache).
      Vérifier dans `service-worker-assets.js` publié que les assemblies satellites des 6 langues
      y figurent.
- [ ] Polices Open Sans converties en `.woff2` et auto-hébergées. Aucune ressource externe :
      ni Google Fonts, ni CDN.
- [ ] Mises à jour : quand un nouveau service worker attend, bandeau « Nouvelle version
      disponible — Mettre à jour » → `skipWaiting` → rechargement. Sans ça, une PWA installée
      peut rester des semaines sur une ancienne version.
- [ ] Indicateur « ✓ Prête hors ligne » dans Réglages une fois le préchargement terminé :
      l'utilisateur sait quand il peut se passer du réseau.
- [ ] `navigator.storage.persist()` pour limiter l'éviction du cache.
- [ ] Installation :
  - Android, Chrome, Edge : intercepter `beforeinstallprompt` → bouton « Installer
    l'application » ;
  - iOS (pas d'invite automatique) : bandeau « Partager → Sur l'écran d'accueil », affiché
    seulement hors mode installé, et masquable.
- [ ] Mesurer le poids total préchargé et le temps de démarrage sur un téléphone d'entrée de
      gamme ; reporter les chiffres dans le README.

### Phase 5 — OCR dans le navigateur (L)

- [ ] Tesseract.js auto-hébergé dans `wwwroot/lib/tesseract/` : worker, cœurs WASM (variantes
      SIMD et non SIMD, choisies à l'exécution), `fra.traineddata`. Version épinglée ; chemins
      `workerPath`, `corePath` et `langPath` locaux, donc aucun appel réseau.
- [ ] `js/ocr.js` : lit la photo, la prépare dans un `<canvas>` (redimensionnement vers
      ~1600 px, niveaux de gris, contraste, binarisation), lance la reconnaissance et renvoie le
      texte brut au C#. L'image ne transite pas par l'interop .NET (inutilement lourd).
- [ ] `ScanWeb` reprend la logique de `ScanService.ScannerAsync` : première passe, puis seconde
      passe avec un autre prétraitement / mode de segmentation si
      `DateOcrParser.Meilleur` ou `ShellCodeParser.TrouverCode` ne trouvent rien.
- [ ] Code coquille : liste blanche de caractères `0-9A-Z` pour limiter les confusions.
- [ ] Le bouton de scan est un `<label>` lié à l'`<input type="file" capture>` : le geste de
      l'utilisateur ouvre directement l'appareil photo, sans JavaScript ni demande de
      permission caméra.
- [ ] Worker Tesseract créé au premier scan, puis gardé en mémoire.
- [ ] Tests E2E sur quelques photos de référence (boîtes, coquilles) dans `tests/fixtures/`.

⚠️ À prévoir : Tesseract est moins bon que ML Kit ou Apple Vision, surtout sur l'impression
jet d'encre d'une coquille courbe. Le résultat reste toujours proposé en saisie modifiable
(c'est déjà le cas). Si la précision ne suffit pas, le levier le plus efficace est un viseur
caméra (`getUserMedia`) avec un cadre de recadrage autour du code.

### Phase 6 — Recette, bascule et nettoyage (M)

- [ ] Test E2E automatisé « hors ligne » (Playwright) : première visite, attente de
      l'activation du service worker, `SetOfflineAsync(true)`, rechargement, parcours des
      4 onglets, calcul, décodage, changement de langue et de thème, OCR sur image de référence.
- [ ] Recette sur **iPhone réel** : Safari → Sur l'écran d'accueil → mode avion → tous les
      écrans, scan DCR, scan coquille, changement de langue et de thème.
- [ ] Recette sur **Android réel** : Chrome → Installer → mode avion → idem.
- [ ] Supprimer `src/OeufFrais.Maui/` et la dépendance `Plugin.Maui.OCR` (l'application reste
      récupérable via le tag `v1-maui`).
- [ ] Réécrire le README : URL de l'app, installation iOS et Android, développement local
      (`dotnet watch` ; le service worker de développement ne met rien en cache, tester le hors
      ligne demande `dotnet publish` + un serveur statique sur `localhost`), déploiement.
- [ ] Pull request de `claude/migration-pwa` vers `main`.

## 6. Hébergement gratuit

Le dépôt est **privé**, or GitHub Pages n'est disponible pour un dépôt privé qu'avec un
abonnement payant. Recommandation : **Cloudflare Pages** (offre gratuite : HTTPS, bande
passante illimitée pour du statique, domaine personnalisé, une URL de prévisualisation par
branche).

- GitHub Actions construit l'app (`dotnet publish -c Release`), puis déploie le dossier
  `wwwroot` publié avec `wrangler pages deploy … --branch=<branche>`. Seul secret nécessaire :
  un jeton d'API Cloudflare. Les 2 000 minutes Actions mensuelles gratuites d'un dépôt privé
  suffisent largement.
- Fichier `_headers` : `Cache-Control: no-cache` sur `index.html`, `service-worker.js` et
  `service-worker-assets.js` (sinon les mises à jour n'arrivent pas) ; cache long sur
  `_framework/*` (fichiers à empreinte).
- Liens profonds (`/code`, `/reglages`) : sans `404.html` dans le site, Cloudflare Pages
  renvoie `index.html` pour toute route inconnue, ce qu'il faut pour une SPA.
- Optionnel : sous-domaine `oeuffrais.olivierastre.fr` (un enregistrement CNAME, si vous gérez
  ce domaine).

Alternative : rendre le dépôt public et publier sur GitHub Pages (même workflow, avec
`<base href="/OeufFrais/">`).

**Coût total : 0 €**, contre 99 €/an pour Apple et 25 $ pour Google.

## 7. Risques et parades

| Risque | Impact | Parade |
|---|---|---|
| OCR moins précis qu'avec ML Kit / Vision | Scan de coquille parfois raté | Prétraitement, liste blanche, double passe, résultat toujours modifiable ; viseur avec recadrage si besoin |
| Premier téléchargement lourd (runtime .NET + Tesseract) | Installation lente sur un réseau mobile faible | Préchargement en arrière-plan sans bloquer l'affichage, indicateur « Prête hors ligne », mesure en phase 4 |
| Version obsolète servie par le cache | Corrections qui n'arrivent pas | Bandeau de mise à jour + en-têtes `no-cache` sur le service worker |
| iOS : pas d'invite d'installation, purge possible du stockage | Utilisateurs qui n'installent pas ; préférences perdues | Bandeau d'aide iOS ; les web apps ajoutées à l'écran d'accueil échappent à la purge des 7 jours de Safari ; `storage.persist()` ; seules des préférences sont stockées |
| Sélecteur de date dans la langue du téléphone | Petite incohérence visuelle | Date reformulée en toutes lettres dans la langue de l'app |
| Changement de langue = rechargement | Environ une seconde d'attente | Acceptable, et fonctionne hors ligne |
| Politique d'Apple envers les PWA | Faible (le retrait envisagé dans l'UE en 2024 a été annulé) | Aucun code spécifique à Apple ; l'app reste utilisable dans Safari même non installée |

## 8. Décisions par défaut (à contester si besoin)

1. **Stack** : Blazor WebAssembly, pour réutiliser le C# et les `.resx`.
2. **Application MAUI** : supprimée en fin de migration, conservée dans le tag `v1-maui`.
   Alternative : la garder pour distribuer un APK Android hors store (gratuit), au prix de deux
   interfaces à maintenir.
3. **Hébergement** : Cloudflare Pages, le dépôt reste privé.
4. **OCR** : Tesseract.js avec le seul modèle français. Les dates et les codes n'utilisent que
   des chiffres et des lettres latines ; ajouter l'anglais alourdirait le cache sans gain.
