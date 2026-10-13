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
        //
        // La carte résultat (dates, titre, résumé) vient d'un EggAssessment sans
        // INotifyPropertyChanged, construit une seule fois au clic sur "Calculer" : elle ne se
        // rafraîchit pas non plus toute seule. On relance le calcul avec la même date pour
        // qu'elle réapparaisse dans la nouvelle langue sans attendre une nouvelle sélection.
        //
        // DpDate lui-même (le texte "jeudi 6 août 2026" affiché dans le sélecteur) est rendu par
        // le contrôle natif à partir de sa propriété Date : changer la culture seule ne déclenche
        // aucune notification côté binding, donc le texte reste dans l'ancienne langue tant que
        // l'utilisateur ne choisit pas une nouvelle date. Handler.UpdateValue ne suffisant pas à
        // faire re-formater le contrôle natif, on force un vrai changement de valeur (aller-retour
        // d'un jour), le seul déclencheur fiable du pipeline de mise à jour de la plateforme.
        Loc.PropertyChanged += (_, _) =>
        {
            AppliquerTexteAide();

            var dateActuelle = DpDate.Date;
            if (dateActuelle is { } valeur)
            {
                DpDate.Date = valeur == DpDate.MaximumDate ? valeur.AddDays(-1) : valeur.AddDays(1);
                DpDate.Date = valeur;
            }

            if (Resultats.IsVisible)
                OnCalculer(this, EventArgs.Empty);
        };
    }

    private bool ModePonte { get; set; } = true;

    private CancellationTokenSource? _ctsAnimationPoule;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ctsAnimationPoule?.Cancel();
        _ctsAnimationPoule = new CancellationTokenSource();
        _ = BouclerAnimationPouleAsync(_ctsAnimationPoule.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ctsAnimationPoule?.Cancel();
    }

    // ===== La poule traverse l'écran en se dandinant, en boucle, gauche-droite puis droite-gauche. =====

    private async Task BouclerAnimationPouleAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(350, ct);

            var versLaDroite = true;
            while (!ct.IsCancellationRequested)
            {
                await JouerTraverseeAsync(versLaDroite, ct);
                versLaDroite = !versLaDroite;
                await Task.Delay(500, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // La page a disparu (ou une nouvelle boucle démarre) : on arrête simplement.
        }
    }

    private async Task JouerTraverseeAsync(bool versLaDroite, CancellationToken ct)
    {
        // La piste n'est pas encore mise en page juste après l'ouverture de la page :
        // on patiente un peu pour connaître sa largeur réelle avant de calculer le trajet.
        for (var tentative = 0; tentative < 10 && PisteMarche.Width <= 0; tentative++)
            await Task.Delay(50, ct);

        if (PisteMarche.Width <= 0) return;

        var largeurPoule = LblPouleMarche.Width > 0 ? LblPouleMarche.Width : 40;
        var borneGauche = -largeurPoule;
        var borneDroite = PisteMarche.Width;
        var depart = versLaDroite ? borneGauche : borneDroite;
        var arrivee = versLaDroite ? borneDroite : borneGauche;

        LblPouleMarche.Rotation = 0;
        LblPouleMarche.TranslationY = 0;
        LblPouleMarche.ScaleX = versLaDroite ? -1 : 1; // regarde dans le sens de la marche
        LblPouleMarche.TranslationX = depart;

        // Petits pas tranquilles avec un dandinement (rebond + inclinaison alternés),
        // regroupés en tronçons : la poule s'arrête, regarde autour d'elle, repart.
        const int pas = 14;
        var tronçons = new[] { 4, 4, 3, 3 };

        var i = 0;
        foreach (var longueur in tronçons)
        {
            for (var j = 0; j < longueur; j++)
            {
                i++;
                var x = depart + (arrivee - depart) * i / (double)pas;
                var enLAir = i % 2 == 0;
                await Task.WhenAll(
                    LblPouleMarche.TranslateToAsync(x, enLAir ? -7 : 0, 320, Easing.SinInOut),
                    LblPouleMarche.RotateToAsync(enLAir ? 6 : -6, 320, Easing.SinInOut));
            }

            if (i >= pas) break;

            // Pause : elle se redresse et regarde de part et d'autre avant de repartir.
            await LblPouleMarche.RotateToAsync(0, 120, Easing.CubicOut);
            await Task.Delay(280, ct);
            await LblPouleMarche.RotateToAsync(-14, 160, Easing.SinInOut);
            await LblPouleMarche.RotateToAsync(14, 220, Easing.SinInOut);
            await LblPouleMarche.RotateToAsync(0, 140, Easing.SinInOut);
            await Task.Delay(200, ct);
        }

        await LblPouleMarche.RotateToAsync(0, 100, Easing.CubicOut);
    }

    private void OnChipPonte(object? sender, TappedEventArgs e) => ChangerMode(true);

    private void OnChipDcr(object? sender, TappedEventArgs e) => ChangerMode(false);

    private void ChangerMode(bool modePonte)
    {
        if (ModePonte == modePonte) return;
        ModePonte = modePonte;
        MettreAJourChips();
        AppliquerMode();
        Resultats.IsVisible = false;
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

    private void AppliquerTexteAide()
        => LblAide.Text = Loc.GetString(ModePonte ? "Main_Aide_Ponte" : "Main_Aide_Dcr");

    private void AppliquerMode()
    {
        var today = DateTime.Today;
        AppliquerTexteAide();

        if (ModePonte)
        {
            DpDate.MinimumDate = today.AddDays(-120);
            DpDate.MaximumDate = today;
            DpDate.Date = today;
        }
        else
        {
            DpDate.MinimumDate = today.AddDays(-120);
            DpDate.MaximumDate = today.AddDays(60);
            DpDate.Date = today.AddDays(14);
        }
    }

    private void OnCalculer(object? sender, EventArgs e)
    {
        var assessment = ModePonte
            ? EggCalculator.DepuisPonte(DpDate.Date!.Value)
            : EggCalculator.DepuisDcr(DpDate.Date!.Value);

        Resultats.BindingContext = assessment;
        Resultats.IsVisible = true;

        // Reconstruit à chaque calcul : garantit un affichage dans la langue actuelle
        // même si elle a changé depuis la dernière fois que cette carte s'est affichée.
        BindableLayout.SetItemsSource(ListeTestVerre, EggCalculator.TestVerreDEau);
    }
}
