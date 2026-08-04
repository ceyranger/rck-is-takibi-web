using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class ProjectCatalogIdentityHelperTests
{
    [Fact]
    public void ResolveEffectiveIdentity_Istinat_InheritsParentFields()
    {
        var parentId = Guid.NewGuid();
        var parent = new ProjectCatalogEntry
        {
            Id = parentId,
            DisplayName = "100-1 Fahrettin Gençgün",
            AdaParsel = "100-1",
            YapiSahibi = "Fahrettin Gençgün",
            YibfNo = "55",
            Muteahhit = "Fahrettin Gençgün",
            Belediye = "Sinop",
            Kind = ProjectCatalogKind.Normal
        };
        var istinat = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId
        };

        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(istinat, [parent, istinat]);

        Assert.Equal("100-1", identity.AdaParsel);
        Assert.Equal("Fahrettin Gençgün", identity.YapiSahibi);
        Assert.Equal("55", identity.YibfNo);
        Assert.Equal("Fahrettin Gençgün", identity.Muteahhit);
        Assert.Equal("Sinop", identity.Belediye);
    }

    [Fact]
    public void ResolveEffectiveIdentity_Normal_FillsMuteahhitFromYapiSahibi()
    {
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            AdaParsel = "10-2",
            YapiSahibi = "Ali Veli",
            Kind = ProjectCatalogKind.Normal
        };

        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project);

        Assert.Equal("Ali Veli", identity.Muteahhit);
    }

    [Fact]
    public void ResolveEffectiveIdentity_Special_DoesNotFillMuteahhitFromYapiSahibi()
    {
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            YapiSahibi = "Ali Veli",
            Kind = ProjectCatalogKind.Special
        };

        var identity = ProjectCatalogIdentityHelper.ResolveEffectiveIdentity(project);

        Assert.Equal(string.Empty, identity.Muteahhit);
    }

    [Fact]
    public void MatchesSearch_Istinat_MatchesParentOwnerName()
    {
        var parentId = Guid.NewGuid();
        var parent = new ProjectCatalogEntry
        {
            Id = parentId,
            DisplayName = "Ana İş",
            YapiSahibi = "Fahrettin Gençgün",
            Kind = ProjectCatalogKind.Normal,
            IsActive = true
        };
        var istinat = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId,
            IsActive = true
        };

        Assert.True(ProjectCatalogIdentityHelper.MatchesSearch(istinat, "Fahrettin", [parent, istinat]));
        Assert.Equal("İstinat · Fahrettin Gençgün", ProjectCatalogIdentityHelper.BuildPickerTitle(istinat, [parent, istinat]));
        Assert.Equal("İstinat", ProjectCatalogIdentityHelper.BuildPickerSubtitle(istinat, [parent, istinat]));
    }

    [Fact]
    public void GetPickerSortKey_OrdersNormalBeforeIstinat_ForSameOwner()
    {
        var parentId = Guid.NewGuid();
        var parent = new ProjectCatalogEntry
        {
            Id = parentId,
            DisplayName = "100-1 Fahrettin Gençgün",
            AdaParsel = "100-1",
            YapiSahibi = "Fahrettin Gençgün",
            Kind = ProjectCatalogKind.Normal,
            DisplayOrder = 5
        };
        var istinat = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId,
            DisplayOrder = 1
        };
        var catalog = new[] { parent, istinat };

        var ordered = catalog
            .OrderBy(item => ProjectCatalogIdentityHelper.GetPickerSortKey(item, catalog))
            .Select(item => item.Id)
            .ToList();

        Assert.Equal([parentId, istinat.Id], ordered);
    }
}
