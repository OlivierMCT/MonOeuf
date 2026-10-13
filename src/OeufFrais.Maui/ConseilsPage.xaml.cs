using OeufFrais.Services;

namespace OeufFrais;

public partial class ConseilsPage : ContentPage
{
    public ConseilsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Ces deux listes sont statiques (pas liées à un calcul) : on les recharge à chaque
        // apparition de l'onglet pour qu'elles reflètent la langue actuelle si elle a changé.
        BindableLayout.SetItemsSource(ListeRegles, EggCalculator.Conservation);
        BindableLayout.SetItemsSource(ListeTestVerre, EggCalculator.TestVerreDEau);
    }
}
