using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class BitenExclusionTests
{
    [Fact]
    public void TumEksikler_ExcludesBitenTadilatEvenIfPassed()
    {
        var workGroupId = Guid.NewGuid();
        var anaBilgi = new List<YibfAnaBilgiEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorkGroupId = workGroupId,
                WorkIdentityId = workGroupId,
                AdaParsel = "10-1",
                YapiSahibi = "Ana"
            }
        };
        var tadilat = new List<TadilatEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SubTab = TadilatSubTab.Biten,
                JobName = "10-1 Ana",
                DigitalReceived = string.Empty
            }
        };

        var vm = new TumEksiklerViewModel();
        vm.RefreshFrom(anaBilgi, [], [], [], tadilat, [], [], []);

        Assert.DoesNotContain(vm.Groups.SelectMany(group => group.Items), item => item.SourceModule == "Tadilat");
    }

    [Fact]
    public void TumEksikler_ExcludesPositiveKarot()
    {
        var anaBilgi = new List<YibfAnaBilgiEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorkGroupId = Guid.NewGuid(),
                AdaParsel = "10-1",
                YapiSahibi = "Ana"
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AdaParsel = "10-1",
                Status = KarotStatus.KarotAlindiOlumlu
            }
        };

        var vm = new TumEksiklerViewModel();
        vm.RefreshFrom(anaBilgi, [], [], [], [], [], [], karot);

        Assert.DoesNotContain(vm.Groups.SelectMany(group => group.Items), item => item.SourceModule == "Karot");
    }

    [Fact]
    public void DryRun_SkipsBitenTadilatAndPositiveKarot()
    {
        var projectId = Guid.NewGuid();
        var catalog = new List<ProjectCatalogEntry>
        {
            new()
            {
                Id = projectId,
                DisplayName = "10-1 Sahip",
                AdaParsel = "10-1",
                YapiSahibi = "Sahip",
                YibfNo = "777",
                IsActive = true
            }
        };
        var tadilat = new List<TadilatEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SubTab = TadilatSubTab.Biten,
                JobName = "10-1 Sahip"
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Status = KarotStatus.KarotAlindiOlumlu,
                AdaParsel = "10-1",
                YapiSahibi = "Sahip",
                YibfNo = "777"
            }
        };

        var service = new ProjectLinkingService(new ProjectCatalogService(new NoOpCatalogRepository()));
        var result = service.DryRun(catalog, karot, tadilat, [], [], [], []);

        Assert.Equal(0, result.AutoLinkCount);
        Assert.Empty(result.Unresolved);
        Assert.True(result.SkippedAlreadyLinkedCount >= 2);
    }

    private sealed class NoOpCatalogRepository : IProjectCatalogRepository
    {
        public Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectCatalogEntry>>([]);

        public Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
