using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using OeufFrais.Services;

namespace OeufFrais.Core.Tests;

public class TraductionsTests : TestsAvecLangue
{
    private static readonly ResourceManager Ressources =
        new("OeufFrais.Resources.Strings.AppStrings", typeof(LocalizationResourceManager).Assembly);

    private static Dictionary<string, string> Chaines(CultureInfo culture)
        => Ressources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string)e.Value!);

    private static readonly Dictionary<string, string> Neutre = Chaines(CultureInfo.InvariantCulture);

    public static TheoryData<string> AutresLangues()
        => new(LocalizationResourceManager.LanguesSupportees.Where(l => l != "fr"));

    private static string[] Arguments(string texte)
        => Regex.Matches(texte, @"\{(\d+)[^}]*\}").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

    [Fact]
    public void Le_francais_est_complet()
    {
        Assert.Equal(130, Neutre.Count);
        Assert.All(Neutre, e => Assert.False(string.IsNullOrWhiteSpace(e.Value), e.Key));
    }

    [Theory]
    [MemberData(nameof(AutresLangues))]
    public void Chaque_langue_traduit_toutes_les_cles(string langue)
    {
        var traduction = Chaines(new CultureInfo(langue));

        Assert.Empty(Neutre.Keys.Except(traduction.Keys));
        Assert.Empty(traduction.Keys.Except(Neutre.Keys));
        Assert.All(traduction, e => Assert.False(string.IsNullOrWhiteSpace(e.Value), e.Key));
    }

    [Theory]
    [MemberData(nameof(AutresLangues))]
    public void Chaque_traduction_garde_les_memes_arguments(string langue)
    {
        var traduction = Chaines(new CultureInfo(langue));

        Assert.All(Neutre, e => Assert.Equal(Arguments(e.Value), Arguments(traduction[e.Key])));
    }

    [Fact]
    public void Plus_aucun_texte_lie_aux_photos()
    {
        Assert.DoesNotContain(Neutre.Keys, k => k.StartsWith("Scan_") || k.Contains("Scan"));
    }
}

public class LocalizationResourceManagerTests : TestsAvecLangue
{
    private static LocalizationResourceManager Loc => LocalizationResourceManager.Instance;

    [Fact]
    public void Le_choix_de_langue_est_memorise()
    {
        var preferences = new PreferencesMemoire();
        Loc.Initialiser(preferences);

        Loc.Definir("it");
        Assert.Equal("it", preferences.Valeurs["app_language"]);
        Assert.Equal("it", Loc.Culture.Name);
        Assert.Equal("it", CultureInfo.CurrentCulture.Name);

        Loc.Definir(null);
        Assert.Equal("system", preferences.Valeurs["app_language"]);
        Assert.Null(Loc.PreferenceExplicite);
    }

    [Fact]
    public void La_langue_memorisee_est_reprise_au_demarrage()
    {
        var preferences = new PreferencesMemoire();
        preferences.Set("app_language", "nl");

        Loc.Initialiser(preferences);

        Assert.Equal("nl", Loc.PreferenceExplicite);
        Assert.Equal("nl", Loc.Culture.Name);
    }

    [Fact]
    public void Une_langue_inconnue_memorisee_revient_au_systeme()
    {
        var preferences = new PreferencesMemoire();
        preferences.Set("app_language", "xx");

        Loc.Initialiser(preferences);

        Assert.Null(Loc.PreferenceExplicite);
    }

    [Fact]
    public void Revenir_sur_Systeme_abandonne_la_langue_choisie()
    {
        Loc.Definir("de");
        Loc.Definir(null);

        Assert.Equal(LocalizationResourceManager.CultureDuSysteme().Name, Loc.Culture.Name);
    }

    [Fact]
    public void Le_systeme_est_suivi_seulement_s_il_est_traduit()
    {
        var systeme = LocalizationResourceManager.CultureSysteme;
        var attendu = LocalizationResourceManager.LanguesSupportees.Contains(systeme.TwoLetterISOLanguageName)
            ? systeme.Name
            : "fr";

        Assert.Equal(attendu, LocalizationResourceManager.CultureDuSysteme().Name);
    }

    [Fact]
    public void Cle_inconnue_renvoyee_telle_quelle()
    {
        Assert.Equal("Cle_Qui_N_Existe_Pas", Loc.GetString("Cle_Qui_N_Existe_Pas"));
    }

    [Theory]
    [InlineData(0, "0 jour")]
    [InlineData(1, "1 jour")]
    [InlineData(2, "2 jours")]
    public void Pluriel(int n, string attendu)
    {
        Assert.Equal(attendu, Loc.Plural("Common_Jour_Un", "Common_Jour_Plusieurs", n));
    }
}
