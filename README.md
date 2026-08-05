# Œuf Frais — application .NET MAUI

Détermine si un œuf est encore consommable à partir de sa **date de ponte** ou de la
**DCR** imprimée sur la boîte, et indique ce que l'on peut en faire à ce stade.

Trois onglets :

| Onglet | Contenu |
|---|---|
| **Fraîcheur** | Saisie d'une date (ponte ou DCR), statut, dates clés, usages autorisés ou déconseillés |
| **Code coquille** | Décodage du code imprimé sur l'œuf : mode d'élevage, pays, identifiant d'élevage |
| **Conserver** | Règles de conservation, test du verre d'eau, différence DCR / DLC |

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

## Mise en place du projet

Les fichiers de ce dossier sont le **code applicatif**. Le squelette de plateforme
(`Platforms/`, `Resources/`, `.csproj`) est généré par le template officiel :

```bash
dotnet workload install maui
dotnet new maui -n OeufFrais
```

Copiez ensuite le contenu de ce dossier dans le projet généré, en écrasant les fichiers
existants :

```
MauiProgram.cs
App.xaml            App.xaml.cs
AppShell.xaml       AppShell.xaml.cs
MainPage.xaml       MainPage.xaml.cs
CodePage.xaml       CodePage.xaml.cs
ConseilsPage.xaml   ConseilsPage.xaml.cs
Services/EggCalculator.cs
Services/ShellCodeParser.cs
```

Puis, dans `OeufFrais.csproj`, ajustez :

```xml
<ApplicationTitle>Œuf Frais</ApplicationTitle>
<ApplicationId>fr.olivierastre.oeuffrais</ApplicationId>
```

Vérifiez que le fichier est bien enregistré en **UTF-8** (les accents et le « Œ »).

---

## Tester

**Sur Windows**, le plus rapide pour voir l'interface :

```bash
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

**Sur iPhone**, MAUI a besoin d'un Mac quelque part dans la chaîne :

- *Mac* : `dotnet build -t:Run -f net9.0-ios` après avoir ouvert le projet sur macOS
- *Windows + Mac sur le réseau* : Visual Studio 2022, appairage Mac (Pair to Mac)
- *Windows seul* : **Hot Restart** dans Visual Studio 2022 déploie sur un iPhone
  physique branché en USB sans Mac. Il faut iTunes installé et un compte Apple
  Developer (99 €/an pour un déploiement durable ; un compte gratuit fonctionne
  aussi mais l'app expire au bout de 7 jours et doit être réinstallée).

Cible minimale : iOS 15.

---

## Pistes d'évolution

- **Scan photo du code coquille** : `ZXing.Net.Maui` pour le code-barres de la boîte,
  ou une reconnaissance de texte (Vision sur iOS) pour lire le code imprimé sur l'œuf.
  Le parseur `ShellCodeParser` accepte déjà n'importe quelle chaîne, il suffit de lui
  passer le texte reconnu.
- **Suivi de plusieurs boîtes** : persister une liste avec `Preferences` ou SQLite.
- **Notification** quelques jours avant la DCR, via `Plugin.LocalNotification`.

---

## Avertissement

L'application donne des repères réglementaires, pas un diagnostic. Un œuf fêlé, sale,
à l'odeur suspecte ou au blanc trouble se jette, quelle que soit sa date.
