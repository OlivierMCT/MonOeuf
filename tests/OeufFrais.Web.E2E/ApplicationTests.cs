using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace OeufFrais.Web.E2E;

[Collection(CollectionSite.Nom)]
public sealed class ApplicationTests(SiteFixture site) : IAsyncLifetime
{
    private IBrowserContext _contexte = default!;
    private IPage _page = default!;
    private readonly List<string> _erreurs = new();

    public async Task InitializeAsync()
    {
        _contexte = await site.NouveauTelephoneAsync();
        _page = await NouvellePageAsync(_contexte);
    }

    public Task DisposeAsync() => _contexte.CloseAsync();

    private async Task<IPage> NouvellePageAsync(IBrowserContext contexte)
    {
        var page = await contexte.NewPageAsync();
        page.PageError += (_, e) => _erreurs.Add(e);
        // Un lien profond reçoit 404.html avec le code 404, comme sur GitHub Pages : c'est voulu
        // pour la page elle-même, pas pour les fichiers qu'elle charge.
        page.Response += (_, r) =>
        {
            if (r.Status >= 400 && r.Request.ResourceType != "document") _erreurs.Add($"{r.Status} {r.Url}");
        };
        page.Console += (_, m) =>
        {
            if (m.Type == "error" && !m.Text.StartsWith("Failed to load resource")) _erreurs.Add(m.Text);
        };
        return page;
    }

    /// <summary>Ouvre une page de l'app et attend que Blazor l'ait affichée.</summary>
    private async Task OuvrirAsync(IPage page, string chemin = "")
    {
        await page.GotoAsync(site.Url + chemin);
        await Expect(page.Locator(".onglets")).ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    private static string Iso(DateTime date) => date.ToString("yyyy-MM-dd");

    private void AucuneErreur() => Assert.Empty(_erreurs);

    [Fact]
    public async Task Calcule_la_fraicheur_depuis_la_date_de_ponte()
    {
        await OuvrirAsync(_page);
        await Expect(_page.Locator("h1")).ToHaveTextAsync("🥚 Cet œuf est-il encore bon ?");

        await _page.Locator("input[type=date]").FillAsync(Iso(DateTime.Today.AddDays(-3)));
        await _page.GetByRole(AriaRole.Button, new() { Name = "Calculer" }).ClickAsync();

        await Expect(_page.Locator(".statut")).ToHaveTextAsync("Extra frais");
        await Expect(_page.Locator(".dates")).ToContainTextAsync("3 jours");
        await Expect(_page.Locator(".usages li")).ToHaveCountAsync(5);
        AucuneErreur();
    }

    [Fact]
    public async Task Calcule_la_fraicheur_depuis_la_DCR()
    {
        await OuvrirAsync(_page);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Date sur la boîte (DCR)" }).ClickAsync();
        await Expect(_page.GetByText("La DCR est la date")).ToBeVisibleAsync();

        // DCR dans 5 jours = pondu il y a 23 jours.
        await _page.Locator("input[type=date]").FillAsync(Iso(DateTime.Today.AddDays(5)));
        await _page.GetByRole(AriaRole.Button, new() { Name = "Calculer" }).ClickAsync();

        await Expect(_page.Locator(".statut")).ToHaveTextAsync("Frais");
        await Expect(_page.Locator(".dates")).ToContainTextAsync("23 jours");
        AucuneErreur();
    }

    [Fact]
    public async Task Une_date_hors_bornes_est_ramenee_dans_les_bornes()
    {
        await OuvrirAsync(_page);

        // En mode ponte, la date ne peut pas dépasser aujourd'hui.
        await _page.Locator("input[type=date]").FillAsync(Iso(DateTime.Today.AddDays(10)));

        await Expect(_page.Locator("input[type=date]")).ToHaveValueAsync(Iso(DateTime.Today));
    }

    [Fact]
    public async Task Le_test_du_verre_d_eau_apparait_apres_la_DCR()
    {
        await OuvrirAsync(_page);

        await _page.Locator("input[type=date]").FillAsync(Iso(DateTime.Today.AddDays(-35)));
        await _page.GetByRole(AriaRole.Button, new() { Name = "Calculer" }).ClickAsync();

        await Expect(_page.Locator(".statut")).ToHaveTextAsync("DCR dépassée");
        await Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "LE TEST DU VERRE D'EAU" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Decode_un_code_coquille_saisi_au_clavier()
    {
        await OuvrirAsync(_page);
        await _page.GetByRole(AriaRole.Link, new() { Name = "Code coquille" }).ClickAsync();

        await _page.GetByLabel("SAISIR LE CODE").FillAsync("0fr abc01");
        await _page.Keyboard.PressAsync("Enter");

        await Expect(_page.Locator(".code-normalise")).ToHaveTextAsync("0FRABC01");
        await Expect(_page.Locator(".dates")).ToContainTextAsync("Biologique");
        await Expect(_page.Locator(".dates")).ToContainTextAsync("France (FR)");

        await _page.GetByLabel("SAISIR LE CODE").FillAsync("9XYZ");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Décoder" }).ClickAsync();
        await Expect(_page.Locator(".erreur")).ToContainTextAsync("Code non reconnu");
        AucuneErreur();
    }

    [Fact]
    public async Task Le_champ_du_code_ne_corrige_pas_la_saisie()
    {
        await OuvrirAsync(_page, "code");

        var champ = _page.GetByLabel("SAISIR LE CODE");
        await Expect(champ).ToHaveAttributeAsync("autocapitalize", "characters");
        await Expect(champ).ToHaveAttributeAsync("autocorrect", "off");
        await Expect(champ).ToHaveAttributeAsync("spellcheck", "false");
    }

    [Fact]
    public async Task Affiche_les_conseils_de_conservation()
    {
        await OuvrirAsync(_page, "conserver");

        await Expect(_page.Locator("h1")).ToHaveTextAsync("🧊 Bien conserver ses œufs");
        await Expect(_page.Locator(".liste-puces").First.Locator("li")).ToHaveCountAsync(5);
    }

    [Fact]
    public async Task Un_lien_profond_fonctionne_des_la_premiere_visite()
    {
        // GitHub Pages répond 404.html (copie de index.html) : Blazor démarre et affiche la bonne page.
        await OuvrirAsync(_page, "reglages");

        await Expect(_page.Locator("h1")).ToHaveTextAsync("⚙️ Réglages");
    }

    [Fact]
    public async Task Le_choix_de_langue_est_applique_et_memorise()
    {
        await OuvrirAsync(_page, "reglages");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Deutsch" }).ClickAsync();

        await Expect(_page.Locator("h1")).ToHaveTextAsync("⚙️ Einstellungen", new() { Timeout = 30_000 });
        await Expect(_page.Locator("html")).ToHaveAttributeAsync("lang", "de");

        await OuvrirAsync(_page);
        await Expect(_page.Locator("h1")).ToHaveTextAsync("🥚 Ist dieses Ei noch gut?");
        await _page.Locator("input[type=date]").FillAsync(Iso(DateTime.Today.AddDays(-2)));
        await _page.Locator(".bouton-principal").ClickAsync();
        await Expect(_page.Locator(".statut")).ToHaveTextAsync("Extra frisch");
        AucuneErreur();
    }

    [Fact]
    public async Task Suit_la_langue_du_telephone_si_elle_est_traduite()
    {
        await using var contexte = await site.NouveauTelephoneAsync("es-ES");
        var page = await NouvellePageAsync(contexte);

        await OuvrirAsync(page);

        await Expect(page.Locator("h1")).ToHaveTextAsync("🥚 ¿Este huevo sigue siendo bueno?");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "es");
    }

    [Fact]
    public async Task Retombe_sur_le_francais_si_la_langue_du_telephone_n_est_pas_traduite()
    {
        await using var contexte = await site.NouveauTelephoneAsync("ja-JP");
        var page = await NouvellePageAsync(contexte);

        await OuvrirAsync(page);

        await Expect(page.Locator("h1")).ToHaveTextAsync("🥚 Cet œuf est-il encore bon ?");
        await Expect(page.Locator(".champ-date .texte")).ToContainTextAsync(DateTime.Today.Year.ToString());
        await Expect(page.Locator(".champ-date .texte")).Not.ToContainTextAsync("年");
    }

    [Fact]
    public async Task Le_theme_choisi_est_applique_et_memorise()
    {
        await OuvrirAsync(_page, "reglages");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Lagon" }).ClickAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sombre" }).ClickAsync();

        var html = _page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-palette", "lagon");
        await Expect(html).ToHaveAttributeAsync("data-theme", "sombre");
        await Expect(_page.Locator("meta[name=theme-color]")).ToHaveAttributeAsync("content", "#0F1F1C");

        await _page.ReloadAsync();
        await Expect(_page.Locator(".onglets")).ToBeVisibleAsync(new() { Timeout = 30_000 });
        await Expect(html).ToHaveAttributeAsync("data-theme", "sombre");
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Sombre" })).ToHaveAttributeAsync("aria-pressed", "true");
        AucuneErreur();
    }

    [Fact]
    public async Task Le_manifeste_et_ses_icones_sont_servis()
    {
        var reponse = await _contexte.APIRequest.GetAsync(site.Url + "manifest.webmanifest");
        Assert.True(reponse.Ok);

        var manifeste = await reponse.JsonAsync();
        Assert.Equal("Œuf Frais", manifeste!.Value.GetProperty("name").GetString());
        Assert.Equal("standalone", manifeste.Value.GetProperty("display").GetString());

        foreach (var icone in manifeste.Value.GetProperty("icons").EnumerateArray())
        {
            var image = await _contexte.APIRequest.GetAsync(site.Url + icone.GetProperty("src").GetString());
            Assert.True(image.Ok, icone.GetProperty("src").GetString());
        }
    }
}
