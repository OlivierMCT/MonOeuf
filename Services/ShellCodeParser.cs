using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OeufFrais.Services;

public sealed class ShellCode
{
    public bool Valide { get; init; }
    public string Erreur { get; init; } = "";
    public string CodeNormalise { get; init; } = "";
    public string ModeElevage { get; init; } = "";
    public string ModeElevageDetail { get; init; } = "";
    public string Pays { get; init; } = "";
    public string IdentifiantElevage { get; init; } = "";
}

/// <summary>
/// Décodage du code imprimé sur la coquille (règlement (CE) n° 589/2008).
/// Format : [chiffre 0-3][code pays ISO 2 lettres][identifiant de l'élevage]
/// Exemple : 3FRABC01
///
/// Attention : ce code n'indique PAS la date de ponte. Seule la DCR
/// imprimée sur la boîte (ou parfois sur la coquille) permet de la retrouver.
/// </summary>
public static class ShellCodeParser
{
    private static readonly Regex Format =
        new(@"^([0-3])\s*([A-Z]{2})\s*([A-Z0-9]{2,12})$", RegexOptions.Compiled);

    public static ShellCode Parse(string? saisie)
    {
        if (string.IsNullOrWhiteSpace(saisie))
            return new ShellCode { Erreur = "Saisissez le code imprimé sur la coquille." };

        var code = saisie.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        var m = Format.Match(code);

        if (!m.Success)
            return new ShellCode
            {
                Erreur = "Code non reconnu. Format attendu : un chiffre (0 à 3), " +
                         "le code pays sur 2 lettres, puis l'identifiant de l'élevage. Exemple : 3FRABC01."
            };

        var chiffre = m.Groups[1].Value[0];

        var (mode, detail) = chiffre switch
        {
            '0' => ("Biologique",
                    "Poules élevées en plein air, alimentation issue de l'agriculture biologique, 6 poules par m² maximum en bâtiment et 4 m² de parcours extérieur par poule."),
            '1' => ("Plein air",
                    "Poules ayant accès à un parcours extérieur en journée, 4 m² par poule, alimentation non biologique."),
            '2' => ("Au sol",
                    "Poules élevées en bâtiment, sans cage mais sans accès à l'extérieur, jusqu'à 9 poules par m²."),
            _ => ("Cage / volière aménagée",
                    "Poules élevées en cage aménagée, environ 750 cm² par poule, sans accès à l'extérieur.")
        };

        return new ShellCode
        {
            Valide = true,
            CodeNormalise = code,
            ModeElevage = mode,
            ModeElevageDetail = detail,
            Pays = NomPays(m.Groups[2].Value),
            IdentifiantElevage = m.Groups[3].Value
        };
    }

    private static readonly HashSet<string> PaysConnus = new(StringComparer.Ordinal)
    {
        "FR","BE","DE","ES","IT","NL","PL","PT","LU","CH","AT","DK","IE","GB","UK",
        "SE","FI","CZ","HU","RO","GR","SK","SI","HR","BG","LT","LV","EE","MT","CY"
    };

    // Candidat ancré : le premier caractère est un chiffre 0-3, que l'OCR confond
    // volontiers avec une lettre ; suivent le code pays et l'identifiant d'élevage.
    private static readonly Regex Candidat =
        new(@"^([0-3OQDB])([A-Z]{2})([A-Z0-9]{2,12})$", RegexOptions.Compiled);

    /// <summary>
    /// Cherche un code d'élevage dans le texte brut d'un OCR de coquille.
    /// Renvoie la meilleure chaîne trouvée, à proposer à l'utilisateur pour
    /// confirmation — jamais à valider sans relecture, la coquille est une
    /// surface trop ingrate pour se fier au résultat.
    /// </summary>
    public static string? TrouverCode(string? texteOcr)
    {
        if (string.IsNullOrWhiteSpace(texteOcr)) return null;

        var meilleur = (Code: (string?)null, Score: 0);

        foreach (var ligne in texteOcr.Split('\n', '\r'))
        {
            var mots = Decouper(ligne);

            // Le code est imprimé d'un seul tenant, mais l'OCR le coupe parfois
            // (« 3 FR ABC01 ») : on teste aussi les groupes de mots consécutifs.
            for (var i = 0; i < mots.Count; i++)
            {
                var assemble = "";
                for (var n = 0; n < 3 && i + n < mots.Count; n++)
                {
                    assemble += mots[i + n];
                    Evaluer(assemble, ref meilleur);
                }
            }
        }

        return meilleur.Code;
    }

    private static void Evaluer(string mot, ref (string? Code, int Score) meilleur)
    {
        var m = Candidat.Match(mot);
        if (!m.Success) return;

        var pays = m.Groups[2].Value;

        // Sans code pays reconnaissable, on est presque sûrement sur du bruit
        // (« DU JOUR », « OEUFS »…). Mieux vaut ne rien proposer.
        if (!PaysConnus.Contains(pays)) return;

        var brut = m.Groups[1].Value[0];
        var tete = brut switch
        {
            'O' or 'Q' or 'D' => '0',
            'B' => '3',
            _ => brut
        };

        var reste = m.Groups[3].Value;

        var score = 50;
        if (pays == "FR") score += 10;
        if (brut is >= '0' and <= '3') score += 20;   // aucune correction nécessaire
        if (reste.Length is >= 4 and <= 8) score += 10;

        if (score > meilleur.Score)
            meilleur = ($"{tete}{pays}{reste}", score);
    }

    /// <summary>Découpe en mots alphanumériques, en majuscules et sans accents.</summary>
    private static List<string> Decouper(string s)
    {
        var sansAccents = new string(s.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());

        return Regex.Split(sansAccents.ToUpperInvariant(), @"[^A-Z0-9]+")
            .Where(t => t.Length > 0)
            .ToList();
    }

    private static string NomPays(string iso) => iso switch
    {
        "FR" => "France (FR)",
        "BE" => "Belgique (BE)",
        "DE" => "Allemagne (DE)",
        "ES" => "Espagne (ES)",
        "IT" => "Italie (IT)",
        "NL" => "Pays-Bas (NL)",
        "PL" => "Pologne (PL)",
        "PT" => "Portugal (PT)",
        "LU" => "Luxembourg (LU)",
        "CH" => "Suisse (CH)",
        _ => iso
    };
}
