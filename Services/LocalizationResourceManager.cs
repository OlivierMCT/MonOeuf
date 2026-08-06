using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Microsoft.Maui.Storage;

namespace OeufFrais.Services;

/// <summary>
/// Point d'accès unique aux chaînes traduites (Resources/Strings/AppStrings*.resx).
/// Les vues XAML s'y lient via l'indexeur (voir TranslateExtension) : changer la culture
/// déclenche PropertyChanged("Item[]"), ce qui rafraîchit tous les bindings existants
/// sans reconstruire les pages.
/// </summary>
public sealed class LocalizationResourceManager : INotifyPropertyChanged
{
    public static LocalizationResourceManager Instance { get; } = new();

    private readonly ResourceManager _resourceManager =
        new("OeufFrais.Resources.Strings.AppStrings", typeof(LocalizationResourceManager).Assembly);

    private const string ClePreference = "app_language";

    /// <summary>Codes ISO à deux lettres des langues traduites dans cette version (vague 1).</summary>
    public static readonly string[] LanguesSupportees = { "fr", "en", "de", "es", "it", "pt", "nl" };

    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private string? _preferenceExplicite;

    private LocalizationResourceManager() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture => _culture;

    /// <summary>Code de langue choisi explicitement par l'utilisateur, ou null si "Système".</summary>
    public string? PreferenceExplicite => _preferenceExplicite;

    /// <summary>À appeler une fois, avant la construction de l'UI (App.xaml.cs).</summary>
    public void Initialiser()
    {
        var pref = Preferences.Get(ClePreference, "system");
        _preferenceExplicite = pref == "system" ? null : pref;
        AppliquerCultureCourante();
    }

    /// <param name="code">Un code parmi LanguesSupportees, ou null pour suivre le système.</param>
    public void Definir(string? code)
    {
        _preferenceExplicite = code;
        Preferences.Set(ClePreference, code ?? "system");
        AppliquerCultureCourante();
    }

    private void AppliquerCultureCourante()
    {
        var culture = _preferenceExplicite is not null
            ? new CultureInfo(_preferenceExplicite)
            : CultureInfo.CurrentUICulture;

        // Pour que le formatage des dates (mois, jours de semaine) suive la même langue,
        // même si l'utilisateur bascule sans redémarrer l'application. Doit être fait AVANT
        // SetCulture : son PropertyChanged force certains contrôles natifs (DatePicker) à se
        // reformater immédiatement, et ils lisent CultureInfo.CurrentCulture au passage.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        SetCulture(culture);
    }

    public void SetCulture(CultureInfo culture)
    {
        _culture = culture;
        // string.Empty (plutôt que la convention WPF "Item[]") : c'est la valeur que le moteur
        // de binding .NET MAUI reconnaît pour « toutes les propriétés ont changé », et donc pour
        // rafraîchir en direct tous les bindings d'indexeur {loc:Translate} déjà posés en XAML.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
        => _resourceManager.GetString(key, _culture) ?? key;

    public string GetString(string key, params object[] args)
        => string.Format(_culture, GetString(key), args);

    /// <summary>
    /// Règle simplifiée singulier/pluriel (n &lt;= 1 → singulier, sinon pluriel) : suffisante pour
    /// les langues de cette version (fr/en/de/es/it/pt/nl suivent toutes cette règle binaire).
    /// Les langues aux formes plurielles multiples (polonais, tchèque…) devront revoir cette
    /// méthode le jour où elles seront ajoutées.
    /// </summary>
    public string Plural(string keySingulier, string keyPluriel, int n)
        => GetString(n <= 1 ? keySingulier : keyPluriel, n);
}
