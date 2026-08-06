using Microsoft.Maui.Storage;
using OeufFrais.Resources.Styles;

namespace OeufFrais.Services;

public enum ModePreference { Systeme, Clair, Sombre }

/// <summary>
/// Applique la palette de couleurs et le mode clair/sombre choisis par l'utilisateur (ou le
/// système par défaut) en écrivant les 7 jetons sémantiques dans Application.Current.Resources.
/// Les vues qui les référencent via {DynamicResource ...} se rafraîchissent automatiquement.
/// </summary>
public sealed class ThemeService
{
    public static ThemeService Instance { get; } = new();

    private const string ClePalette = "theme_palette";
    private const string CleMode = "theme_mode";

    private Palette _palette = Palette.Miel;
    private ModePreference _mode = ModePreference.Systeme;
    private bool _abonne;

    private ThemeService() { }

    public Palette PaletteActuelle => _palette;
    public ModePreference ModeActuel => _mode;

    /// <summary>À appeler une fois, avant la construction de l'UI (App.xaml.cs).</summary>
    public void Initialiser()
    {
        _palette = Enum.TryParse<Palette>(Preferences.Get(ClePalette, nameof(Palette.Miel)), out var p) ? p : Palette.Miel;
        _mode = Enum.TryParse<ModePreference>(Preferences.Get(CleMode, nameof(ModePreference.Systeme)), out var m) ? m : ModePreference.Systeme;

        Appliquer();

        if (!_abonne)
        {
            Application.Current!.RequestedThemeChanged += (_, _) =>
            {
                if (_mode == ModePreference.Systeme) Appliquer();
            };
            _abonne = true;
        }
    }

    public void Definir(Palette palette, ModePreference mode)
    {
        _palette = palette;
        _mode = mode;
        Preferences.Set(ClePalette, palette.ToString());
        Preferences.Set(CleMode, mode.ToString());
        Appliquer();
    }

    private void Appliquer()
    {
        var app = Application.Current!;

        app.UserAppTheme = _mode switch
        {
            ModePreference.Clair => AppTheme.Light,
            ModePreference.Sombre => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };

        var sombre = _mode switch
        {
            ModePreference.Clair => false,
            ModePreference.Sombre => true,
            _ => app.RequestedTheme == AppTheme.Dark
        };

        foreach (var (cle, couleur) in Palettes.Resoudre(_palette, sombre))
            app.Resources[cle] = couleur;
    }
}
