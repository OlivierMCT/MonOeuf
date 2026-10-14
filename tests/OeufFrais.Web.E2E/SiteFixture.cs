using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace OeufFrais.Web.E2E;

/// <summary>
/// Sert le site publié comme GitHub Pages le ferait, et lance Chromium.
///
/// Le dossier publié vient de la variable OEUFFRAIS_SITE (par défaut artifacts/site à la racine
/// du dépôt, là où le workflow le dépose). Le sous-dossier est lu dans la balise &lt;base&gt; de
/// index.html : « /MonOeuf/ » pour un site préparé pour GitHub Pages.
/// Comme GitHub Pages, une adresse inconnue reçoit 404.html avec le code 404.
/// </summary>
public sealed class SiteFixture : IAsyncLifetime
{
    private WebApplication? _serveur;
    private IPlaywright? _playwright;

    public IBrowser Navigateur { get; private set; } = default!;

    /// <summary>Adresse complète de l'app, terminée par « / ».</summary>
    public string Url { get; private set; } = "";

    /// <summary>Copie de travail du site servi : un test peut y simuler la publication d'une nouvelle version.</summary>
    public string Dossier { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var source = Path.GetFullPath(Environment.GetEnvironmentVariable("OEUFFRAIS_SITE")
            ?? Path.Combine(RacineDuDepot(), "artifacts", "site"));
        if (!File.Exists(Path.Combine(source, "index.html")))
            throw new InvalidOperationException(
                $"Site publié introuvable dans {source}. Lancez d'abord scripts/publier-site.sh /MonOeuf/ artifacts/site " +
                "(ou définissez OEUFFRAIS_SITE).");

        var dossier = Dossier = Path.Combine(Path.GetTempPath(), "oeuffrais-e2e-" + Guid.NewGuid().ToString("N"));
        CopierDossier(source, dossier);

        var basePath = Regex.Match(await File.ReadAllTextAsync(Path.Combine(dossier, "index.html")), "<base href=\"([^\"]+)\"").Groups[1].Value;
        if (basePath == "") basePath = "/";

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _serveur = builder.Build();

        var types = new FileExtensionContentTypeProvider();
        types.Mappings[".wasm"] = "application/wasm";
        types.Mappings[".dat"] = "application/octet-stream";
        types.Mappings[".blat"] = "application/octet-stream";
        types.Mappings[".webmanifest"] = "application/manifest+json";
        types.Mappings[".woff2"] = "font/woff2";

        _serveur.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(dossier),
            RequestPath = basePath.TrimEnd('/'),
        });
        _serveur.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(dossier),
            RequestPath = basePath.TrimEnd('/'),
            ContentTypeProvider = types,
            ServeUnknownFileTypes = true,
        });
        _serveur.Run(async contexte =>
        {
            var page404 = Path.Combine(dossier, "404.html");
            contexte.Response.StatusCode = StatusCodes.Status404NotFound;
            if (File.Exists(page404))
            {
                contexte.Response.ContentType = "text/html; charset=utf-8";
                await contexte.Response.SendFileAsync(page404);
            }
        });

        await _serveur.StartAsync();
        var adresse = _serveur.Urls.First();
        Url = adresse.TrimEnd('/') + basePath;

        _playwright = await Playwright.CreateAsync();
        Navigateur = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            // En local, on peut réutiliser un Chromium déjà installé ; en CI, celui de Playwright.
            ExecutablePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH"),
        });
    }

    public async Task DisposeAsync()
    {
        if (Navigateur is not null) await Navigateur.CloseAsync();
        _playwright?.Dispose();
        if (_serveur is not null) await _serveur.DisposeAsync();
        if (Directory.Exists(Dossier)) Directory.Delete(Dossier, recursive: true);
    }

    private static void CopierDossier(string source, string destination)
    {
        foreach (var fichier in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var cible = Path.Combine(destination, Path.GetRelativePath(source, fichier));
            Directory.CreateDirectory(Path.GetDirectoryName(cible)!);
            File.Copy(fichier, cible);
        }
    }

    /// <summary>Un téléphone : écran étroit, langue et fuseau choisis.</summary>
    public Task<IBrowserContext> NouveauTelephoneAsync(string langue = "fr-FR")
        => Navigateur.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            Locale = langue,
            TimezoneId = "Europe/Paris",
            IsMobile = true,
            HasTouch = true,
        });

    private static string RacineDuDepot()
    {
        var dossier = new DirectoryInfo(AppContext.BaseDirectory);
        while (dossier is not null && !File.Exists(Path.Combine(dossier.FullName, "OeufFrais.slnx")))
            dossier = dossier.Parent;
        return dossier?.FullName ?? Directory.GetCurrentDirectory();
    }
}

[CollectionDefinition(Nom)]
public sealed class CollectionSite : ICollectionFixture<SiteFixture>
{
    public const string Nom = "Site publié";
}
