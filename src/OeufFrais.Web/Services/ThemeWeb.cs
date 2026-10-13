using Microsoft.JSInterop;
using OeufFrais.Services;

namespace OeufFrais.Web.Services;

public enum Palette { Miel, Lagon, Corail }

public enum ModePreference { Systeme, Clair, Sombre }

/// <summary>
/// Palette et mode clair/sombre choisis par l'utilisateur. Les couleurs elles-mêmes vivent dans
/// css/theme.css ; ce service mémorise le choix et pose les attributs data-palette / data-theme
/// sur &lt;html&gt; (js/app.js les applique aussi avant le démarrage de .NET, sans flash).
/// Les clés et valeurs stockées sont celles de l'app MAUI.
/// </summary>
public sealed class ThemeWeb(IPreferences preferences, IJSRuntime js)
{
    private const string ClePalette = "theme_palette";
    private const string CleMode = "theme_mode";

    private readonly IJSInProcessRuntime _js = (IJSInProcessRuntime)js;

    public Palette PaletteActuelle =>
        Enum.TryParse<Palette>(preferences.Get(ClePalette, nameof(Palette.Miel)), out var p) ? p : Palette.Miel;

    public ModePreference ModeActuel =>
        Enum.TryParse<ModePreference>(preferences.Get(CleMode, nameof(ModePreference.Systeme)), out var m) ? m : ModePreference.Systeme;

    public void Definir(Palette palette, ModePreference mode)
    {
        preferences.Set(ClePalette, palette.ToString());
        preferences.Set(CleMode, mode.ToString());
        _js.InvokeVoid("oeufFrais.appliquerTheme", palette.ToString(), mode.ToString());
    }

    /// <summary>Couleur d'accent claire de chaque palette, pour les pastilles des Réglages.</summary>
    public static string Accent(Palette palette) => palette switch
    {
        Palette.Lagon => "#21B8A6",
        Palette.Corail => "#FF6F59",
        _ => "#FFC738"
    };
}
