using System.Linq;
using OeufFrais.Services;

namespace OeufFrais;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        MettreAJourChips();
        AppliquerMode();
    }

    private bool ModePonte { get; set; } = true;

    private void OnChipPonte(object sender, TappedEventArgs e) => ChangerMode(true);

    private void OnChipDcr(object sender, TappedEventArgs e) => ChangerMode(false);

    private void ChangerMode(bool modePonte)
    {
        if (ModePonte == modePonte) return;
        ModePonte = modePonte;
        MettreAJourChips();
        AppliquerMode();
        Resultats.IsVisible = false;
        LblScan.IsVisible = false;
    }

    private void MettreAJourChips()
    {
        var appResources = Application.Current!.Resources;
        var jaune = (Color)appResources["Jaune"];
        var encre = (Color)appResources["Encre"];
        var encreDouce = (Color)appResources["EncreDouce"];
        var blanc = (Color)appResources["Blanc"];
        var bordure = (Color)appResources["Bordure"];

        ChipPonte.BackgroundColor = ModePonte ? jaune : blanc;
        ChipPonte.Stroke = ModePonte ? jaune : bordure;
        LblChipPonte.TextColor = ModePonte ? encre : encreDouce;

        ChipDcr.BackgroundColor = !ModePonte ? jaune : blanc;
        ChipDcr.Stroke = !ModePonte ? jaune : bordure;
        LblChipDcr.TextColor = !ModePonte ? encre : encreDouce;
    }

    private void AppliquerMode()
    {
        var today = DateTime.Today;

        if (ModePonte)
        {
            LblAide.Text = "La date de ponte figure parfois sur la boîte. Sinon, retirez 28 jours à la DCR.";
            DpDate.MinimumDate = today.AddDays(-120);
            DpDate.MaximumDate = today;
            DpDate.Date = today;
        }
        else
        {
            LblAide.Text = "La DCR est la date « à consommer de préférence avant » imprimée sur la boîte. Elle vaut ponte + 28 jours.";
            DpDate.MinimumDate = today.AddDays(-120);
            DpDate.MaximumDate = today.AddDays(60);
            DpDate.Date = today.AddDays(14);
        }
    }

    private void OnCalculer(object sender, EventArgs e)
    {
        var assessment = ModePonte
            ? EggCalculator.DepuisPonte(DpDate.Date.Value)
            : EggCalculator.DepuisDcr(DpDate.Date.Value);

        Resultats.BindingContext = assessment;
        Resultats.IsVisible = true;
    }

    // ===== Lecture de la DCR sur la boîte =====

    private async void OnScannerBoite(object sender, EventArgs e)
    {
        var libelle = BtnScan.Text;
        BtnScan.IsEnabled = false;
        BtnScan.Text = "Lecture en cours…";

        try
        {
            var scan = await ScanService.ScannerAsync(
                texte => DateOcrParser.Meilleur(texte) is not null);

            if (!scan.Succes)
            {
                if (scan.Message is not null)
                    await DisplayAlertAsync("Scan impossible", scan.Message, "Fermer");
                return;
            }

            var candidats = DateOcrParser.Extraire(scan.Texte);

            if (candidats.Count == 0)
            {
                Message("Aucune date lisible sur la photo. Cadrez la mention « à consommer de préférence avant le », à plat et bien éclairée — ou saisissez la date à la main.");
                return;
            }

            var choisie = candidats[0].Date;

            // Plusieurs dates plausibles : la ponte et la DCR figurent parfois toutes
            // les deux sur la boîte, on laisse l'utilisateur trancher.
            if (candidats.Count > 1)
            {
                var options = candidats.Take(4).Select(c => c.Libelle).ToArray();
                var choix = await DisplayActionSheetAsync(
                    "Quelle date est la DCR ?", "Annuler", null, options);

                if (string.IsNullOrEmpty(choix) || choix == "Annuler") return;

                choisie = candidats.First(c => c.Libelle == choix).Date;
            }

            // Une date lue sur la boîte est une DCR : on bascule dans ce mode.
            // L'ordre compte, ChangerMode réinitialise le sélecteur de date.
            ChangerMode(false);
            DpDate.Date = choisie;

            Message($"Date lue : {choisie:d MMMM yyyy}. Vérifiez-la avant de calculer.");
            OnCalculer(this, EventArgs.Empty);
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
