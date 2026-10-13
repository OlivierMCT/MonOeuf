# Plan de migration : Œuf Frais en PWA Blazor WebAssembly, 100 % hors ligne

> Branche de travail : `claude/migration-pwa` · Point de départ : `main` @ `4746061`
> (application .NET MAUI 10 : Android, iOS, Windows)

## Avancement

**Phases 0 à 4 faites, phase 5 automatisée faite.** La PWA est complète et testée :
88 tests unitaires et 15 tests de bout en bout dans Chromium, dont un parcours entier réseau
coupé (rechargement, lien profond, calcul, décodage, changement de langue) et l'arrivée d'une
nouvelle version.

Reste à faire, de votre côté :

1. Créer le tag `v1-maui` (l'environnement de travail n'a pu pousser que la branche) :
   `git tag -a v1-maui 4746061 -m "Dernière version MAUI" && git push origin v1-maui`.
2. Vérifier les adresses e-mail des commits et choisir une licence, puis rendre le dépôt public.
3. *Settings → Pages → Source : GitHub Actions*, et autoriser la branche `claude/migration-pwa`
   dans *Settings → Environments → github-pages* pour la tester sur téléphone avant la fusion.
4. Recette sur iPhone et Android réels (phase 5), mesure du démarrage sur un téléphone modeste.
5. Ensuite : supprimer `src/OeufFrais.Maui/`, ouvrir la pull request vers `main`.

Écarts par rapport au plan ci-dessous :

- **App MAUI** : modifiée comme prévu (photos retirées, branchée sur `OeufFrais.Core`), mais
  compilée ici seulement pour son code C# et ses XAML, contre le paquet MAUI en `net10.0` :
  le SDK Android n'était pas accessible. À compiler une fois sous Windows avant de s'y fier.
- **Saisie de la date** : mieux que prévu. La date s'affiche dans la langue de l'app (« mardi
  13 octobre 2026 »), comme sous MAUI ; le champ date natif, transparent, est posé par-dessus
  et ouvre le calendrier du téléphone. Seul ce calendrier reste dans la langue du téléphone.
- **Un seul workflow** (`pwa.yml`) au lieu de `ci.yml` + `pages.yml` ; la mise en ligne est
  sautée tant que le dépôt est privé. Le site est préparé par `scripts/publier-site.sh`,
  utilisé aussi pour les tests de bout en bout.
- **Langue « Système »** : corrigée. Revenir sur « Système » après un choix explicite gardait
  ce choix jusqu'au redémarrage ; et une langue de téléphone non traduite (japonais…) donne
  désormais des dates en français au lieu de dates japonaises à côté de textes français.
- **Lisibilité en mode sombre** : le texte posé sur la couleur d'accent (boutons, puces,
  pastilles) reste foncé, et les erreurs passent en rouge clair ; sous MAUI, texte clair sur
  jaune vif.
- **12 nouveaux textes** traduits dans les 7 langues (installation, mise à jour, hors ligne,
  page introuvable).
- **Poids mesuré** : 8,5 Mo préchargés (2,5 Mo en brotli ; GitHub Pages compresse en gzip).
- **Compatibilité iOS : 16.4 minimum** (iPhone 8 et suivants), contre iOS 15 pour l'app MAUI.
  .NET 10 active d'office WebAssembly SIMD, que Safari ne gère que depuis la 16.4. Un
  navigateur trop ancien affiche un message au lieu d'un chargement sans fin. Pour viser
  iOS 15 (iPhone 6s, 7, SE de 1re génération), il faudrait désactiver SIMD
  (`WasmEnableSIMD=false`), ce qui demande le workload `wasm-tools` dans le workflow.

## 1. Objectif et contraintes

- Distribuer l'application **sans compte Apple Developer (99 €/an) ni Google Play Console (25 $)** :
  une PWA qu'on installe depuis le navigateur, sur iPhone comme sur Android (et sur ordinateur).
- **Toutes les fonctionnalités disponibles hors ligne** après la première visite : calcul de
  fraîcheur, décodage du code coquille, conseils, réglages, 7 langues.
- **Coût d'exploitation : 0 €.** Le dépôt devient public, il est donc hébergé sur GitHub Pages.
- Aucune régression sur les règles métier (règlement (CE) n° 589/2008, ANSES) : la logique
  existante est réutilisée telle quelle, pas réécrite.
- **Plus aucune photo** : la date et le code coquille se saisissent au clavier (voir § 4).

## 2. Choix technique : Blazor WebAssembly

**Blazor WebAssembly autonome, modèle PWA** (`dotnet new blazorwasm --pwa`), .NET 10.

- La logique métier est déjà du C# pur (`EggCalculator`, `ShellCodeParser`,
  `LocalizationResourceManager`). Elle s'exécute telle quelle dans le navigateur : aucune règle
  réglementaire à réécrire, donc aucun risque de divergence.
- Les 7 fichiers `.resx` sont réutilisés sans conversion (130 clés une fois les textes liés aux
  photos retirés).
- Le modèle PWA de Blazor fournit un service worker qui précharge **tous** les fichiers publiés
  (assemblies, runtime .NET, données ICU, CSS, JS) avec contrôle d'intégrité : l'application
  entière tourne hors ligne dès que l'installation est terminée.
- Sans photo ni reconnaissance de texte, l'app n'a besoin d'aucune bibliothèque JavaScript ni
  d'aucune autorisation (caméra, fichiers) : tout est en C#, HTML et CSS.
- Même langage, mêmes outils (Visual Studio / `dotnet`), pas de nouvel écosystème.

Contrepartie : un premier téléchargement plus lourd qu'une application JavaScript (le runtime
.NET pèse quelques Mo compressés) et environ une seconde de démarrage du runtime à chaque
lancement. C'est acceptable pour une application installée et en cache ; à mesurer en phase 4.

## 3. Correspondance fonctionnelle MAUI → PWA

| Fonction | Aujourd'hui (MAUI) | Demain (PWA) | Hors ligne |
|---|---|---|---|
| Calcul de fraîcheur (ponte / DCR) | `EggCalculator` | `EggCalculator` inchangé (projet Core) | ✅ |
| Saisie de la date | `DatePicker` + forçage de la locale native | `<input type="date">` natif + date reformulée dans la langue de l'app | ✅ |
| Décodage du code coquille | `ShellCodeParser.Parse` | inchangé | ✅ |
| Saisie du code coquille | `Entry` | `<input>` adapté au code (voir phase 3) | ✅ |
| Onglet Conserver | XAML + `EggCalculator.Conservation` | composant Razor | ✅ |
| 7 langues | `.resx` + `LocalizationResourceManager`, changement en direct | mêmes `.resx` ; changement = enregistrement puis rechargement de la page (motif standard Blazor) | ✅ assemblies satellites en cache |
| Palettes Miel / Lagon / Corail, clair / sombre / système | `ThemeService` → `DynamicResource` | variables CSS + `prefers-color-scheme` | ✅ |
| Préférences | `Preferences` MAUI | `localStorage`, derrière une interface | ✅ |
| Navigation 4 onglets | `Shell` + `TabBar` | barre d'onglets en bas, routes `/`, `/code`, `/conserver`, `/reglages` | ✅ |
| Poule qui marche | animations MAUI en C# | animation CSS, coupée si `prefers-reduced-motion` | ✅ |
| Icône, écran de démarrage | `MauiIcon`, `MauiSplashScreen` | `manifest.webmanifest`, icônes PNG 192/512 + maskable, `apple-touch-icon` | ✅ |
| Mises à jour | stores | automatiques, avec bandeau « Nouvelle version — Mettre à jour » | nécessite le réseau, par nature |

### Pistes d'évolution du README, revues pour le web

- **Suivi de plusieurs boîtes** : `localStorage` ou IndexedDB → fonctionne hors ligne.
- **Notification avant la DCR** : le web ne sait pas programmer une notification locale hors
  ligne (Web Push exige un serveur et le réseau ; sur iOS, uniquement pour une PWA installée,
  iOS 16.4+). Remplacement proposé : bouton « Ajouter au calendrier » qui génère un fichier
  `.ics` côté client (rappel à DCR − 2 jours). 100 % hors ligne, rappel géré par le calendrier
  du téléphone.
- La piste « Scan photo du code coquille » du README actuel est abandonnée.

## 4. Ce qui est retiré

### Toute prise de photo et la reconnaissance de texte (décision produit)

| Où | Ce qui part |
|---|---|
| Code | `Services/ScanService.cs`, `Services/DateOcrParser.cs` ; dans `ShellCodeParser`, `TrouverCode` et ce qui ne sert qu'à lui (`Candidat`, `PaysConnus`, `Evaluer`, `Decouper`) — `Parse` et `NomPays` restent |
| Fraîcheur (`MainPage`) | bouton « Lire la date sur la boîte », sa note, la feuille de choix entre plusieurs dates, `OnScannerBoite`, `RebinderTexte`, `Message` |
| Code coquille (`CodePage`) | bouton photo, sa note, `OnScannerCoquille`, `RebinderTexte`, `Message` |
| Styles | `NoteScan` dans `App.xaml` |
| Dépendances et plateformes | paquet `Plugin.Maui.OCR`, `.UseOcr()` dans `MauiProgram`, `NoWarn XA4301` du `.csproj`, permission `CAMERA`, `uses-feature camera` et méta-donnée ML Kit de l'`AndroidManifest`, `NSCameraUsageDescription` de l'`Info.plist` iOS |
| Traductions (16 clés × 7 langues) | `Main_BoutonScan`, `Main_ScanAucuneDate`, `Main_ScanQuelleDcr`, `Main_ScanDateLue`, `Code_BoutonScan`, `Code_ScanIllisible`, `Code_ScanRelire`, `Common_LectureEnCours`, `Common_ScanImpossible`, `Common_Fermer`, `Common_Annuler`, `Scan_PermissionRefusee`, `Scan_NonSupporte`, `Scan_PhotoEchec`, `Scan_LectureEchec`, `Scan_OcrEchec` |

Ces suppressions se font dans l'app MAUI dès la phase 1, pour qu'elle reste une référence
fidèle de ce qu'on porte. Le tag `v1-maui` garde la version avec photos.

### Forçage de la langue du calendrier natif (impossible sur le web)

`<input type="date">` affiche son calendrier dans la langue du téléphone, sans moyen de la
forcer. Le mapper du `DatePicker` dans `MauiProgram` et `MainActivity.AttachBaseContext`
n'ont pas d'équivalent ; la date choisie est reformulée en toutes lettres dans la langue de
l'app juste en dessous du champ.

## 5. Architecture cible

```
OeufFrais.slnx
global.json                    SDK .NET 10 épinglé
src/
  OeufFrais.Core/              net10.0, aucune dépendance MAUI, RootNamespace = OeufFrais
    Services/
      EggCalculator.cs
      ShellCodeParser.cs
      LocalizationResourceManager.cs
      IPreferences.cs          remplace Microsoft.Maui.Storage.Preferences
    Resources/Strings/
      AppStrings*.resx         déplacés tels quels
  OeufFrais.Web/               Blazor WebAssembly PWA
    Program.cs                 préférences + culture avant RunAsync
    App.razor
    Layout/                    MainLayout, BarreOnglets
    Pages/                     Fraicheur, CodeCoquille, Conserver, Reglages
    Components/                Carte, PouleQuiMarche, BandeauInstallation, BandeauMiseAJour
    Services/                  PreferencesNavigateur
    wwwroot/
      index.html
      manifest.webmanifest
      service-worker.js        (développement : ne met rien en cache)
      service-worker.published.js
      css/app.css, css/theme.css
      js/pwa.js                installation, mises à jour, « prête hors ligne »
      icons/, fonts/
  OeufFrais.Maui/              application actuelle, gardée pendant la migration puis supprimée
tests/
  OeufFrais.Core.Tests/        xUnit
  OeufFrais.Web.E2E/           Playwright (.NET), dont le scénario hors ligne
.github/workflows/
  ci.yml                       build + tests à chaque push et pull request
  pages.yml                    publication sur GitHub Pages
```

Le projet MAUI doit quitter la racine : un `.csproj` SDK inclut tous les `**/*.cs` situés sous
lui et compilerait le code des projets Core et Web.

En gardant `RootNamespace = OeufFrais` et le dossier `Resources/Strings/` dans le projet Core,
le nom de base du `ResourceManager` (`OeufFrais.Resources.Strings.AppStrings`) et l'espace de
noms `OeufFrais.Services` restent identiques : le déplacement ne change aucune ligne de code
appelant.

## 6. Phases

Chaque phase = un ou plusieurs commits sur `claude/migration-pwa`, qui compilent et passent
les tests. Taille indicative : **S** (≤ ½ jour), **M** (~1 jour), **L** (2–3 jours).

### Phase 0 — Préparation et passage en public (S)

- [ ] Taguer l'état actuel : `git tag v1-maui` sur `main` (point de retour garanti, avec les
      photos encore présentes).
- [x] Avant de rendre le dépôt public :
  - [x] historique parcouru : aucun secret (clé, jeton, mot de passe) ;
  - [x] retirer `.claude/settings.local.json` du suivi et l'ajouter au `.gitignore` (réglages
        locaux de Claude Code, avec des chemins de votre poste) ;
  - [ ] vérifier les adresses e-mail des commits : elles deviennent publiques ;
  - [ ] choisir une licence (sans fichier `LICENSE`, le code est visible mais pas réutilisable).
- [x] `git mv` du projet MAUI vers `src/OeufFrais.Maui/`, chemins du `.slnx` mis à jour ;
      vérifier qu'il compile (`dotnet build -f net10.0-android`).
- [x] Ajouter `global.json`.

### Phase 1 — Retirer les photos, extraire le cœur métier et le tester (M)

- [x] Retirer tout ce qui touche aux photos (liste du § 4), dans l'app MAUI et dans les 7
      `.resx`.
- [x] Créer `OeufFrais.Core` (net10.0) et y déplacer `EggCalculator`, `ShellCodeParser`,
      `LocalizationResourceManager` et les `.resx`. `TranslateExtension`, `ThemeService` et
      `Palettes` restent côté MAUI (ils dépendent de XAML ou de `Application.Current`).
- [x] Retirer les dépendances MAUI du cœur :
  - `EggAssessment.CouleurStatut` / `CouleurStatutClaire` et `UsageConseil.Couleur` /
    `CouleurClaire` (type `Microsoft.Maui.Graphics.Color`) : le cœur expose `Statut`,
    `Autorise` et `CouleurHex` ; les propriétés `Color` passent dans un convertisseur côté MAUI.
  - `Preferences` → `IPreferences { string Get(string cle, string defaut); void Set(string cle, string valeur); }`,
    passée à `LocalizationResourceManager.Initialiser(...)`.
- [x] MAUI référence `OeufFrais.Core`. Dans les XAML, `EggAssessment`, `UsageConseil` et
      `ShellCode` vivent désormais dans une autre assembly :
      `xmlns:core="clr-namespace:OeufFrais.Services;assembly=OeufFrais.Core"` pour les
      `x:DataType`. L'application doit se comporter exactement comme avant (moins les photos) :
      c'est la preuve que l'extraction n'a rien cassé.
- [x] Créer `OeufFrais.Core.Tests` (le dépôt n'a aucun test aujourd'hui) :
  - bornes de `EggCalculator` : J0, J9/J10, J28/J29, J42/J43, date future, conversion
    DCR ↔ ponte ;
  - `ShellCodeParser.Parse` : chiffres 0 à 3, minuscules, espaces et tirets, code trop court
    ou trop long, chiffre de tête hors 0–3, pays connu et inconnu ;
  - traductions : chaque clé du `.resx` neutre existe dans les 6 autres langues, avec le même
    nombre d'arguments `{n}` ; une chaîne qui revient égale à sa clé fait échouer le test.

### Phase 2 — Squelette PWA et mise en ligne (M)

La mise en ligne arrive tôt exprès : un service worker n'est actif qu'en HTTPS (ou sur
`localhost`), donc tester sur un vrai téléphone demande une URL publique dès le début.

- [x] `dotnet new blazorwasm --pwa -o src/OeufFrais.Web`, référence vers `OeufFrais.Core`.
- [x] `.csproj` :
  - `<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>` :
    sans ça, le portugais et le néerlandais n'ont pas leurs noms de jours et de mois (le
    découpage ICU par défaut ne les embarque pas) ;
  - `InvariantGlobalization` et `InvariantTimezone` laissés à `false` : le fuseau du navigateur
    est nécessaire pour que « aujourd'hui » soit juste.
- [x] `Program.cs` : créer `PreferencesNavigateur` (`localStorage` via `IJSInProcessRuntime`,
      appel synchrone), appeler `LocalizationResourceManager.Instance.Initialiser(...)` **avant**
      `RunAsync`. C'est la culture en place à ce moment-là qui détermine les assemblies
      satellites chargées.
- [x] `index.html` :
  - écran de chargement aux couleurs de l'app (l'œuf de `appiconfg.svg`) à la place de
    « Loading… » ;
  - `viewport-fit=cover`, `theme-color`, `apple-touch-icon` 180×180,
    `apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style` ;
  - petit script inline qui pose palette et thème depuis `localStorage` avant le démarrage de
    .NET, pour éviter un flash de mauvaises couleurs.
- [x] `manifest.webmanifest` : nom « Œuf Frais », `short_name`, `lang`, `display: standalone`,
      `start_url` et `scope` relatifs (`./`), `background_color` et `theme_color` (#FF6F59),
      icônes 192, 512 et 512 maskable générées depuis `Resources/AppIcon/*.svg`.
- [x] Mise en page : barre d'onglets en bas avec les icônes `icon_*.svg` existantes, marges
      `env(safe-area-inset-*)` pour l'encoche et la barre d'accueil de l'iPhone.
- [x] `theme.css` : les 7 jetons (`--couleur-fond`, `--couleur-accent`, …) × 3 palettes ×
      clair/sombre, recopiés de `Palettes.cs`, choisis par `data-palette` / `data-theme` sur
      `<html>` ; mode « Système » via `@media (prefers-color-scheme: dark)`.
- [x] Workflow `pwa.yml` (voir § 7) : tests, publication, tests de bout en bout, mise en ligne.
      La première mise en ligne attend le passage du dépôt en public.

### Phase 3 — Portage des écrans (L)

- [x] **Fraîcheur** : puces Ponte / DCR, texte d'aide, `<input type="date">` avec les mêmes
      bornes que `MainPage.AppliquerMode` (−120 j / aujourd'hui en mode ponte, −120 j / +60 j en
      mode DCR), date reformulée en toutes lettres en dessous, bouton Calculer, carte statut
      (couleur par classe CSS selon `Freshness`), dates clés, usages ✓ / ✕, test du verre d'eau
      si recommandé, avertissement.
- [x] **Code coquille** : la saisie devient le seul moyen d'entrer le code, elle doit être
      confortable au pouce :
  - `autocapitalize="characters"`, `autocomplete="off"`, `autocorrect="off"`,
    `spellcheck="false"` (sinon le clavier « corrige » `3FRABC01`), `enterkeyhint="go"`,
    `maxlength="16"` ;
  - affichage en majuscules ; Entrée ou bouton = décoder ;
  - résultat ou carte d'erreur, encart « Bon à savoir ».
- [x] **Conserver** : règles, test du verre d'eau, DCR vs DLC, avertissement.
- [x] **Réglages** : langue (Système + 7), palette, thème ; changement de langue →
      `Loc.Definir(code)` puis `NavigationManager.NavigateTo(uri, forceLoad: true)`.
- [x] `<html lang>` suit la langue choisie (lecteurs d'écran, césure).
- [x] Poule qui marche : animation CSS reprenant le trajet actuel (aller-retour, dandinement,
      pauses où elle regarde autour d'elle), arrêtée si `prefers-reduced-motion`.

### Phase 4 — Hors ligne et installation (M)

- [x] `service-worker.published.js` : compléter `offlineAssetsInclude` avec `.svg` et `.woff2`,
      absents de la liste par défaut (ils ne seraient sinon jamais mis en cache). Vérifier dans
      `service-worker-assets.js` publié que les assemblies satellites des 6 langues y figurent.
- [x] Polices Open Sans converties en `.woff2` et auto-hébergées. Aucune ressource externe :
      ni Google Fonts, ni CDN.
- [x] Mises à jour : quand un nouveau service worker attend, bandeau « Nouvelle version
      disponible — Mettre à jour » → `skipWaiting` → rechargement. Vérification à chaque retour
      au premier plan (`visibilitychange`) : une PWA installée reste parfois des jours en
      mémoire, sur une ancienne version.
- [x] Indicateur « ✓ Prête hors ligne » dans Réglages une fois le préchargement terminé :
      l'utilisateur sait quand il peut se passer du réseau.
- [x] `navigator.storage.persist()` pour limiter l'éviction du cache.
- [x] Installation :
  - Android, Chrome, Edge : intercepter `beforeinstallprompt` → bouton « Installer
    l'application » ;
  - iOS (pas d'invite automatique) : bandeau « Partager → Sur l'écran d'accueil », affiché
    seulement hors mode installé, et masquable.
- [ ] Mesurer le poids total préchargé et le temps de démarrage sur un téléphone d'entrée de
      gamme ; reporter les chiffres dans le README.

### Phase 5 — Recette, bascule et nettoyage (M)

- [x] Test Playwright « hors ligne » : première visite, attente de l'activation du service
      worker, `SetOfflineAsync(true)`, rechargement, parcours des 4 onglets, calcul en mode
      ponte et DCR, décodage d'un code valide et d'un code invalide, changement de langue et de
      thème.
- [ ] Recette sur **iPhone réel** : Safari → Sur l'écran d'accueil → mode avion → tous les
      écrans, saisie de date et de code, changement de langue et de thème.
- [ ] Recette sur **Android réel** : Chrome → Installer → mode avion → idem.
- [ ] Supprimer `src/OeufFrais.Maui/` (l'application reste récupérable via le tag `v1-maui`).
- [x] Réécrire le README : URL de l'app, installation iOS et Android, développement local
      (`dotnet watch` ; le service worker de développement ne met rien en cache, tester le hors
      ligne demande `dotnet publish` + un serveur statique sur `localhost`), déploiement.
- [ ] Pull request de `claude/migration-pwa` vers `main`.

## 7. Hébergement : GitHub Pages

Dépôt public → GitHub Pages et GitHub Actions sont gratuits. Adresse :
**`https://oliviermct.github.io/OeufFrais/`**.

- Réglage unique : *Settings → Pages → Source : GitHub Actions*.
- `pages.yml` : .NET 10, `dotnet test`, puis :
  1. remplacer `<base href="/" />` par `<base href="/OeufFrais/" />` dans `wwwroot/index.html`
     **avant** `dotnet publish`. Le manifeste du service worker contient l'empreinte SHA-256 de
     chaque fichier : modifier `index.html` après publication ferait échouer l'installation du
     service worker, et l'app ne marcherait plus hors ligne ;
  2. `dotnet publish src/OeufFrais.Web -c Release` ;
  3. copier le `index.html` publié en `404.html` et ajouter un fichier `.nojekyll` ;
  4. `actions/upload-pages-artifact` (dossier `wwwroot` publié) + `actions/deploy-pages`.
- **Liens profonds** : GitHub Pages ne sait pas réécrire les URL. Le `404.html` fait servir
  l'application pour `/OeufFrais/code` ou `/OeufFrais/reglages` à la première visite ; ensuite
  le service worker répond lui-même à toutes les navigations, en ligne comme hors ligne.
- **Cache HTTP** : GitHub Pages impose 10 minutes et ne permet pas d'en-têtes personnalisés.
  Sans conséquence : le modèle Blazor enregistre le service worker avec
  `updateViaCache: 'none'`, et les fichiers de `_framework/` portent une empreinte.
- **Pendant la migration** : rien n'est encore publié depuis `main`, donc on déploie la branche
  `claude/migration-pwa` pour tester sur téléphone. L'environnement `github-pages` n'autorise
  par défaut que la branche principale : ajouter la branche dans *Settings → Environments →
  github-pages → Deployment branches*, puis la retirer après la fusion.
- Optionnel : domaine personnalisé (un enregistrement CNAME + fichier `CNAME`) ; la balise
  `<base>` reste alors à `/`.

**Coût total : 0 €**, contre 99 €/an pour Apple et 25 $ pour Google.

## 8. Risques et parades

| Risque | Impact | Parade |
|---|---|---|
| Premier téléchargement plus lourd (runtime .NET) | Installation un peu lente sur un réseau mobile faible | Préchargement en arrière-plan sans bloquer l'affichage, indicateur « Prête hors ligne », mesure en phase 4 |
| Service worker qui ne s'installe pas | App inutilisable hors ligne | Ne jamais modifier un fichier après `dotnet publish` ; test Playwright hors ligne dans la CI |
| Version obsolète servie par le cache | Corrections qui n'arrivent pas | Bandeau de mise à jour, vérification au retour au premier plan |
| iOS : pas d'invite d'installation, purge possible du stockage | Utilisateurs qui n'installent pas ; préférences perdues | Bandeau d'aide iOS ; les web apps ajoutées à l'écran d'accueil échappent à la purge des 7 jours de Safari ; `storage.persist()` ; seules des préférences sont stockées |
| Erreurs de frappe dans le code coquille | Code refusé | Clavier sans correction automatique, majuscules forcées, espaces et tirets tolérés (déjà le cas dans `Parse`), message d'erreur explicite |
| Sélecteur de date dans la langue du téléphone | Petite incohérence visuelle | Date reformulée en toutes lettres dans la langue de l'app |
| Changement de langue = rechargement | Environ une seconde d'attente | Acceptable, et fonctionne hors ligne |
| Politique d'Apple envers les PWA | Faible (le retrait envisagé dans l'UE en 2024 a été annulé) | Aucun code spécifique à Apple ; l'app reste utilisable dans Safari même non installée |

## 9. Décisions

Validées :

1. **Stack** : Blazor WebAssembly, pour réutiliser le C# et les `.resx`.
2. **Hébergement** : GitHub Pages, le dépôt passe public.
3. **Photos** : plus aucune ; date et code coquille saisis au clavier.

Par défaut (à contester si besoin) :

4. **Application MAUI** : supprimée en fin de migration, conservée dans le tag `v1-maui`.
5. **Adresse** : `oliviermct.github.io/OeufFrais`, domaine personnalisé possible plus tard.
