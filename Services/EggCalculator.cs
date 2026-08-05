using System;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace OeufFrais.Services;

public enum Freshness
{
    Future,        // date saisie dans le futur
    ExtraFrais,    // 0 - 9 jours après ponte
    Frais,         // 10 - 28 jours (jusqu'à la DCR)
    ApresDcr,      // 29 jours - ~6 semaines
    Douteux        // au-delà : à ne plus consommer
}

/// <summary>
/// Résultat complet de l'évaluation d'un œuf.
/// </summary>
public sealed class EggAssessment
{
    public DateTime DatePonte { get; init; }
    public DateTime FinExtraFrais { get; init; }   // ponte + 9 j
    public DateTime LimiteVente { get; init; }     // ponte + 21 j
    public DateTime Dcr { get; init; }             // ponte + 28 j
    public DateTime LimitePrudence { get; init; }  // DCR + 14 j

    public int AgeJours { get; init; }
    public int JoursAvantDcr { get; init; }        // négatif si DCR dépassée
    public Freshness Statut { get; init; }

    public string Titre { get; init; } = "";
    public string Resume { get; init; } = "";
    public string CouleurHex { get; init; } = "#8A8A8A";

    /// <summary>Ce que l'on peut faire de l'œuf, du plus au moins permissif.</summary>
    public IReadOnlyList<UsageConseil> Conseils { get; init; } = Array.Empty<UsageConseil>();

    /// <summary>Vrai si le test du verre d'eau est recommandé avant consommation.</summary>
    public bool TestVerreDEauRecommande { get; init; }

    // Propriétés d'affichage, utilisées directement par les bindings XAML.
    public Color CouleurStatut => Color.FromArgb(CouleurHex);
    public Color CouleurStatutClaire => CouleurStatut.WithAlpha(0.16f);
    public string PonteTexte => DatePonte.ToString("dddd d MMMM yyyy");
    public string ExtraFraisTexte => FinExtraFrais.ToString("d MMMM yyyy");
    public string VenteTexte => LimiteVente.ToString("d MMMM yyyy");
    public string DcrTexte => Dcr.ToString("d MMMM yyyy");
    public string PrudenceTexte => LimitePrudence.ToString("d MMMM yyyy");
    public string AgeTexte => AgeJours < 0 ? "—" : $"{AgeJours} jour{(AgeJours > 1 ? "s" : "")}";
}

public sealed record UsageConseil(string Usage, bool Autorise, string Detail)
{
    public string Icone => Autorise ? "✓" : "✕";
    public Color Couleur => Autorise ? Color.FromArgb("#2E9E4F") : Color.FromArgb("#C0392B");
    public Color CouleurClaire => Couleur.WithAlpha(0.16f);
}

/// <summary>
/// Calcul des échéances d'un œuf selon le règlement (CE) n° 589/2008
/// et les recommandations de l'ANSES.
///
///   - Extra frais : jusqu'à 9 jours après la ponte
///   - Frais       : jusqu'à la DCR, soit 28 jours après la ponte
///   - Vente au consommateur : interdite au-delà de 21 jours après la ponte
///     (soit 7 jours avant la DCR)
/// </summary>
public static class EggCalculator
{
    public const int JoursExtraFrais = 9;
    public const int JoursLimiteVente = 21;
    public const int JoursDcr = 28;
    /// <summary>Marge de prudence après la DCR, œuf intact et gardé au frais, cuit à cœur.</summary>
    public const int JoursApresDcr = 14;

    public static EggAssessment DepuisPonte(DateTime datePonte, DateTime? aujourdHui = null)
        => Evaluer(datePonte.Date, (aujourdHui ?? DateTime.Today).Date);

    /// <summary>La DCR imprimée sur la boîte vaut ponte + 28 jours.</summary>
    public static EggAssessment DepuisDcr(DateTime dcr, DateTime? aujourdHui = null)
        => Evaluer(dcr.Date.AddDays(-JoursDcr), (aujourdHui ?? DateTime.Today).Date);

    private static EggAssessment Evaluer(DateTime ponte, DateTime today)
    {
        var age = (int)(today - ponte).TotalDays;
        var dcr = ponte.AddDays(JoursDcr);

        var statut = age switch
        {
            < 0 => Freshness.Future,
            <= JoursExtraFrais => Freshness.ExtraFrais,
            <= JoursDcr => Freshness.Frais,
            <= JoursDcr + JoursApresDcr => Freshness.ApresDcr,
            _ => Freshness.Douteux
        };

        return new EggAssessment
        {
            DatePonte = ponte,
            FinExtraFrais = ponte.AddDays(JoursExtraFrais),
            LimiteVente = ponte.AddDays(JoursLimiteVente),
            Dcr = dcr,
            LimitePrudence = dcr.AddDays(JoursApresDcr),
            AgeJours = age,
            JoursAvantDcr = (int)(dcr - today).TotalDays,
            Statut = statut,
            Titre = Titre(statut),
            Resume = Resume(statut, age, (int)(dcr - today).TotalDays),
            CouleurHex = Couleur(statut),
            Conseils = Conseils(statut),
            TestVerreDEauRecommande = statut is Freshness.ApresDcr or Freshness.Douteux
        };
    }

    private static string Titre(Freshness s) => s switch
    {
        Freshness.Future => "Date à venir",
        Freshness.ExtraFrais => "Extra frais",
        Freshness.Frais => "Frais",
        Freshness.ApresDcr => "DCR dépassée",
        _ => "À ne plus consommer"
    };

    private static string Couleur(Freshness s) => s switch
    {
        Freshness.Future => "#8A8A8A",
        Freshness.ExtraFrais => "#2E9E4F",   // vert
        Freshness.Frais => "#E8A31C",        // ambre
        Freshness.ApresDcr => "#E2711D",     // orange
        _ => "#C0392B"                       // rouge
    };

    private static string Resume(Freshness s, int age, int joursAvantDcr) => s switch
    {
        Freshness.Future =>
            "La date saisie est dans le futur. Vérifiez la date de ponte ou la DCR indiquée sur la boîte.",
        Freshness.ExtraFrais =>
            $"Pondu il y a {age} jour{(age > 1 ? "s" : "")}. Tous les usages sont possibles, y compris cru " +
            $"ou peu cuit. Encore {joursAvantDcr} jours avant la DCR.",
        Freshness.Frais =>
            $"Pondu il y a {age} jours. Toujours dans sa période de consommation recommandée " +
            (joursAvantDcr == 0
                ? "— la DCR tombe aujourd'hui même"
                : $"({joursAvantDcr} jour{(joursAvantDcr > 1 ? "s" : "")} restant{(joursAvantDcr > 1 ? "s" : "")})") +
            ", mais à consommer bien cuit de préférence.",
        Freshness.ApresDcr =>
            $"Pondu il y a {age} jours, soit {-joursAvantDcr} jour{(-joursAvantDcr > 1 ? "s" : "")} après la DCR. " +
            "La DCR n'est pas une DLC : si la coquille est intacte et l'œuf resté au frais, il est encore " +
            "consommable, mais uniquement cuit à cœur. Faites le test du verre d'eau avant.",
        _ =>
            $"Pondu il y a {age} jours. Trop ancien : il vaut mieux le jeter, même s'il paraît normal."
    };

    private static IReadOnlyList<UsageConseil> Conseils(Freshness s) => s switch
    {
        Freshness.Future => Array.Empty<UsageConseil>(),

        Freshness.ExtraFrais => new[]
        {
            new UsageConseil("Cru (mayonnaise, tiramisu, mousse au chocolat)", true,
                "Réservé aux œufs extra frais. Préparez au dernier moment, gardez au réfrigérateur et consommez dans les 24 h. À éviter pour les enfants, femmes enceintes, personnes âgées ou immunodéprimées."),
            new UsageConseil("Œuf à la coque, mollet, poché", true,
                "C'est le moment idéal : le blanc est ferme et se tient bien."),
            new UsageConseil("Au plat, brouillé, omelette", true, "Aucune restriction."),
            new UsageConseil("Œuf dur", true,
                "Possible, mais un œuf très frais s'écale mal. Attendez 7 à 10 jours pour un épluchage facile."),
            new UsageConseil("Pâtisserie, cuisson au four", true, "Aucune restriction.")
        },

        Freshness.Frais => new[]
        {
            new UsageConseil("Cru (mayonnaise, tiramisu, mousse au chocolat)", false,
                "Déconseillé au-delà de 9 jours après la ponte. Préférez un œuf extra frais."),
            new UsageConseil("Œuf à la coque, mollet, poché", false,
                "Peu cuit : à éviter. Le jaune n'atteint pas une température suffisante."),
            new UsageConseil("Au plat, brouillé, omelette", true,
                "Oui, en cuisant bien le blanc et le jaune."),
            new UsageConseil("Œuf dur", true, "Usage idéal à ce stade, et il s'écale facilement."),
            new UsageConseil("Pâtisserie, cuisson au four", true, "Aucune restriction.")
        },

        Freshness.ApresDcr => new[]
        {
            new UsageConseil("Cru ou peu cuit", false, "Exclu. Aucune préparation crue, à la coque, molle ou pochée."),
            new UsageConseil("Œuf dur", true,
                "Possible si le test du verre d'eau est concluant. Cuisson 10 min à partir de l'ébullition."),
            new UsageConseil("Cuisson à cœur (cake, quiche, gratin, omelette bien cuite)", true,
                "Le blanc et le jaune doivent être entièrement pris. Visez 70 °C à cœur."),
            new UsageConseil("Odeur ou aspect anormal", false,
                "Cassez l'œuf dans un bol à part : la moindre odeur soufrée ou un blanc trouble et liquide = poubelle.")
        },

        _ => new[]
        {
            new UsageConseil("Tous usages", false,
                "Au-delà d'environ six semaines après la ponte, le risque ne vaut pas l'économie. Jetez l'œuf."),
            new UsageConseil("Ne le cassez pas au-dessus d'un plat", false,
                "Un œuf gâté peut contaminer toute une préparation.")
        }
    };

    /// <summary>Conseils de conservation, valables quel que soit l'âge.</summary>
    public static readonly string[] Conservation =
    {
        "Ne lavez jamais les œufs : cela détruit la cuticule protectrice de la coquille.",
        "Gardez-les à température constante. Un œuf sorti du froid puis remis au chaud se couvre de condensation, qui facilite le passage des bactéries.",
        "Au réfrigérateur, placez-les sur une clayette plutôt que dans la porte, où la température varie.",
        "Rangez-les pointe vers le bas, à l'écart des aliments odorants : la coquille est poreuse.",
        "Une fois cuits durs et écalés, consommez-les dans les 2 à 3 jours."
    };

    /// <summary>Le test du verre d'eau, décrit étape par étape.</summary>
    public static readonly string[] TestVerreDEau =
    {
        "Remplissez un verre d'eau froide et plongez-y l'œuf.",
        "Il reste couché au fond : très frais, aucun souci.",
        "Il se redresse à la verticale au fond : encore consommable, mais bien cuit.",
        "Il flotte : la poche d'air est trop grande, jetez-le sans l'ouvrir."
    };
}
