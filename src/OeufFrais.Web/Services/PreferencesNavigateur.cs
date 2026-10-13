using Microsoft.JSInterop;
using OeufFrais.Services;

namespace OeufFrais.Web.Services;

/// <summary>
/// Préférences stockées dans le localStorage du navigateur. Appels synchrones : sous
/// WebAssembly, .NET et JavaScript partagent le même thread.
/// </summary>
public sealed class PreferencesNavigateur(IJSRuntime js) : IPreferences
{
    private readonly IJSInProcessRuntime _js = (IJSInProcessRuntime)js;

    public string Get(string cle, string defaut) => _js.Invoke<string?>("oeufFrais.lire", cle) ?? defaut;

    public void Set(string cle, string valeur) => _js.InvokeVoid("oeufFrais.ecrire", cle, valeur);
}
