using System.Linq;
using OeufFrais.Services;

namespace OeufFrais;

public partial class MainPage : ContentPage
{
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

    public MainPage()
    {
        InitializeComponent();
        MettreAJourChips();
        AppliquerMode();

        // LblAide n'est pas lié par {svc:Translate} (son contenu dépend de ModePonte) :
        // sans ça, il resterait figé dans l'ancienne langue après un changement en direct.
        Loc.PropertyChanged += (_, _) => AppliquerMode();
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
        var accent = (Color)appResources["CouleurAccent"];
        var texte = (Color)appResources["CouleurTexte"];
        var texteDouce = (Color)appResources["CouleurTexteDouce"];
        var carte = (Color)appResources["CouleurCarte"];
        var bordure = (Color)appResources["CouleurBordure"];

        ChipPonte.BackgroundColor = ModePonte ? accent : carte;
        ChipPonte.Stroke = ModePonte ? accent : bordure;
        LblChipPonte.TextColor = ModePonte ? texte : texteDouce;

        ChipDcr.BackgroundColor = !ModePonte ? accent : carte;
        ChipDcr.Stroke = !ModePonte ? accent : bordure;
        LblChipDcr.TextColor = !ModePonte ? texte : texteDouce;
    }

    private void AppliquerMode()
    {
        var today = DateTime.Today;

        if (ModePonte)
        {
            LblAide.Text = Loc.GetString("Main_Aide_Ponte");
            DpDate.MinimumDate = today.AddDays(-120);
            DpDate.MaximumDate = today;
            DpDate.Date = today;
        }
        else
        {
            LblAide.Text = Loc.GetString("Main_Aide_Dcr");
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

        // Reconstruit à chaque calcul : garantit un affichage dans la langue actuelle
        // même si elle a changé depuis la dernière fois que cette carte s'est affichée.
        BindableLayout.SetItemsSource(ListeTestVerre, EggCalculator.TestVerreDEau);
    }

    // ===== Lecture de la DCR sur la boîte =====

    private async void OnScannerBoite(object sender, EventArgs e)
    {
        BtnScan.IsEnabled = false;
        // Rebinder (plutôt que fixer Text en dur) : une simple affectation de Text
        // supprimerait le binding {svc:Translate} posé en XAML et casserait la
        // mise à jour en direct si la langue change ensuite.
        RebinderTexte(BtnScan, "Common_LectureEnCours");

        try
        {
            var scan = await ScanService.ScannerAsync(
                texte => DateOcrParser.Meilleur(texte) is not null);

            if (!scan.Succes)
            {
                if (scan.Message is not null)
                    await DisplayAlertAsync(Loc.GetString("Common_ScanImpossible"), scan.Message, Loc.GetString("Common_Fermer"));
                return;
            }

            var candidats = DateOcrParser.Extraire(scan.Texte);

            if (candidats.Count == 0)
            {
                Message(Loc.GetString("Main_ScanAucuneDate"));
                return;
            }

            var choisie = candidats[0].Date;

            // Plusieurs dates plausibles : la ponte et la DCR figurent parfois toutes
            // les deux sur la boîte, on laisse l'utilisateur trancher.
            if (candidats.Count > 1)
            {
                var options = candidats.Take(4).Select(c => c.Libelle).ToArray();
                var choix = await DisplayActionSheetAsync(
                    Loc.GetString("Main_ScanQuelleDcr"), Loc.GetString("Common_Annuler"), null, options);

                if (string.IsNullOrEmpty(choix) || choix == Loc.GetString("Common_Annuler")) return;

                choisie = candidats.First(c => c.Libelle == choix).Date;
            }

            // Une date lue sur la boîte est une DCR : on bascule dans ce mode.
            // L'ordre compte, ChangerMode réinitialise le sélecteur de date.
            ChangerMode(false);
            DpDate.Date = choisie;

            Message(Loc.GetString("Main_ScanDateLue", choisie.ToString("d MMMM yyyy", Loc.Culture)));
            OnCalculer(this, EventArgs.Empty);
        }
        finally
        {
            RebinderTexte(BtnScan, "Main_BoutonScan");
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
