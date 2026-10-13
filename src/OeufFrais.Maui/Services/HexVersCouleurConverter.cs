using System.Globalization;

namespace OeufFrais.Services;

/// <summary>
/// Convertit une couleur hexadécimale du projet Core (« #2E9E4F ») en Color MAUI.
/// Le paramètre facultatif est une opacité entre 0 et 1, pour les fonds pâles (0.16).
/// </summary>
public sealed class HexVersCouleurConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string hex) return null;

        var couleur = Color.FromArgb(hex);
        return parameter is string alpha && float.TryParse(alpha, NumberStyles.Float, CultureInfo.InvariantCulture, out var a)
            ? couleur.WithAlpha(a)
            : couleur;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
