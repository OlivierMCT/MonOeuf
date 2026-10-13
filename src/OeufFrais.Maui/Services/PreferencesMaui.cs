using Microsoft.Maui.Storage;

namespace OeufFrais.Services;

/// <summary>Préférences de l'utilisateur stockées par MAUI (SharedPreferences, NSUserDefaults…).</summary>
public sealed class PreferencesMaui : IPreferences
{
    public string Get(string cle, string defaut) => Preferences.Get(cle, defaut);

    public void Set(string cle, string valeur) => Preferences.Set(cle, valeur);
}
