using RizaCanKilicIsTakibi.Helpers;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class DistrictCatalogTests
{
    [Fact]
    public void UnifiedCatalog_Includes_Action_And_Tadilat_Osb_Districts()
    {
        Assert.Contains("BOYABAT OSB", DistrictCatalog.All);
        Assert.Contains("SİNOP OSB", DistrictCatalog.All);
        Assert.Contains("MERKEZ", DistrictCatalog.All);
        Assert.Contains("SİNOP", DistrictCatalog.All);
    }

    [Theory]
    [InlineData("MERKEZ", "sinop")]
    [InlineData("SİNOP", "merkez")]
    public void Alias_Is_Used_For_Filtering(string storedDistrict, string query)
    {
        Assert.True(DistrictCatalog.ContainsForFilter(storedDistrict, query));
        Assert.True(DistrictCatalog.AreFilterAliases(storedDistrict, query));
    }

    [Fact]
    public void NormalizeStoredValue_Does_Not_Rename_Merkez()
    {
        Assert.Equal("MERKEZ", DistrictCatalog.NormalizeStoredValue("merkez"));
        Assert.Equal("SİNOP", DistrictCatalog.NormalizeStoredValue("sinop"));
    }
}
