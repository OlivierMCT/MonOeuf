using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Plugin.Maui.OCR;

namespace OeufFrais.Services;

/// <param name="Succes">Faux si la photo n'a pas pu être prise ou lue.</param>
/// <param name="Texte">Texte brut reconnu, à passer aux parseurs.</param>
/// <param name="Message">Message à afficher, ou null si l'utilisateur a simplement annulé.</param>
public sealed record ScanResultat(bool Succes, string Texte, string? Message)
{
    public static ScanResultat Annule => new(false, "", null);
    public static ScanResultat Echec(string message) => new(false, "", message);
}

/// <summary>
/// Prise de photo puis reconnaissance de texte, via l'appareil photo du téléphone.
/// S'appuie sur Vision (iOS), ML Kit (Android) et Windows.Media.Ocr, sans moteur externe.
/// </summary>
public static class ScanService
{
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

    private static bool _initialise;
    private static readonly SemaphoreSlim Verrou = new(1, 1);

    /// <param name="estConcluant">
    /// Permet au demandeur de dire si la première passe suffit. Sinon une seconde passe
    /// est tentée en mode « accurate ».
    /// </param>
    public static async Task<ScanResultat> ScannerAsync(
        Func<string, bool> estConcluant, CancellationToken ct = default)
    {
        FileResult? photo;
        try
        {
            // Sur téléphone on déclenche l'appareil photo ; sur un poste sans webcam
            // (le cas classique en développement sous Windows) on ouvre une image
            // existante, ce qui permet de tester toute la chaîne de reconnaissance.
            photo = MediaPicker.Default.IsCaptureSupported
                ? await MediaPicker.Default.CapturePhotoAsync()
                : await MediaPicker.Default.PickPhotoAsync();
        }
        catch (PermissionException)
        {
            return ScanResultat.Echec(Loc.GetString("Scan_PermissionRefusee"));
        }
        catch (FeatureNotSupportedException)
        {
            return ScanResultat.Echec(Loc.GetString("Scan_NonSupporte"));
        }
        catch (Exception ex)
        {
            return ScanResultat.Echec(Loc.GetString("Scan_PhotoEchec", ex.Message));
        }

        if (photo is null) return ScanResultat.Annule;   // l'utilisateur a fermé l'appareil photo

        byte[] image;
        try
        {
            using var source = await photo.OpenReadAsync();
            using var memoire = new MemoryStream();
            await source.CopyToAsync(memoire, ct);
            image = memoire.ToArray();
        }
        catch (Exception ex)
        {
            return ScanResultat.Echec(Loc.GetString("Scan_LectureEchec", ex.Message));
        }

        try
        {
            await InitialiserAsync(ct);

            // Première passe sans « tryHard ». Sur iOS et macOS, cette option active
            // aussi la correction linguistique de Vision, qui transforme volontiers
            // un code ou une date en mot du dictionnaire. On ne l'emploie qu'en secours.
            var rapide = await OcrPlugin.Default.RecognizeTextAsync(image, tryHard: false, ct);
            var texte = rapide.Success ? rapide.AllText ?? "" : "";

            if (estConcluant(texte))
                return new ScanResultat(true, texte, null);

            var precis = await OcrPlugin.Default.RecognizeTextAsync(image, tryHard: true, ct);
            var texte2 = precis.Success ? precis.AllText ?? "" : "";

            if (estConcluant(texte2))
                return new ScanResultat(true, texte2, null);

            // Rien de concluant : on renvoie les deux passes, le parseur fera au mieux
            // et l'écran affichera son message d'échec.
            return new ScanResultat(true, $"{texte}\n{texte2}", null);
        }
        catch (Exception ex)
        {
            return ScanResultat.Echec(Loc.GetString("Scan_OcrEchec", ex.Message));
        }
    }

    private static async Task InitialiserAsync(CancellationToken ct)
    {
        if (_initialise) return;

        await Verrou.WaitAsync(ct);
        try
        {
            if (_initialise) return;
            await OcrPlugin.Default.InitAsync(ct);
            _initialise = true;
        }
        finally
        {
            Verrou.Release();
        }
    }
}
