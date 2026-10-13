using OeufFrais.Services;

// LocalizationResourceManager est un singleton qui modifie la culture de tout le processus :
// les tests ne doivent pas tourner en parallèle.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OeufFrais.Core.Tests;

/// <summary>Préférences en mémoire, pour les tests.</summary>
public sealed class PreferencesMemoire : IPreferences
{
    public Dictionary<string, string> Valeurs { get; } = new();

    public string Get(string cle, string defaut) => Valeurs.TryGetValue(cle, out var v) ? v : defaut;

    public void Set(string cle, string valeur) => Valeurs[cle] = valeur;
}

/// <summary>Chaque test démarre en français, langue des .resx neutres.</summary>
public abstract class TestsAvecLangue
{
    protected TestsAvecLangue()
    {
        LocalizationResourceManager.Instance.Initialiser(new PreferencesMemoire());
        Langue("fr");
    }

    protected static void Langue(string code) => LocalizationResourceManager.Instance.Definir(code);
}
