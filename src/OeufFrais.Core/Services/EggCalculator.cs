using System;
using System.Collections.Generic;

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
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

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

    // Propriétés d'affichage, utilisées directement par les vues. Les couleurs restent des
    // chaînes hexadécimales : chaque interface les convertit dans son propre type.
    public string PonteTexte => DatePonte.ToString("dddd d MMMM yyyy", Loc.Culture);
    public string ExtraFraisTexte => FinExtraFrais.ToString("d MMMM yyyy", Loc.Culture);
    public string VenteTexte => LimiteVente.ToString("d MMMM yyyy", Loc.Culture);
    public string DcrTexte => Dcr.ToString("d MMMM yyyy", Loc.Culture);
    public string PrudenceTexte => LimitePrudence.ToString("d MMMM yyyy", Loc.Culture);
    public string AgeTexte => AgeJours < 0 ? "—" : Loc.Plural("Common_Jour_Un", "Common_Jour_Plusieurs", AgeJours);
}

public sealed record UsageConseil(string Usage, bool Autorise, string Detail)
{
    public string Icone => Autorise ? "✓" : "✕";
    public string CouleurHex => Autorise ? "#2E9E4F" : "#C0392B";
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
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

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
        var joursAvantDcr = (int)(dcr - today).TotalDays;

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
            JoursAvantDcr = joursAvantDcr,
            Statut = statut,
            Titre = Titre(statut),
            Resume = Resume(statut, age, joursAvantDcr),
            CouleurHex = Couleur(statut),
            Conseils = Conseils(statut),
            TestVerreDEauRecommande = statut is Freshness.ApresDcr or Freshness.Douteux
        };
    }

    private static string Titre(Freshness s) => s switch
    {
        Freshness.Future => Loc.GetString("Egg_Titre_Future"),
        Freshness.ExtraFrais => Loc.GetString("Egg_Titre_ExtraFrais"),
        Freshness.Frais => Loc.GetString("Egg_Titre_Frais"),
        Freshness.ApresDcr => Loc.GetString("Egg_Titre_ApresDcr"),
        _ => Loc.GetString("Egg_Titre_Douteux")
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
        Freshness.Future => Loc.GetString("Egg_Resume_Future"),

        Freshness.ExtraFrais => Loc.GetString("Egg_Resume_ExtraFrais",
            Loc.Plural("Common_Jour_Un", "Common_Jour_Plusieurs", age),
            joursAvantDcr),

        Freshness.Frais => Loc.GetString("Egg_Resume_Frais", age,
            joursAvantDcr == 0
                ? Loc.GetString("Egg_Frais_Aujourdhui")
                : Loc.Plural("Common_JourRestant_Un", "Common_JourRestant_Plusieurs", joursAvantDcr)),

        Freshness.ApresDcr => Loc.GetString("Egg_Resume_ApresDcr", age,
            Loc.Plural("Common_Jour_Un", "Common_Jour_Plusieurs", -joursAvantDcr)),

        _ => Loc.GetString("Egg_Resume_Douteux", age)
    };

    private static IReadOnlyList<UsageConseil> Conseils(Freshness s) => s switch
    {
        Freshness.Future => Array.Empty<UsageConseil>(),

        Freshness.ExtraFrais => new[]
        {
            new UsageConseil(Loc.GetString("Egg_Conseil_EF_Cru_Usage"), true, Loc.GetString("Egg_Conseil_EF_Cru_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_EF_Coque_Usage"), true, Loc.GetString("Egg_Conseil_EF_Coque_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_EF_Plat_Usage"), true, Loc.GetString("Egg_Conseil_EF_Plat_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_EF_Dur_Usage"), true, Loc.GetString("Egg_Conseil_EF_Dur_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_EF_Patisserie_Usage"), true, Loc.GetString("Egg_Conseil_EF_Patisserie_Detail"))
        },

        Freshness.Frais => new[]
        {
            new UsageConseil(Loc.GetString("Egg_Conseil_F_Cru_Usage"), false, Loc.GetString("Egg_Conseil_F_Cru_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_F_Coque_Usage"), false, Loc.GetString("Egg_Conseil_F_Coque_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_F_Plat_Usage"), true, Loc.GetString("Egg_Conseil_F_Plat_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_F_Dur_Usage"), true, Loc.GetString("Egg_Conseil_F_Dur_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_F_Patisserie_Usage"), true, Loc.GetString("Egg_Conseil_F_Patisserie_Detail"))
        },

        Freshness.ApresDcr => new[]
        {
            new UsageConseil(Loc.GetString("Egg_Conseil_AD_Cru_Usage"), false, Loc.GetString("Egg_Conseil_AD_Cru_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_AD_Dur_Usage"), true, Loc.GetString("Egg_Conseil_AD_Dur_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_AD_Cuisson_Usage"), true, Loc.GetString("Egg_Conseil_AD_Cuisson_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_AD_Odeur_Usage"), false, Loc.GetString("Egg_Conseil_AD_Odeur_Detail"))
        },

        _ => new[]
        {
            new UsageConseil(Loc.GetString("Egg_Conseil_D_Tous_Usage"), false, Loc.GetString("Egg_Conseil_D_Tous_Detail")),
            new UsageConseil(Loc.GetString("Egg_Conseil_D_NeCassez_Usage"), false, Loc.GetString("Egg_Conseil_D_NeCassez_Detail"))
        }
    };

    /// <summary>Conseils de conservation, valables quel que soit l'âge.</summary>
    public static string[] Conservation => new[]
    {
        Loc.GetString("Egg_Conservation_1"),
        Loc.GetString("Egg_Conservation_2"),
        Loc.GetString("Egg_Conservation_3"),
        Loc.GetString("Egg_Conservation_4"),
        Loc.GetString("Egg_Conservation_5")
    };

    /// <summary>Le test du verre d'eau, décrit étape par étape.</summary>
    public static string[] TestVerreDEau => new[]
    {
        Loc.GetString("Egg_TestVerre_1"),
        Loc.GetString("Egg_TestVerre_2"),
        Loc.GetString("Egg_TestVerre_3"),
        Loc.GetString("Egg_TestVerre_4")
    };
}
