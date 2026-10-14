# Œuf Frais

Détermine si un œuf est encore consommable à partir de sa **date de ponte** ou de la
**DCR** imprimée sur la boîte, et indique ce que l'on peut en faire à ce stade.

Application web installable (PWA) : elle s'installe depuis le navigateur sur iPhone, Android
ou ordinateur, sans passer par les stores, et **fonctionne entièrement hors ligne** une fois
ouverte une première fois.

**Adresse : <https://oliviermct.github.io/MonOeuf/>**

| Onglet | Contenu |
|---|---|
| **Fraîcheur** | Saisie d'une date (ponte ou DCR), statut, dates clés, usages autorisés ou déconseillés |
| **Code coquille** | Décodage du code imprimé sur l'œuf : mode d'élevage, pays, identifiant d'élevage |
| **Conserver** | Règles de conservation, test du verre d'eau, différence DCR / DLC |
| **Réglages** | Langue (7 langues), palette de couleurs, thème clair / sombre, installation et état hors ligne |

---

## Installer l'application

- **iPhone / iPad** : ouvrir l'adresse dans Safari, toucher **Partager**, puis
  **Sur l'écran d'accueil**. (Chrome et Edge le proposent aussi depuis iOS 16.4.)
- **Android** : ouvrir l'adresse dans Chrome, puis toucher **Installer l'application**
  (bandeau en haut de l'app, onglet Réglages, ou menu ⋮ de Chrome).
- **Ordinateur** : Chrome ou Edge affichent une icône d'installation dans la barre d'adresse.

Compatibilité : iOS 16.4 ou plus récent (iPhone 8 et suivants), Chrome à jour sur Android,
navigateurs récents sur ordinateur. Sur un navigateur plus ancien, l'app l'explique au lieu de
démarrer.

L'onglet **Réglages** indique « Prête hors ligne » quand tout est en cache : l'app marche
alors sans réseau. Quand une nouvelle version est publiée, un bandeau propose de la charger.

---

## Les règles appliquées

Règlement (CE) n° 589/2008 et recommandations de l'ANSES :

- **Extra frais** : jusqu'à **9 jours** après la ponte → consommation crue ou peu cuite possible
- **Frais** : jusqu'à **28 jours** après la ponte, c'est la **DCR** → bien cuit de préférence
- **Vente au consommateur** interdite au-delà de **21 jours** après la ponte
- **Après la DCR** : la DCR n'est pas une DLC. Coquille intacte + conservation au frais →
  encore consommable **cuit à cœur**, avec le test du verre d'eau. L'app fixe une limite
  de prudence à **DCR + 14 jours**, au-delà de laquelle elle recommande de jeter.

La DCR étant toujours ponte + 28 jours, l'app convertit l'une en l'autre sans ambiguïté.

---

## Structure du dépôt

```
src/OeufFrais.Core/        règles métier et traductions (.resx), sans interface : C# pur
src/OeufFrais.Web/         la PWA, Blazor WebAssembly
tests/OeufFrais.Core.Tests tests unitaires des règles et des traductions (xUnit)
tests/OeufFrais.Web.E2E    tests de bout en bout dans Chromium, dont le hors ligne (Playwright)
scripts/publier-site.sh    prépare le site tel qu'il est mis en ligne
docs/MIGRATION_PWA.md      plan et suivi de la migration MAUI → PWA
```

---

## Développer

Prérequis : SDK .NET 10.

```bash
dotnet watch --project src/OeufFrais.Web        # app en développement, rechargée à chaque modification
dotnet test tests/OeufFrais.Core.Tests          # tests des règles et des traductions
```

En développement, le service worker ne met rien en cache : l'indicateur « Prête hors ligne »
n'y veut rien dire. Pour tester le vrai comportement hors ligne, il faut le site publié :

```bash
scripts/publier-site.sh /MonOeuf/ artifacts/site                # comme sur GitHub Pages
dotnet build tests/OeufFrais.Web.E2E
pwsh tests/OeufFrais.Web.E2E/bin/Debug/net10.0/playwright.ps1 install chromium   # une seule fois
dotnet test tests/OeufFrais.Web.E2E
```

Les textes de l'interface sont dans `src/OeufFrais.Core/Resources/Strings/AppStrings*.resx`
(le fichier sans suffixe est le français). Un test vérifie que chaque clé existe dans les
7 langues avec les mêmes paramètres `{0}`.

---

## Mettre en ligne

Le workflow `.github/workflows/pwa.yml` lance les tests à chaque push et publie `main` sur
GitHub Pages. Réglage à faire une fois : *Settings → Pages → Source* : **GitHub Actions**
(le dépôt doit rester public : GitHub Pages est payant pour un dépôt privé).

Le site est servi sous `/MonOeuf/` (le nom du dépôt) : `scripts/publier-site.sh` écrit ce chemin dans la balise
`<base>` **avant** la publication. Ne jamais modifier un fichier du site après coup : le service
worker vérifie l'empreinte de chaque fichier et refuserait de s'installer.

---

## Avertissement

L'application donne des repères réglementaires, pas un diagnostic. Un œuf fêlé, sale,
à l'odeur suspecte ou au blanc trouble se jette, quelle que soit sa date.
