using System.Globalization;
using System.Reflection;

namespace OeufFrais.Web.Services;

/// <summary>
/// Version publiée, inscrite dans l'assembly à la construction (voir OeufFrais.Web.csproj) :
/// permet de vérifier sur un téléphone qu'il tourne bien sur la dernière mise en ligne.
/// </summary>
public static class VersionApp
{
    private static string? Metadonnee(string cle)
        => typeof(VersionApp).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == cle)?.Value;

    /// <summary>Identifiant court du commit publié, ou « dev » en développement.</summary>
    public static string Commit => Metadonnee("VersionCommit") ?? "dev";

    /// <summary>Date de construction, en UTC.</summary>
    public static DateTime? Date
        => DateTime.TryParse(Metadonnee("VersionDate"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
            ? date
            : null;

    /// <summary>« 14/10/2026 15:10 · 46287d4 », date à l'heure du téléphone.</summary>
    public static string Texte(CultureInfo culture)
        => Date is { } date ? $"{date.ToLocalTime().ToString("g", culture)} · {Commit}" : Commit;
}
