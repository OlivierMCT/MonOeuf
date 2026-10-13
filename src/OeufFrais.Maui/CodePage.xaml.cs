using OeufFrais.Services;

namespace OeufFrais;

public partial class CodePage : ContentPage
{
    public CodePage()
    {
        InitializeComponent();
    }

    private void OnDecoder(object? sender, EventArgs e)
    {
        var resultat = ShellCodeParser.Parse(TxtCode.Text);

        if (resultat.Valide)
        {
            Resultat.BindingContext = resultat;
            Resultat.IsVisible = true;
            CarteErreur.IsVisible = false;
        }
        else
        {
            Resultat.IsVisible = false;
            LblErreur.Text = resultat.Erreur;
            CarteErreur.IsVisible = true;
        }
    }
}
