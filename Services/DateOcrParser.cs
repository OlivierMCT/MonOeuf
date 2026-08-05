using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OeufFrais.Services;

/// <summary>Une date trouvée dans le texte reconnu, avec son contexte et un score de confiance.</summary>
public sealed record DateCandidate(DateTime Date, string Extrait, int Score)
{
    public string Libelle => Date.ToString("d MMMM yyyy");
}

/// <summary>
/// Extraction de la DCR à partir du texte brut renvoyé par l'OCR d'une boîte d'œufs.
///
/// Les boîtes françaises impriment la date sous des formes assez variées :
///   « À CONSOMMER DE PRÉFÉRENCE AVANT LE 20/08/2026 »
///   « DCR 20.08.26 »
///   « 20 AOUT 2026 »
///   « 20-08-26 »
/// et l'OCR y ajoute ses propres approximations. On collecte donc tous les candidats,
/// on les note, et on laisse l'utilisateur trancher en cas d'ambiguïté.
/// </summary>
public static class DateOcrParser
{
    /// <summary>Fenêtre de plausibilité : une DCR lisible sur une boîte reste proche d'aujourd'hui.</summary>
    private const int JoursPasse = 60;
    private const int JoursFutur = 45;

    private static readonly string[] MotsClesForts =
    {
        "CONSOMMER", "PREFERENCE", "DCR", "DLC", "CONSOMMATION"
    };

    private static readonly string[] MotsClesFaibles = { "AVANT", "LE", "EXP" };

    private static readonly Dictionary<string, int> Mois = new(StringComparer.Ordinal)
    {
        ["JANV"] = 1, ["JANVIER"] = 1, ["JAN"] = 1,
        ["FEVR"] = 2, ["FEVRIER"] = 2, ["FEV"] = 2,
        ["MARS"] = 3, ["MAR"] = 3,
        ["AVRIL"] = 4, ["AVR"] = 4,
        ["MAI"] = 5,
        ["JUIN"] = 6,
        ["JUIL"] = 7, ["JUILLET"] = 7,
        ["AOUT"] = 8, ["AOU"] = 8,
        ["SEPT"] = 9, ["SEPTEMBRE"] = 9, ["SEP"] = 9,
        ["OCT"] = 10, ["OCTOBRE"] = 10,
        ["NOV"] = 11, ["NOVEMBRE"] = 11,
        ["DEC"] = 12, ["DECEMBRE"] = 12
    };

    // 20/08/2026 · 20.08.26 · 20-8-2026
    // Le séparateur est capturé puis exigé identique des deux côtés : sans cela,
    // une suite de nombres sans rapport sur l'étiquette produit de fausses dates.
    private static readonly Regex Numerique =
        new(@"(?<!\d)(\d{1,2})\s*([/.\-])\s*(\d{1,2})\s*\2\s*(\d{2,4})(?!\d)", RegexOptions.Compiled);

    // 20 AOUT 2026 · 20AOU26
    private static readonly Regex AvecMois =
        new(@"(?<!\d)(\d{1,2})\s*(JANVIER|JANV|JAN|FEVRIER|FEVR|FEV|MARS|MAR|AVRIL|AVR|MAI|JUIN|JUILLET|JUIL|AOUT|AOU|SEPTEMBRE|SEPT|SEP|OCTOBRE|OCT|NOVEMBRE|NOV|DECEMBRE|DEC)\.?\s*(\d{2,4})?(?!\d)",
            RegexOptions.Compiled);

    // 200826 collé, uniquement retenu si un mot-clé traîne dans la même ligne.
    private static readonly Regex Compact =
        new(@"(?<!\d)(\d{2})(\d{2})(\d{2})(?!\d)", RegexOptions.Compiled);

    /// <summary>Meilleur candidat, ou null si rien de plausible n'a été trouvé.</summary>
    public static DateCandidate? Meilleur(string? texte, DateTime? aujourdHui = null)
        => Extraire(texte, aujourdHui).FirstOrDefault();

    /// <summary>Tous les candidats plausibles, du plus au moins probable.</summary>
    public static IReadOnlyList<DateCandidate> Extraire(string? texte, DateTime? aujourdHui = null)
    {
        if (string.IsNullOrWhiteSpace(texte)) return Array.Empty<DateCandidate>();

        var today = (aujourdHui ?? DateTime.Today).Date;
        var min = today.AddDays(-JoursPasse);
        var max = today.AddDays(JoursFutur);
        var candidats = new List<DateCandidate>();

        foreach (var ligneBrute in texte.Split('\n', '\r'))
        {
            var ligne = Normaliser(ligneBrute);
            if (ligne.Length == 0) continue;

            var bonus = Bonus(ligne);

            foreach (Match m in Numerique.Matches(ligne))
            {
                var d = Construire(
                    Chiffres(m.Groups[1].Value),
                    Chiffres(m.Groups[3].Value),
                    Chiffres(m.Groups[4].Value));
                Ajouter(candidats, d, m.Value, bonus + 20, min, max, today);
            }

            foreach (Match m in AvecMois.Matches(ligne))
            {
                if (!Mois.TryGetValue(m.Groups[2].Value, out var mois)) continue;
                var annee = m.Groups[3].Success ? Chiffres(m.Groups[3].Value) : today.Year;
                var d = Construire(Chiffres(m.Groups[1].Value), mois, annee);
                // Un mois écrit en toutes lettres se confond difficilement : on lui fait confiance.
                Ajouter(candidats, d, m.Value, bonus + 30, min, max, today);
            }

            if (bonus > 0)
            {
                foreach (Match m in Compact.Matches(ligne))
                {
                    var d = Construire(
                        Chiffres(m.Groups[1].Value),
                        Chiffres(m.Groups[2].Value),
                        Chiffres(m.Groups[3].Value));
                    Ajouter(candidats, d, m.Value, bonus, min, max, today);
                }
            }
        }

        return candidats
            .GroupBy(c => c.Date)
            .Select(g => g.OrderByDescending(c => c.Score).First())
            .OrderByDescending(c => c.Score)
            .ThenBy(c => Math.Abs((c.Date - today).Days))
            .ToList();
    }

    private static void Ajouter(List<DateCandidate> liste, DateTime? d, string extrait,
                                int score, DateTime min, DateTime max, DateTime today)
    {
        if (d is null) return;
        if (d < min || d > max) return;

        // À score égal, une date proche d'aujourd'hui est plus vraisemblable
        // qu'une date aux bords de la fenêtre.
        var proximite = Math.Max(0, 20 - Math.Abs((d.Value - today).Days) / 3);
        liste.Add(new DateCandidate(d.Value, extrait.Trim(), score + proximite));
    }

    private static int Bonus(string ligne)
    {
        var score = 0;
        if (MotsClesForts.Any(ligne.Contains)) score += 40;
        if (MotsClesFaibles.Any(m => Regex.IsMatch(ligne, $@"\b{m}\b"))) score += 10;
        return score;
    }

    /// <summary>
    /// Jour/mois/année à la française. On tente une inversion si le jour dépasse 12
    /// en deuxième position — certaines boîtes importées impriment mois/jour.
    /// </summary>
    private static DateTime? Construire(int j, int m, int a)
    {
        if (a < 100) a += 2000;
        if (a is < 2020 or > 2099) return null;

        if (m > 12 && j <= 12) (j, m) = (m, j);
        if (m is < 1 or > 12) return null;
        if (j < 1 || j > DateTime.DaysInMonth(a, m)) return null;

        return new DateTime(a, m, j);
    }

    private static int Chiffres(string s)
        => int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : -1;

    /// <summary>Majuscules, sans accents, avec les confusions OCR classiques corrigées.</summary>
    private static string Normaliser(string s)
    {
        var sansAccents = new string(s.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        var sb = new StringBuilder(sansAccents.Length);
        foreach (var c in sansAccents.ToUpperInvariant())
        {
            sb.Append(c switch
            {
                'O' or 'Q' => 'O',   // conservé, la désambiguïsation se fait plus bas
                _ => c
            });
        }

        var texte = sb.ToString();

        // Dans un contexte manifestement numérique, O et I sont presque toujours 0 et 1.
        texte = Regex.Replace(texte, @"(?<=\d)[OQ](?=[\d/.\-])", "0");
        texte = Regex.Replace(texte, @"(?<=[\d/.\-])[OQ](?=\d)", "0");
        texte = Regex.Replace(texte, @"(?<=\d)[IL](?=[\d/.\-])", "1");
        texte = Regex.Replace(texte, @"(?<=[\d/.\-])[IL](?=\d)", "1");

        return texte.Trim();
    }
}
