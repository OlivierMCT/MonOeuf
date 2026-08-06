using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

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
}
