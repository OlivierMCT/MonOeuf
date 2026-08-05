using OeufFrais.Services;

namespace OeufFrais;

public partial class CodePage : ContentPage
{
    public CodePage()
    {
        InitializeComponent();
    }

    private void OnDecoder(object sender, EventArgs e)
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

    private async void OnScannerCoquille(object sender, EventArgs e)
    {
        var libelle = BtnScan.Text;
        BtnScan.IsEnabled = false;
        BtnScan.Text = "Lecture en cours…";

        try
        {
            var scan = await ScanService.ScannerAsync(
                texte => ShellCodeParser.TrouverCode(texte) is not null);

            if (!scan.Succes)
            {
                if (scan.Message is not null)
                    await DisplayAlertAsync("Scan impossible", scan.Message, "Fermer");
                return;
            }

            var code = ShellCodeParser.TrouverCode(scan.Texte);

            if (code is null)
            {
                Message("Code illisible. Approchez-vous, orientez l'œuf pour que le code soit bien à plat face à l'objectif et évitez les ombres portées.");
                return;
            }

            TxtCode.Text = code;
            Message("Relisez le code avant de valider : sur une coquille, les confusions 0/O, 1/I et 5/S sont fréquentes.");
            OnDecoder(this, EventArgs.Empty);
        }
        finally
        {
            BtnScan.Text = libelle;
            BtnScan.IsEnabled = true;
        }
    }

    private void Message(string texte)
    {
        LblScan.Text = texte;
        LblScan.IsVisible = true;
    }
}
