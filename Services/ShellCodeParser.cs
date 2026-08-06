using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OeufFrais.Services;

public sealed class ShellCode
{
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

    public bool Valide { get; init; }
    public string Erreur { get; init; } = "";
    public string CodeNormalise { get; init; } = "";
    public string ModeElevage { get; init; } = "";
    public string ModeElevageDetail { get; init; } = "";
    public string Pays { get; init; } = "";
    public string IdentifiantElevage { get; init; } = "";

    public string IdentifiantElevageTexte => Loc.GetString("Code_LabelIdentifiant", IdentifiantElevage);
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
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

    private static readonly Regex Format =
        new(@"^([0-3])\s*([A-Z]{2})\s*([A-Z0-9]{2,12})$", RegexOptions.Compiled);

    public static ShellCode Parse(string? saisie)
    {
        if (string.IsNullOrWhiteSpace(saisie))
            return new ShellCode { Erreur = Loc.GetString("ShellCode_Erreur_Vide") };

        var code = saisie.Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        var m = Format.Match(code);

        if (!m.Success)
            return new ShellCode { Erreur = Loc.GetString("ShellCode_Erreur_Format") };

        var chiffre = m.Groups[1].Value[0];

        var (mode, detail) = chiffre switch
        {
            '0' => (Loc.GetString("ShellCode_Mode_Bio"), Loc.GetString("ShellCode_Mode_Bio_Detail")),
            '1' => (Loc.GetString("ShellCode_Mode_PleinAir"), Loc.GetString("ShellCode_Mode_PleinAir_Detail")),
            '2' => (Loc.GetString("ShellCode_Mode_Sol"), Loc.GetString("ShellCode_Mode_Sol_Detail")),
            _ => (Loc.GetString("ShellCode_Mode_Cage"), Loc.GetString("ShellCode_Mode_Cage_Detail"))
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
        "FR" => Loc.GetString("ShellCode_Pays_FR"),
        "BE" => Loc.GetString("ShellCode_Pays_BE"),
        "DE" => Loc.GetString("ShellCode_Pays_DE"),
        "ES" => Loc.GetString("ShellCode_Pays_ES"),
        "IT" => Loc.GetString("ShellCode_Pays_IT"),
        "NL" => Loc.GetString("ShellCode_Pays_NL"),
        "PL" => Loc.GetString("ShellCode_Pays_PL"),
        "PT" => Loc.GetString("ShellCode_Pays_PT"),
        "LU" => Loc.GetString("ShellCode_Pays_LU"),
        "CH" => Loc.GetString("ShellCode_Pays_CH"),
        _ => iso
    };
}
