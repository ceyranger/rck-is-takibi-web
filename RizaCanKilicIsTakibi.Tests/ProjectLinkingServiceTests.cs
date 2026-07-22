using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class ProjectLinkingServiceTests
{
    [Fact]
    public void DryRun_AutoLinks_WhenYibfNoMatchesUniquely()
    {
        var projectId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "100-1 Sahip",
                AdaParsel = "100-1",
                YapiSahibi = "Sahip",
                YibfNo = "555",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdaParsel = "farklı",
                YapiSahibi = "farklı",
                YibfNo = "555"
            }
        };

        var service = CreateService();
        var result = service.DryRun(catalog, karot, [], [], [], [], []);

        Assert.Equal(1, result.AutoLinkCount);
        Assert.Empty(result.Unresolved);
        Assert.Equal(projectId, result.AutoActions[0].ProjectId);
    }

    [Fact]
    public void Apply_WritesProjectId_Only()
    {
        var projectId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = entryId,
                AdaParsel = "Korunan",
                YapiSahibi = "Metin"
            }
        };
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "X",
                AdaParsel = "1-1",
                IsActive = true
            }
        };

        var service = CreateService();
        service.Apply(
            [new AutoProjectLinkAction
            {
                Module = ProjectLinkSourceModule.Karot,
                EntryId = entryId,
                ProjectId = projectId
            }],
            [],
            karot,
            [],
            [],
            [],
            [],
            [],
            catalog);

        Assert.Equal(projectId, karot[0].ProjectId);
        Assert.Equal("Korunan", karot[0].AdaParsel);
        Assert.Equal("Metin", karot[0].YapiSahibi);
    }

    [Fact]
    public void Apply_YibfIsTakibi_SetsIdsEvenWhenAlreadyNonEmpty()
    {
        var projectId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var yibf = new List<YibfIsTakibiEntry>
        {
            new()
            {
                Id = entryId,
                WorkGroupId = entryId,
                WorkIdentityId = entryId,
                JobName = "100-1 Sahip"
            }
        };
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "100-1 Sahip",
                AdaParsel = "100-1",
                YapiSahibi = "Sahip",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };

        var service = CreateService();
        service.Apply(
            [new AutoProjectLinkAction
            {
                Module = ProjectLinkSourceModule.YibfIsTakibi,
                EntryId = entryId,
                ProjectId = projectId
            }],
            [],
            [],
            [],
            [],
            [],
            [],
            yibf,
            catalog);

        Assert.Equal(projectId, yibf[0].WorkGroupId);
        Assert.Equal(projectId, yibf[0].WorkIdentityId);
    }

    [Fact]
    public void DryRun_AutoLinks_WhenAdaParselSpacingDiffers()
    {
        var projectId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "100-1 Sahip",
                AdaParsel = "100 - 1",
                YapiSahibi = "Sahip",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdaParsel = "100-1",
                YapiSahibi = "Sahip"
            }
        };

        var result = CreateService().DryRun(catalog, karot, [], [], [], [], []);

        Assert.Equal(1, result.AutoLinkCount);
        Assert.Empty(result.Unresolved);
        Assert.Equal(projectId, result.AutoActions[0].ProjectId);
    }

    [Fact]
    public void DryRun_AutoLinks_Normal_WhenNormalAndIstinatBothMatch()
    {
        var normalId = Guid.NewGuid();
        var istinatId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = normalId,
                DisplayName = "100-1 Sahip",
                AdaParsel = "100-1",
                YapiSahibi = "Ali Veli",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            },
            new()
            {
                Id = istinatId,
                DisplayName = "İstinat",
                AdaParsel = "100-1",
                YapiSahibi = "Ali Veli",
                Kind = ProjectCatalogKind.Istinat,
                ParentProjectId = normalId,
                IsActive = true
            }
        };
        var tadilat = new List<TadilatEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                JobName = "100-1 Ali Veli",
                SubTab = TadilatSubTab.Aktif
            }
        };

        var result = CreateService().DryRun(catalog, [], tadilat, [], [], [], []);

        Assert.Equal(1, result.AutoLinkCount);
        Assert.Empty(result.Unresolved);
        Assert.Equal(normalId, result.AutoActions[0].ProjectId);
    }

    [Fact]
    public void DryRun_AutoLinks_WhenOwnerNameIsPartialMatch()
    {
        var projectId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "200-2",
                AdaParsel = "200/2",
                YapiSahibi = "Ahmet Yılmaz İnşaat",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };
        var missing = new List<MissingProjectEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdaParsel = "200-2",
                YapiSahibi = "Ahmet Yılmaz"
            }
        };

        var result = CreateService().DryRun(catalog, [], [], [], missing, [], []);

        Assert.Equal(1, result.AutoLinkCount);
        Assert.Empty(result.Unresolved);
        Assert.Equal(projectId, result.AutoActions[0].ProjectId);
    }

    [Fact]
    public void DryRun_AutoLinks_WhenAdaExact_AndOwnerFirstWordsMatch()
    {
        var projectId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "725-4 Cemalettin Ersoy",
                AdaParsel = "725-4",
                YapiSahibi = "Cemalettin Ersoy İnşaat Ltd",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                DisplayName = "725-4 Başka",
                AdaParsel = "725-4",
                YapiSahibi = "Başka Sahip",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdaParsel = "725 / 4",
                YapiSahibi = "Cemalettin Ersoy"
            }
        };

        var result = CreateService().DryRun(catalog, karot, [], [], [], [], []);

        Assert.Equal(1, result.AutoLinkCount);
        Assert.Empty(result.Unresolved);
        Assert.Equal(projectId, result.AutoActions[0].ProjectId);
    }

    [Fact]
    public void DryRun_LeavesUnresolved_WhenAdaMatches_ButOwnerFirstWordsDiffer()
    {
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                DisplayName = "10-1",
                AdaParsel = "10-1",
                YapiSahibi = "Ali Veli",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdaParsel = "10-1",
                YapiSahibi = "Mehmet Yılmaz"
            }
        };

        var result = CreateService().DryRun(catalog, karot, [], [], [], [], []);

        Assert.Equal(0, result.AutoLinkCount);
        Assert.Single(result.Unresolved);
    }

    [Fact]
    public void DryRun_Action_UsesOwnerParcelText_NotWorkText_ForOwner()
    {
        var projectId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "100-1 Sahip",
                AdaParsel = "100-1",
                YapiSahibi = "Sahip",
                Kind = ProjectCatalogKind.Normal,
                IsActive = true
            }
        };
        var action = new List<ActionEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OwnerParcelText = "100 - 1 Sahip",
                WorkText = "Ruhsat takip dosyası"
            }
        };

        var service = CreateService();
        var result = service.DryRun(catalog, [], [], action, [], [], []);

        Assert.Equal(1, result.AutoLinkCount);
        Assert.Equal(projectId, result.AutoActions[0].ProjectId);
    }

    private static ProjectLinkingService CreateService()
        => new(new ProjectCatalogService(new NoOpCatalogRepository()));

    private sealed class NoOpCatalogRepository : IProjectCatalogRepository
    {
        public Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectCatalogEntry>>([]);

        public Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
