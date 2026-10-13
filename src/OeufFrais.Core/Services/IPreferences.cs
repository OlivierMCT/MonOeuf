namespace OeufFrais.Services;

/// <summary>
/// Stockage clé/valeur des réglages de l'utilisateur (langue, palette, thème). Chaque plateforme
/// fournit le sien : Preferences sous MAUI, localStorage dans le navigateur.
/// </summary>
public interface IPreferences
{
    string Get(string cle, string defaut);
    void Set(string cle, string valeur);
}
