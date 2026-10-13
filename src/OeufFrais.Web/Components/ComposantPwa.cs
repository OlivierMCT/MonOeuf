using Microsoft.AspNetCore.Components;
using OeufFrais.Web.Services;

namespace OeufFrais.Web.Components;

/// <summary>Base des composants qui se redessinent quand l'état de l'app installable change.</summary>
public abstract class ComposantPwa : ComponentBase, IDisposable
{
    [Inject] protected EtatPwa Pwa { get; set; } = default!;

    protected override void OnInitialized() => Pwa.Change += Redessiner;

    private void Redessiner() => InvokeAsync(StateHasChanged);

    public void Dispose() => Pwa.Change -= Redessiner;
}
