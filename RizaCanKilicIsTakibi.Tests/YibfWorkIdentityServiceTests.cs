using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class YibfWorkIdentityServiceTests
{
    [Fact]
    public void NormalizeIdentities_Links_Exact_IsTakibi_Row_To_AnaBilgi_Identity()
    {
        var anaBilgi = CreateAnaBilgiEntry("725-4", "CEMALETTİN ERSOY");
        var isTakibi = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = "725-4 CEMALETTİN ERSOY"
        };

        YibfWorkIdentityService.NormalizeIdentities([anaBilgi], [isTakibi]);

        Assert.Equal(anaBilgi.Id, anaBilgi.WorkGroupId);
        Assert.Equal(anaBilgi.Id, anaBilgi.WorkIdentityId);
        Assert.Equal(anaBilgi.WorkGroupId, isTakibi.WorkGroupId);
        Assert.Equal(anaBilgi.WorkIdentityId, isTakibi.WorkIdentityId);
        Assert.Equal(string.Empty, isTakibi.WorkVariantLabel);
    }

    [Theory]
    [InlineData("725-4 CEMALETTİN ERSOY İSTİNAT", "İSTİNAT")]
    [InlineData("725-4 CEMALETTİN ERSOY A BLOK", "A BLOK")]
    public void NormalizeIdentities_Keeps_Variants_In_Same_Group_With_Separate_Identity(string jobName, string expectedVariant)
    {
        var anaBilgi = CreateAnaBilgiEntry("725-4", "CEMALETTİN ERSOY");
        var isTakibi = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = jobName
        };

        YibfWorkIdentityService.NormalizeIdentities([anaBilgi], [isTakibi]);

        Assert.Equal(anaBilgi.WorkGroupId, isTakibi.WorkGroupId);
        Assert.Equal(isTakibi.Id, isTakibi.WorkIdentityId);
        Assert.NotEqual(anaBilgi.WorkIdentityId, isTakibi.WorkIdentityId);
        Assert.Equal(expectedVariant, isTakibi.WorkVariantLabel);
    }

    [Fact]
    public void Classify_Returns_Ambiguous_When_Base_Key_Is_Duplicated()
    {
        var first = CreateAnaBilgiEntry("725-4", "CEMALETTİN ERSOY");
        var second = CreateAnaBilgiEntry("725-4", "CEMALETTİN ERSOY");
        var isTakibi = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = "725-4 CEMALETTİN ERSOY"
        };

        var match = YibfWorkIdentityService.Classify(isTakibi, [first, second]);
        YibfWorkIdentityService.NormalizeIdentities([first, second], [isTakibi]);

        Assert.Equal(YibfWorkIdentityMatchKind.Ambiguous, match.Kind);
        Assert.Equal(isTakibi.Id, isTakibi.WorkGroupId);
        Assert.Equal(isTakibi.Id, isTakibi.WorkIdentityId);
    }

    [Fact]
    public void NormalizeWorkKey_Normalizes_Case_Whitespace_And_Separators()
    {
        var key = YibfWorkIdentityService.NormalizeWorkKey("  725 / 4   cemalettin   ersoy  ");

        Assert.Equal("725 4 CEMALETTİN ERSOY", key);
    }

    [Fact]
    public void NormalizeIdentities_Preserves_Unmatched_Catalog_Ids()
    {
        var parentId = Guid.NewGuid();
        var istinatId = Guid.NewGuid();
        var anaBilgi = CreateAnaBilgiEntry("200-2", "Sahip");
        anaBilgi.Id = parentId;
        anaBilgi.WorkGroupId = parentId;
        anaBilgi.WorkIdentityId = parentId;

        var isTakibi = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            WorkGroupId = parentId,
            WorkIdentityId = istinatId,
            JobName = "İstinat Duvarı",
            WorkVariantLabel = "İstinat"
        };

        YibfWorkIdentityService.NormalizeIdentities([anaBilgi], [isTakibi]);

        Assert.Equal(parentId, isTakibi.WorkGroupId);
        Assert.Equal(istinatId, isTakibi.WorkIdentityId);
        Assert.Equal("İstinat", isTakibi.WorkVariantLabel);
    }

    private static YibfAnaBilgiEntry CreateAnaBilgiEntry(string adaParsel, string yapiSahibi)
        => new()
        {
            Id = Guid.NewGuid(),
            AdaParsel = adaParsel,
            YapiSahibi = yapiSahibi
        };
}
