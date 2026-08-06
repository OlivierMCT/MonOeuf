using OeufFrais.Services;

namespace OeufFrais;

public partial class CodePage : ContentPage
{
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

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

    // ===== Lecture du code sur la coquille =====
    //
    // La coquille est une surface ingrate : courbe, mate, mouchetée, avec une
    // impression jet d'encre en petits caractères. On propose donc toujours le
    // résultat en saisie modifiable plutôt que de le valider directement.

    private async void OnScannerCoquille(object? sender, EventArgs e)
    {
        BtnScan.IsEnabled = false;
        RebinderTexte(BtnScan, "Common_LectureEnCours");

        try
        {
            var scan = await ScanService.ScannerAsync(
                texte => ShellCodeParser.TrouverCode(texte) is not null);

            if (!scan.Succes)
            {
                if (scan.Message is not null)
                    await DisplayAlertAsync(Loc.GetString("Common_ScanImpossible"), scan.Message, Loc.GetString("Common_Fermer"));
                return;
            }

            var code = ShellCodeParser.TrouverCode(scan.Texte);

            if (code is null)
            {
                Message(Loc.GetString("Code_ScanIllisible"));
                return;
            }

            TxtCode.Text = code;
            Message(Loc.GetString("Code_ScanRelire"));
            OnDecoder(this, EventArgs.Empty);
        }
        finally
        {
            RebinderTexte(BtnScan, "Code_BoutonScan");
            BtnScan.IsEnabled = true;
        }
    }

    private static void RebinderTexte(Button bouton, string cle)
        => bouton.SetBinding(Button.TextProperty, new Binding($"[{cle}]", source: Loc));

    private void Message(string texte)
    {
        LblScan.Text = texte;
        LblScan.IsVisible = true;
    }
}
