using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace OeufFrais.Web.E2E;

/// <summary>
/// Le cœur de la migration : après une première visite en ligne, toute l'application doit
/// fonctionner sans réseau, y compris un lien profond et le changement de langue.
/// </summary>
[Collection(CollectionSite.Nom)]
public sealed class HorsLigneTests(SiteFixture site)
{
    private static async Task AttendreAppAsync(IPage page)
        => await Expect(page.Locator(".onglets")).ToBeVisibleAsync(new() { Timeout = 30_000 });

    [Fact]
    public async Task Toute_l_application_fonctionne_sans_reseau()
    {
        await using var contexte = await site.NouveauTelephoneAsync();
        var page = await contexte.NewPageAsync();
        var erreurs = new List<string>();
        page.PageError += (_, e) => erreurs.Add(e);

        // Première visite, en ligne : le service worker précharge tout.
        await page.GotoAsync(site.Url + "reglages");
        await AttendreAppAsync(page);
        await Expect(page.GetByText("Prête hors ligne")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await contexte.SetOfflineAsync(true);
        Assert.Equal("hors ligne", await page.EvaluateAsync<string>(
            "fetch('manifest.webmanifest?nocache=' + Date.now()).then(() => 'en ligne', () => 'hors ligne')"));

        // Rechargement et lien profond, sans réseau.
        await page.ReloadAsync();
        await AttendreAppAsync(page);
        await page.GotoAsync(site.Url + "code");
        await AttendreAppAsync(page);
        await Expect(page.Locator("h1")).ToHaveTextAsync("🔎 Le code sur la coquille");

        await page.GetByLabel("SAISIR LE CODE").FillAsync("1FRXY12");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator(".dates")).ToContainTextAsync("Plein air");

        await page.GetByRole(AriaRole.Link, new() { Name = "Fraîcheur" }).ClickAsync();
        await page.Locator("input[type=date]").FillAsync(DateTime.Today.AddDays(-15).ToString("yyyy-MM-dd"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Calculer" }).ClickAsync();
        await Expect(page.Locator(".statut")).ToHaveTextAsync("Frais");

        await page.GetByRole(AriaRole.Link, new() { Name = "Conserver" }).ClickAsync();
        await Expect(page.Locator(".liste-puces").First.Locator("li")).ToHaveCountAsync(5);

        // Changer de langue recharge la page : les traductions doivent venir du cache.
        await page.GetByRole(AriaRole.Link, new() { Name = "Réglages" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Italiano" }).ClickAsync();
        await Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "it", new() { Timeout = 30_000 });
        await AttendreAppAsync(page);
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Freschezza" })).ToBeVisibleAsync();

        Assert.Empty(erreurs);
    }

    [Fact]
    public async Task Une_nouvelle_version_est_proposee_puis_installee()
    {
        await using var contexte = await site.NouveauTelephoneAsync();
        var page = await contexte.NewPageAsync();

        await page.GotoAsync(site.Url + "reglages");
        await AttendreAppAsync(page);
        await Expect(page.GetByText("Prête hors ligne")).ToBeVisibleAsync(new() { Timeout = 60_000 });

        // Rechargée, la page est servie par le service worker : c'est le cas d'une app installée.
        await page.ReloadAsync();
        await AttendreAppAsync(page);
        Assert.True(await page.EvaluateAsync<bool>("navigator.serviceWorker.controller !== null"));

        // Publication d'une nouvelle version : le manifeste des fichiers change.
        var manifeste = Path.Combine(site.Dossier, "service-worker-assets.js");
        var original = await File.ReadAllTextAsync(manifeste);
        try
        {
            await File.WriteAllTextAsync(manifeste,
                System.Text.RegularExpressions.Regex.Replace(original, "\"version\": \"[^\"]+\"", "\"version\": \"nouvelle-version\""));

            // Ce que fait js/app.js au retour au premier plan.
            await page.EvaluateAsync("navigator.serviceWorker.getRegistration().then(r => r.update())");

            var bandeau = page.GetByText("Une nouvelle version est disponible.");
            await Expect(bandeau).ToBeVisibleAsync(new() { Timeout = 30_000 });

            await page.GetByRole(AriaRole.Button, new() { Name = "Mettre à jour" }).ClickAsync();
            await AttendreAppAsync(page);
            await Expect(bandeau).Not.ToBeVisibleAsync(new() { Timeout = 30_000 });
            Assert.Contains("nouvelle-version", await page.EvaluateAsync<string>("caches.keys().then(k => k.join(' '))"));
        }
        finally
        {
            await File.WriteAllTextAsync(manifeste, original);
        }
    }
}
