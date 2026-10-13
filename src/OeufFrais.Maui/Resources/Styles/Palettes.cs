using Microsoft.Maui.Graphics;

namespace OeufFrais.Resources.Styles;

public enum Palette { Miel, Lagon, Corail }

/// <summary>
/// Valeurs concrètes des 7 jetons sémantiques (CouleurFond, CouleurAccent, CouleurAccentFonce,
/// CouleurTexte, CouleurTexteDouce, CouleurBordure, CouleurCarte) pour chaque palette et chaque
/// variante claire/sombre. Consommé par ThemeService, qui écrit ces valeurs dans
/// Application.Current.Resources au démarrage et à chaque changement de réglage.
/// </summary>
public static class Palettes
{
    public static readonly Dictionary<string, Color> MielClair = new()
    {
        ["CouleurFond"] = Color.FromArgb("#FFF3DC"),
        ["CouleurAccent"] = Color.FromArgb("#FFC738"),
        ["CouleurAccentFonce"] = Color.FromArgb("#E8960B"),
        ["CouleurTexte"] = Color.FromArgb("#2B2622"),
        ["CouleurTexteDouce"] = Color.FromArgb("#6B625A"),
        ["CouleurBordure"] = Color.FromArgb("#F5DDA8"),
        ["CouleurCarte"] = Color.FromArgb("#FFFFFF"),
    };

    public static readonly Dictionary<string, Color> MielSombre = new()
    {
        ["CouleurFond"] = Color.FromArgb("#1F1A12"),
        ["CouleurAccent"] = Color.FromArgb("#FFC738"),
        ["CouleurAccentFonce"] = Color.FromArgb("#FFD766"),
        ["CouleurTexte"] = Color.FromArgb("#F5EFE6"),
        ["CouleurTexteDouce"] = Color.FromArgb("#B8AC9B"),
        ["CouleurBordure"] = Color.FromArgb("#4A3D28"),
        ["CouleurCarte"] = Color.FromArgb("#2A2319"),
    };

    public static readonly Dictionary<string, Color> LagonClair = new()
    {
        ["CouleurFond"] = Color.FromArgb("#E7F7F4"),
        ["CouleurAccent"] = Color.FromArgb("#21B8A6"),
        ["CouleurAccentFonce"] = Color.FromArgb("#0F8C7D"),
        ["CouleurTexte"] = Color.FromArgb("#173330"),
        ["CouleurTexteDouce"] = Color.FromArgb("#54706B"),
        ["CouleurBordure"] = Color.FromArgb("#BEE7DF"),
        ["CouleurCarte"] = Color.FromArgb("#FFFFFF"),
    };

    public static readonly Dictionary<string, Color> LagonSombre = new()
    {
        ["CouleurFond"] = Color.FromArgb("#0F1F1C"),
        ["CouleurAccent"] = Color.FromArgb("#2DD9C4"),
        ["CouleurAccentFonce"] = Color.FromArgb("#21B8A6"),
        ["CouleurTexte"] = Color.FromArgb("#E7F5F2"),
        ["CouleurTexteDouce"] = Color.FromArgb("#9FC3BC"),
        ["CouleurBordure"] = Color.FromArgb("#1F4640"),
        ["CouleurCarte"] = Color.FromArgb("#16302B"),
    };

    public static readonly Dictionary<string, Color> CorailClair = new()
    {
        ["CouleurFond"] = Color.FromArgb("#FFEDE8"),
        ["CouleurAccent"] = Color.FromArgb("#FF6F59"),
        ["CouleurAccentFonce"] = Color.FromArgb("#E24B33"),
        ["CouleurTexte"] = Color.FromArgb("#3A2420"),
        ["CouleurTexteDouce"] = Color.FromArgb("#7A5C55"),
        ["CouleurBordure"] = Color.FromArgb("#FFD3C6"),
        ["CouleurCarte"] = Color.FromArgb("#FFFFFF"),
    };

    public static readonly Dictionary<string, Color> CorailSombre = new()
    {
        ["CouleurFond"] = Color.FromArgb("#241512"),
        ["CouleurAccent"] = Color.FromArgb("#FF8A73"),
        ["CouleurAccentFonce"] = Color.FromArgb("#FF6F59"),
        ["CouleurTexte"] = Color.FromArgb("#FBEAE6"),
        ["CouleurTexteDouce"] = Color.FromArgb("#D2ACA3"),
        ["CouleurBordure"] = Color.FromArgb("#4F2A22"),
        ["CouleurCarte"] = Color.FromArgb("#2E1B17"),
    };

    public static Dictionary<string, Color> Resoudre(Palette palette, bool sombre) => (palette, sombre) switch
    {
        (Palette.Miel, false) => MielClair,
        (Palette.Miel, true) => MielSombre,
        (Palette.Lagon, false) => LagonClair,
        (Palette.Lagon, true) => LagonSombre,
        (Palette.Corail, false) => CorailClair,
        (Palette.Corail, true) => CorailSombre,
        _ => MielClair
    };
}
