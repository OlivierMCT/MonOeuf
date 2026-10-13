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
