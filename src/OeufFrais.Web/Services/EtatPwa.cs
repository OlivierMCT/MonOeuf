using Microsoft.JSInterop;

namespace OeufFrais.Web.Services;

public enum EtatHorsLigne { Preparation, Prete, Indisponible }

/// <summary>
/// État de l'application installable, tenu à jour par js/app.js : préchargement hors ligne,
/// nouvelle version en attente, installation possible.
/// </summary>
public sealed class EtatPwa
{
    private IJSInProcessRuntime? _js;

    public EtatHorsLigne HorsLigne { get; private set; } = EtatHorsLigne.Preparation;
    public bool MiseAJourDisponible { get; private set; }

    /// <summary>Android, Chrome, Edge : le navigateur propose sa propre invite d'installation.</summary>
    public bool Installable { get; private set; }

    /// <summary>iPhone ou iPad dans Safari, pas encore installée : l'installation passe par le menu Partager.</summary>
    public bool AideIos { get; private set; }

    /// <summary>L'utilisateur a masqué le bandeau d'installation.</summary>
    public bool BandeauMasque { get; private set; }

    public event Action? Change;

    public void Connecter(IJSInProcessRuntime js)
    {
        _js = js;
        js.InvokeVoid("oeufFrais.pwa.abonner", DotNetObjectReference.Create(this));
    }

    [JSInvokable]
    public void MettreAJourEtat(string horsLigne, bool miseAJour, bool installable, bool aideIos, bool bandeauMasque)
    {
        HorsLigne = Enum.TryParse<EtatHorsLigne>(horsLigne, ignoreCase: true, out var e) ? e : EtatHorsLigne.Indisponible;
        MiseAJourDisponible = miseAJour;
        Installable = installable;
        AideIos = aideIos;
        BandeauMasque = bandeauMasque;
        Change?.Invoke();
    }

    public void Installer() => _js?.InvokeVoid("oeufFrais.pwa.installer");

    public void MettreAJour() => _js?.InvokeVoid("oeufFrais.pwa.mettreAJour");

    public void MasquerBandeau() => _js?.InvokeVoid("oeufFrais.pwa.masquerBandeau");
}
