using RizaCanKilicIsTakibi.Helpers;

namespace RizaCanKilicIsTakibi.Tests;

public class SearchContextAliasBuilderTests
{
    [Fact]
    public void BuildParcelAliasLookup_Merges_Parcel_Context_Without_Duplicates()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397"),
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397"),
            new SearchContextIdentitySeed("642-25", "YASİN ERGÜN", null, "999")
        ]);

        Assert.True(lookup.ParcelAliases.ContainsKey("787-5"));
        Assert.Contains("ALAADDİN BEYAZ", lookup.ParcelAliases["787-5"]);
        Assert.Contains("SEKVAN", lookup.ParcelAliases["787-5"]);
        Assert.Contains("1855397", lookup.ParcelAliases["787-5"]);
    }

    [Fact]
    public void EnrichSearchText_Appends_Aliases_When_Parcel_Token_Exists()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("787-5 evrak istenecek", lookup);

        Assert.Contains("787-5 evrak istenecek", enriched);
        Assert.Contains("ALAADDİN BEYAZ", enriched);
        Assert.Contains("SEKVAN", enriched);
        Assert.Contains("1855397", enriched);
    }

    [Fact]
    public void EnrichSearchText_Does_Not_Match_Partial_Parcel_Tokens()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("1787-5 evrak istenecek", lookup);

        Assert.DoesNotContain("ALAADDİN BEYAZ", enriched);
        Assert.DoesNotContain("SEKVAN", enriched);
        Assert.DoesNotContain("1855397", enriched);
    }

    [Fact]
    public void EnrichSearchText_Appends_Parcel_Context_When_Owner_Name_Exists()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("Alaaddin Beyaz evrak istenecek", lookup);

        Assert.Contains("Alaaddin Beyaz evrak istenecek", enriched);
        Assert.Contains("787-5", enriched);
        Assert.Contains("SEKVAN", enriched);
        Assert.Contains("1855397", enriched);
    }

    [Fact]
    public void EnrichSearchText_Appends_Parcel_Context_When_Unique_Name_Token_Exists()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("235-1", "ORSA ENERJİ", "SEKVAN", "111"),
            new SearchContextIdentitySeed("999-1", "BAŞKA ENERJİ", "DİĞER", "222")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("Orsa evrak istenecek", lookup);

        Assert.Contains("235-1", enriched);
        Assert.Contains("ORSA ENERJİ", enriched);
        Assert.Contains("111", enriched);
    }

    [Fact]
    public void EnrichSearchText_Does_Not_Append_Ambiguous_Name_Token_Context()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("235-1", "ORSA ENERJİ", "SEKVAN", "111"),
            new SearchContextIdentitySeed("999-1", "ORSA MADENCİLİK", "DİĞER", "222")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("Orsa evrak istenecek", lookup);

        Assert.DoesNotContain("235-1", enriched);
        Assert.DoesNotContain("999-1", enriched);
    }

    [Fact]
    public void EnrichSearchText_Appends_Parcel_Context_When_Yibf_No_Exists_As_Exact_Token()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("1855397 dosyasi islenecek", lookup);

        Assert.Contains("787-5", enriched);
        Assert.Contains("ALAADDİN BEYAZ", enriched);
    }

    [Fact]
    public void EnrichSearchText_Does_Not_Match_Partial_Yibf_No_Tokens()
    {
        var lookup = SearchContextAliasBuilder.BuildAliasLookup(
        [
            new SearchContextIdentitySeed("787-5", "ALAADDİN BEYAZ", "SEKVAN", "1855397")
        ]);

        var enriched = SearchContextAliasBuilder.EnrichSearchText("11855397 dosyasi islenecek", lookup);

        Assert.DoesNotContain("787-5", enriched);
        Assert.DoesNotContain("ALAADDİN BEYAZ", enriched);
    }
}
