using OeufFrais.Services;

namespace OeufFrais;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Doit tourner avant la construction de AppShell : les bindings {Translate} et
        // {DynamicResource} du premier rendu doivent déjà voir la bonne langue et le bon thème.
        LocalizationResourceManager.Instance.Initialiser();
        ThemeService.Instance.Initialiser();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell()) { Title = LocalizationResourceManager.Instance.GetString("Shell_Title") };
}
