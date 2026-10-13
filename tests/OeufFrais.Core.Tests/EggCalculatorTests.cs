using OeufFrais.Services;

namespace OeufFrais.Core.Tests;

public class EggCalculatorTests : TestsAvecLangue
{
    private static readonly DateTime Aujourdhui = new(2026, 8, 6);

    private static EggAssessment AgeDe(int jours) => EggCalculator.DepuisPonte(Aujourdhui.AddDays(-jours), Aujourdhui);

    [Theory]
    [InlineData(-1, Freshness.Future)]
    [InlineData(0, Freshness.ExtraFrais)]
    [InlineData(9, Freshness.ExtraFrais)]
    [InlineData(10, Freshness.Frais)]
    [InlineData(28, Freshness.Frais)]
    [InlineData(29, Freshness.ApresDcr)]
    [InlineData(42, Freshness.ApresDcr)]
    [InlineData(43, Freshness.Douteux)]
    [InlineData(120, Freshness.Douteux)]
    public void Statut_suit_les_seuils_reglementaires(int age, Freshness attendu)
    {
        var resultat = AgeDe(age);

        Assert.Equal(attendu, resultat.Statut);
        Assert.Equal(age, resultat.AgeJours);
    }

    [Fact]
    public void Dates_cles_calculees_depuis_la_ponte()
    {
        var ponte = new DateTime(2026, 8, 1);

        var resultat = EggCalculator.DepuisPonte(ponte, Aujourdhui);

        Assert.Equal(ponte, resultat.DatePonte);
        Assert.Equal(new DateTime(2026, 8, 10), resultat.FinExtraFrais);
        Assert.Equal(new DateTime(2026, 8, 22), resultat.LimiteVente);
        Assert.Equal(new DateTime(2026, 8, 29), resultat.Dcr);
        Assert.Equal(new DateTime(2026, 9, 12), resultat.LimitePrudence);
        Assert.Equal(23, resultat.JoursAvantDcr);
    }

    [Fact]
    public void La_DCR_vaut_ponte_plus_28_jours()
    {
        var dcr = new DateTime(2026, 8, 20);

        var resultat = EggCalculator.DepuisDcr(dcr, Aujourdhui);

        Assert.Equal(new DateTime(2026, 7, 23), resultat.DatePonte);
        Assert.Equal(dcr, resultat.Dcr);
        Assert.Equal(14, resultat.JoursAvantDcr);
    }

    [Fact]
    public void Les_heures_de_la_date_saisie_sont_ignorees()
    {
        var resultat = EggCalculator.DepuisPonte(new DateTime(2026, 8, 1, 23, 59, 0), Aujourdhui.AddHours(1));

        Assert.Equal(5, resultat.AgeJours);
    }

    [Theory]
    [InlineData(2026, 3, 27, 2026, 3, 30, 3)]    // passage à l'heure d'été (29 mars 2026)
    [InlineData(2026, 10, 23, 2026, 10, 26, 3)]  // passage à l'heure d'hiver (25 octobre 2026)
    public void Un_changement_d_heure_ne_decale_pas_l_age(int ap, int mp, int jp, int aa, int ma, int ja, int attendu)
    {
        var resultat = EggCalculator.DepuisPonte(new DateTime(ap, mp, jp), new DateTime(aa, ma, ja));

        Assert.Equal(attendu, resultat.AgeJours);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 5, 5)]
    [InlineData(15, 5, 3)]
    [InlineData(35, 4, 2)]
    [InlineData(50, 2, 0)]
    public void Usages_proposes_selon_le_statut(int age, int nombre, int autorises)
    {
        var conseils = AgeDe(age).Conseils;

        Assert.Equal(nombre, conseils.Count);
        Assert.Equal(autorises, conseils.Count(c => c.Autorise));
        Assert.All(conseils, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Usage));
            Assert.False(string.IsNullOrWhiteSpace(c.Detail));
        });
    }

    [Fact]
    public void Extra_frais_autorise_la_consommation_crue()
    {
        Assert.All(AgeDe(3).Conseils, c => Assert.True(c.Autorise));
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(28, false)]
    [InlineData(29, true)]
    [InlineData(60, true)]
    public void Test_du_verre_d_eau_recommande_apres_la_DCR(int age, bool attendu)
    {
        Assert.Equal(attendu, AgeDe(age).TestVerreDEauRecommande);
    }

    [Theory]
    [InlineData(-1, "#8A8A8A")]
    [InlineData(0, "#2E9E4F")]
    [InlineData(15, "#E8A31C")]
    [InlineData(35, "#E2711D")]
    [InlineData(50, "#C0392B")]
    public void Couleur_du_statut(int age, string attendu)
    {
        Assert.Equal(attendu, AgeDe(age).CouleurHex);
    }

    [Fact]
    public void Couleur_d_un_usage()
    {
        Assert.Equal("#2E9E4F", new UsageConseil("u", true, "d").CouleurHex);
        Assert.Equal("#C0392B", new UsageConseil("u", false, "d").CouleurHex);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(28)]
    [InlineData(35)]
    [InlineData(50)]
    public void Titre_et_resume_sont_traduits(int age)
    {
        var resultat = AgeDe(age);

        Assert.DoesNotContain("Egg_", resultat.Titre);
        Assert.DoesNotContain("Egg_", resultat.Resume);
        Assert.DoesNotContain("{", resultat.Resume);
    }

    [Fact]
    public void Textes_en_francais()
    {
        var resultat = EggCalculator.DepuisPonte(new DateTime(2026, 8, 1), Aujourdhui);

        Assert.Equal("samedi 1 août 2026", resultat.PonteTexte);
        Assert.Equal("29 août 2026", resultat.DcrTexte);
        Assert.Equal("5 jours", resultat.AgeTexte);
        Assert.Equal("1 jour", AgeDe(1).AgeTexte);
        Assert.Equal("—", AgeDe(-3).AgeTexte);
    }

    [Fact]
    public void Textes_suivent_la_langue_choisie()
    {
        Langue("en");

        var resultat = EggCalculator.DepuisPonte(new DateTime(2026, 8, 1), Aujourdhui);

        Assert.Equal("Saturday 1 August 2026", resultat.PonteTexte);
    }

    [Fact]
    public void Listes_statiques_completes()
    {
        Assert.Equal(5, EggCalculator.Conservation.Length);
        Assert.Equal(4, EggCalculator.TestVerreDEau.Length);
        Assert.All(EggCalculator.Conservation.Concat(EggCalculator.TestVerreDEau),
            t => Assert.DoesNotContain("Egg_", t));
    }
}
