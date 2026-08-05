using System.Globalization;
using Microsoft.Extensions.Logging;
using Plugin.Maui.OCR;

namespace OeufFrais;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Les dates sont affichées en français quelle que soit la langue du téléphone.
        var fr = new CultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = fr;
        CultureInfo.DefaultThreadCurrentUICulture = fr;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseOcr();   // Vision (iOS) / ML Kit (Android) / Windows.Media.Ocr

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
