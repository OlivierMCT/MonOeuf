using Microsoft.Maui.Controls.Shapes;
using OeufFrais.Resources.Styles;
using OeufFrais.Services;

namespace OeufFrais;

public partial class SettingsPage : ContentPage
{
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

    // Noms natifs : volontairement non traduits, une langue s'appelle pareil dans toutes les langues.
    private static readonly (string? Code, string Nom)[] Langues =
    {
        (null, "__systeme__"),
        ("fr", "Français"),
        ("en", "English"),
        ("de", "Deutsch"),
        ("es", "Español"),
        ("it", "Italiano"),
        ("pt", "Português"),
        ("nl", "Nederlands"),
    };

    private static readonly (Palette Valeur, string CleNom)[] PalettesDisponibles =
    {
        (Palette.Miel, "Settings_PaletteMiel"),
        (Palette.Lagon, "Settings_PaletteLagon"),
        (Palette.Corail, "Settings_PaletteCorail"),
    };

    private static readonly (ModePreference Valeur, string CleNom)[] ThemesDisponibles =
    {
        (ModePreference.Systeme, "Settings_ThemeSysteme"),
        (ModePreference.Clair, "Settings_ThemeClair"),
        (ModePreference.Sombre, "Settings_ThemeSombre"),
    };

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Reconstruire();
    }

    private void Reconstruire()
    {
        ConstruireLangues();
        ConstruirePalettes();
        ConstruireThemes();
    }

    private void ConstruireLangues()
    {
        LangueContainer.Children.Clear();
        var res = Application.Current!.Resources;
        var selection = Loc.PreferenceExplicite;

        foreach (var (code, nom) in Langues)
        {
            var selectionne = code == selection;
            var libelle = code is null ? Loc.GetString("Settings_LangueSysteme") : nom;

            var border = new Border
            {
                Padding = new Thickness(14, 12),
                StrokeThickness = 1.5,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = selectionne
                    ? (Color)res["CouleurAccent"]
                    : (Color)res["CouleurCarte"],
                Stroke = selectionne
                    ? (Color)res["CouleurAccent"]
                    : (Color)res["CouleurBordure"],
            };

            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            grid.Add(new Label
            {
                Text = libelle,
                FontAttributes = selectionne ? FontAttributes.Bold : FontAttributes.None,
                TextColor = selectionne
                    ? (Color)res["CouleurTexte"]
                    : (Color)res["CouleurTexteDouce"],
                VerticalOptions = LayoutOptions.Center,
            }, 0, 0);

            if (selectionne)
            {
                var coche = new Label
                {
                    Text = "✓",
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color)res["CouleurTexte"],
                    VerticalOptions = LayoutOptions.Center,
                };
                grid.Add(coche, 1, 0);
            }

            border.Content = grid;
            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    Loc.Definir(code);
                    Reconstruire();
                })
            });

            LangueContainer.Children.Add(border);
        }
    }

    private void ConstruirePalettes()
    {
        PaletteContainer.Children.Clear();
        PaletteContainer.ColumnDefinitions.Clear();
        var res = Application.Current!.Resources;
        var actuelle = ThemeService.Instance.PaletteActuelle;

        for (var i = 0; i < PalettesDisponibles.Length; i++)
        {
            PaletteContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var (valeur, cleNom) = PalettesDisponibles[i];
            var selectionnee = valeur == actuelle;
            var accent = Palettes.Resoudre(valeur, sombre: false)["CouleurAccent"];

            var stack = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center };

            var pastille = new Border
            {
                WidthRequest = 40,
                HeightRequest = 40,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                BackgroundColor = accent,
                StrokeThickness = selectionnee ? 3 : 0,
                Stroke = (Color)res["CouleurTexte"],
            };

            var label = new Label
            {
                Text = Loc.GetString(cleNom),
                FontSize = 12,
                FontAttributes = selectionnee ? FontAttributes.Bold : FontAttributes.None,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = (Color)res["CouleurTexte"],
            };

            stack.Children.Add(pastille);
            stack.Children.Add(label);

            stack.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    ThemeService.Instance.Definir(valeur, ThemeService.Instance.ModeActuel);
                    Reconstruire();
                })
            });

            Grid.SetColumn(stack, i);
            PaletteContainer.Children.Add(stack);
        }
    }

    private void ConstruireThemes()
    {
        ThemeContainer.Children.Clear();
        ThemeContainer.ColumnDefinitions.Clear();
        var res = Application.Current!.Resources;
        var actuel = ThemeService.Instance.ModeActuel;

        for (var i = 0; i < ThemesDisponibles.Length; i++)
        {
            ThemeContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var (valeur, cleNom) = ThemesDisponibles[i];
            var selectionne = valeur == actuel;

            var border = new Border
            {
                Padding = new Thickness(8, 12),
                StrokeThickness = 1.5,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                BackgroundColor = selectionne
                    ? (Color)res["CouleurAccent"]
                    : (Color)res["CouleurCarte"],
                Stroke = selectionne
                    ? (Color)res["CouleurAccent"]
                    : (Color)res["CouleurBordure"],
                Content = new Label
                {
                    Text = Loc.GetString(cleNom),
                    FontSize = 14,
                    FontAttributes = selectionne ? FontAttributes.Bold : FontAttributes.None,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = selectionne
                        ? (Color)res["CouleurTexte"]
                        : (Color)res["CouleurTexteDouce"],
                }
            };

            border.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    ThemeService.Instance.Definir(ThemeService.Instance.PaletteActuelle, valeur);
                    Reconstruire();
                })
            });

            Grid.SetColumn(border, i);
            ThemeContainer.Children.Add(border);
        }
    }
}
