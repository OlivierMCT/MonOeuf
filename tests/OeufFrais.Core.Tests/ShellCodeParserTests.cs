using OeufFrais.Services;

namespace OeufFrais.Core.Tests;

public class ShellCodeParserTests : TestsAvecLangue
{
    [Theory]
    [InlineData("0FRABC01", "Bio")]
    [InlineData("1FRABC01", "Plein air")]
    [InlineData("2FRABC01", "Au sol")]
    [InlineData("3FRABC01", "Cage")]
    public void Le_premier_chiffre_donne_le_mode_d_elevage(string code, string debutMode)
    {
        var resultat = ShellCodeParser.Parse(code);

        Assert.True(resultat.Valide);
        Assert.StartsWith(debutMode, resultat.ModeElevage, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(resultat.ModeElevageDetail));
    }

    [Theory]
    [InlineData("3FRABC01")]
    [InlineData("3frabc01")]
    [InlineData("  3FR ABC01  ")]
    [InlineData("3-FR-ABC01")]
    [InlineData("3 fr abc 01")]
    public void La_saisie_est_normalisee(string saisie)
    {
        var resultat = ShellCodeParser.Parse(saisie);

        Assert.True(resultat.Valide);
        Assert.Equal("3FRABC01", resultat.CodeNormalise);
        Assert.Equal("France (FR)", resultat.Pays);
        Assert.Equal("ABC01", resultat.IdentifiantElevage);
        Assert.Contains("ABC01", resultat.IdentifiantElevageTexte);
    }

    [Theory]
    [InlineData("3FRAB")]
    [InlineData("3FRABCDEFGHIJKL")]
    public void Identifiant_de_2_a_12_caracteres(string saisie)
    {
        Assert.True(ShellCodeParser.Parse(saisie).Valide);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Saisie_vide(string? saisie)
    {
        var resultat = ShellCodeParser.Parse(saisie);

        Assert.False(resultat.Valide);
        Assert.Equal(LocalizationResourceManager.Instance.GetString("ShellCode_Erreur_Vide"), resultat.Erreur);
    }

    [Theory]
    [InlineData("4FRABC01")]      // pas de 4e mode d'élevage
    [InlineData("FRABC01")]       // chiffre manquant
    [InlineData("3F")]            // trop court
    [InlineData("3FRA")]          // identifiant d'un seul caractère
    [InlineData("3FRABCDEFGHIJKLM")] // identifiant de 13 caractères
    [InlineData("3F1ABC01")]      // le pays doit être en lettres
    [InlineData("3FRABC_01")]     // caractère interdit
    public void Format_invalide(string saisie)
    {
        var resultat = ShellCodeParser.Parse(saisie);

        Assert.False(resultat.Valide);
        Assert.Equal(LocalizationResourceManager.Instance.GetString("ShellCode_Erreur_Format"), resultat.Erreur);
    }

    [Theory]
    [InlineData("FR", "France (FR)")]
    [InlineData("BE", "Belgique (BE)")]
    [InlineData("DE", "Allemagne (DE)")]
    [InlineData("ZZ", "ZZ")]   // pays inconnu : on affiche le code ISO tel quel
    public void Nom_du_pays(string iso, string attendu)
    {
        Assert.Equal(attendu, ShellCodeParser.Parse($"1{iso}XY12").Pays);
    }

    [Fact]
    public void Nom_du_pays_traduit()
    {
        Langue("de");

        Assert.Equal("Frankreich (FR)", ShellCodeParser.Parse("1FRXY12").Pays);
    }
}
