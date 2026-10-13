using System.Globalization;
using OeufFrais.Services;

namespace OeufFrais.Web;

/// <summary>Raccourcis importés dans tous les composants (_Imports.razor).</summary>
public static class Textes
{
    public static string T(string cle) => LocalizationResourceManager.Instance.GetString(cle);

    public static string T(string cle, params object[] args) => LocalizationResourceManager.Instance.GetString(cle, args);

    /// <summary>Culture de la langue choisie dans l'app, pour formater les dates.</summary>
    public static CultureInfo CultureApp => LocalizationResourceManager.Instance.Culture;

    /// <summary>Valeur d'un attribut ARIA booléen (aria-pressed…), qui attend "true" ou "false".</summary>
    public static string Aria(bool valeur) => valeur ? "true" : "false";

    /// <summary>Couleur hexadécimale à 16 % d'opacité, pour les fonds pâles des pastilles.</summary>
    public static string Pale(string hex) => hex + "29";
}
