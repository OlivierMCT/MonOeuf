using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using Java.Util;

namespace OeufFrais;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Désactive le mode edge-to-edge : sans ça, la TabBar est dessinée
        // derrière la barre de navigation système et son texte se retrouve
        // trop bas / partiellement masqué.
        if (Window is not null)
        {
            WindowCompat.SetDecorFitsSystemWindows(Window, true);
        }
    }

    // Les dates sont forcées en français partout dans l'app (voir MauiProgram), mais le
    // CultureInfo .NET ne pilote pas les dialogues natifs Android (ex. le calendrier du
    // DatePicker), qui restent sinon dans la langue du téléphone. On force donc aussi la
    // configuration de l'Activity, dont hérite tout composant natif ouvert par la suite.
    protected override void AttachBaseContext(Context? baseContext)
    {
        if (baseContext?.Resources?.Configuration is { } configuration)
        {
            var configurationFr = new Android.Content.Res.Configuration(configuration);
            configurationFr.SetLocale(new Locale("fr", "FR"));
            baseContext = baseContext.CreateConfigurationContext(configurationFr);
        }

        base.AttachBaseContext(baseContext);
    }
}
