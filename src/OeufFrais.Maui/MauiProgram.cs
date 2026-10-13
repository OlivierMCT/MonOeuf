using Microsoft.Extensions.Logging;

namespace OeufFrais;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Le CultureInfo ci-dessus ne pilote que le formatage .NET (DateTime.ToString).
        // Le calendrier natif qui s'ouvre au clic sur le DatePicker suit encore la langue
        // du téléphone : sur iOS/MacCatalyst et Windows on force donc aussi sa locale
        // pour qu'il reste cohérent avec le reste de l'écran (le cas Android est traité
        // dans MainActivity.AttachBaseContext, le contrôle natif n'exposant pas de Locale).
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("LocaleFrancaise", (handler, view) =>
        {
#if IOS || MACCATALYST
            // Le contrôle visible est un champ de texte ; le vrai UIDatePicker natif
            // n'apparaît qu'en InputView quand l'utilisateur le fait apparaître.
            if (handler.PlatformView.InputView is UIKit.UIDatePicker nativePicker)
                nativePicker.Locale = new Foundation.NSLocale("fr_FR");
#elif WINDOWS
            handler.PlatformView.Language = "fr-FR";
#endif
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
