using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class ProjectCatalogServiceTests
{
    [Fact]
    public void BuildSeedFromAnaBilgi_UsesWorkGroupId_AndDoesNotDuplicate()
    {
        var workGroupId = Guid.NewGuid();
        var anaBilgi = new List<YibfAnaBilgiEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorkGroupId = workGroupId,
                WorkIdentityId = workGroupId,
                AdaParsel = "100-1",
                YapiSahibi = "Test Sahip",
                YibfNo = "123"
            },
            new()
            {
                Id = Guid.NewGuid(),
                WorkGroupId = workGroupId,
                WorkIdentityId = workGroupId,
                AdaParsel = "100-1",
                YapiSahibi = "Test Sahip",
                YibfNo = "123"
            }
        };

        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var seeded = service.BuildSeedFromAnaBilgi(anaBilgi);

        Assert.Single(seeded);
        Assert.Equal(workGroupId, seeded[0].Id);
        Assert.Equal(ProjectCatalogKind.Normal, seeded[0].Kind);
        Assert.Contains("100-1", seeded[0].DisplayName, StringComparison.Ordinal);
        Assert.Equal("100-1", anaBilgi[0].AdaParsel);
    }

    [Fact]
    public void BuildFanOut_Normal_CreatesAnaBilgiAndIsTakibi()
    {
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var entry = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "200-2 Sahip",
            AdaParsel = "200-2",
            YapiSahibi = "Sahip",
            YibfNo = "999",
            Belediye = "Sinop Belediyesi",
            Muteahhit = "ABC İnşaat",
            Kind = ProjectCatalogKind.Normal
        };

        var fanOut = service.BuildFanOut(entry);

        Assert.NotNull(fanOut.AnaBilgiStub);
        Assert.NotNull(fanOut.IsTakibiStub);
        Assert.Equal(entry.Id, fanOut.AnaBilgiStub!.WorkGroupId);
        Assert.Equal(entry.Id, fanOut.IsTakibiStub!.WorkGroupId);
        Assert.Equal("200-2", fanOut.AnaBilgiStub.AdaParsel);
        Assert.Equal("Sinop Belediyesi", fanOut.AnaBilgiStub.Idare);
        Assert.Equal("ABC İnşaat", fanOut.AnaBilgiStub.Muteahhit);
    }

    [Fact]
    public void BuildSeedFromAnaBilgi_CopiesBelediyeAndMuteahhit()
    {
        var workGroupId = Guid.NewGuid();
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var seeded = service.BuildSeedFromAnaBilgi(
        [
            new YibfAnaBilgiEntry
            {
                Id = Guid.NewGuid(),
                WorkGroupId = workGroupId,
                WorkIdentityId = workGroupId,
                AdaParsel = "10-1",
                YapiSahibi = "Sahip",
                Idare = "Boyabat Belediyesi",
                Muteahhit = "XYZ Müteahhit"
            }
        ]);

        Assert.Single(seeded);
        Assert.Equal("Boyabat Belediyesi", seeded[0].Belediye);
        Assert.Equal("XYZ Müteahhit", seeded[0].Muteahhit);
    }

    [Fact]
    public void BuildFanOut_Istinat_OnlyIsTakibi()
    {
        var parentId = Guid.NewGuid();
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var entry = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            AdaParsel = "200-2",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId
        };

        var fanOut = service.BuildFanOut(entry);

        Assert.Null(fanOut.AnaBilgiStub);
        Assert.NotNull(fanOut.IsTakibiStub);
        Assert.Equal(parentId, fanOut.IsTakibiStub!.WorkGroupId);
    }

    [Fact]
    public void ApplyProjectSelection_DoesNotOverwriteFilledFields()
    {
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var karot = new KarotEntry
        {
            AdaParsel = "Dolu",
            YapiSahibi = string.Empty,
            YibfNo = string.Empty
        };
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            AdaParsel = "Yeni",
            YapiSahibi = "Sahip",
            YibfNo = "1"
        };

        service.ApplyProjectSelection(karot, project);

        Assert.Equal(project.Id, karot.ProjectId);
        Assert.Equal("Dolu", karot.AdaParsel);
        Assert.Equal("Sahip", karot.YapiSahibi);
        Assert.Equal("1", karot.YibfNo);
    }

    [Fact]
    public void ApplyProjectSelection_DoesNotPutDisplayNameIntoAdaParsel()
    {
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var karot = new KarotEntry();
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "Ali Veli",
            AdaParsel = string.Empty,
            YapiSahibi = "Ali Veli"
        };

        service.ApplyProjectSelection(karot, project);

        Assert.Equal(string.Empty, karot.AdaParsel);
        Assert.Equal("Ali Veli", karot.YapiSahibi);
    }

    [Fact]
    public void ApplyProjectSelection_Action_DoesNotFillWorkTextFromDisplayName()
    {
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var action = new ActionEntry();
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "100-1 Sahip",
            AdaParsel = "100-1",
            YapiSahibi = "Sahip"
        };

        service.ApplyProjectSelection(action, project);

        Assert.Equal(project.Id, action.ProjectId);
        Assert.Equal("100-1 Sahip", action.OwnerParcelText);
        Assert.Equal(string.Empty, action.WorkText);
    }

    [Fact]
    public void ApplyProjectSelection_YibfIsTakibi_Istinat_UsesParentAsWorkGroup()
    {
        var parentId = Guid.NewGuid();
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var entry = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            WorkGroupId = Guid.NewGuid(),
            WorkIdentityId = Guid.NewGuid()
        };
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId
        };

        service.ApplyProjectSelection(entry, project);

        Assert.Equal(parentId, entry.WorkGroupId);
        Assert.Equal(project.Id, entry.WorkIdentityId);
    }

    private sealed class InMemoryProjectCatalogRepository : IProjectCatalogRepository
    {
        private List<ProjectCatalogEntry> _entries = [];

        public Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectCatalogEntry>>(_entries.Select(item => item.Clone()).ToList());

        public Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
        {
            _entries = entries.Select(item => item.Clone()).ToList();
            return Task.CompletedTask;
        }
    }
}
