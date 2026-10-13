using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using OeufFrais.Services;
using OeufFrais.Web;
using OeufFrais.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IPreferences, PreferencesNavigateur>();
builder.Services.AddSingleton<ThemeWeb>();
builder.Services.AddSingleton<EtatPwa>();

var host = builder.Build();

// La langue doit être fixée avant RunAsync : c'est la culture courante à ce moment-là qui décide
// quelles assemblies satellites (les traductions) le runtime charge. Changer de langue passe donc
// par un rechargement de la page, qui fonctionne aussi hors ligne.
var loc = LocalizationResourceManager.Instance;
loc.Initialiser(host.Services.GetRequiredService<IPreferences>());

var js = (IJSInProcessRuntime)host.Services.GetRequiredService<IJSRuntime>();
js.InvokeVoid("oeufFrais.definirLangue", loc.Culture.TwoLetterISOLanguageName);

host.Services.GetRequiredService<EtatPwa>().Connecter(js);

await host.RunAsync();
