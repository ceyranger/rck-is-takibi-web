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
        Assert.Equal("Ali Veli", karot.Muteahhit);
    }

    [Fact]
    public void ApplyProjectSelection_Karot_Istinat_UsesParentIdentityAndMuteahhitFallback()
    {
        var parentId = Guid.NewGuid();
        var parent = new ProjectCatalogEntry
        {
            Id = parentId,
            DisplayName = "100-1 Fahrettin Gençgün",
            AdaParsel = "100-1",
            YapiSahibi = "Fahrettin Gençgün",
            YibfNo = "77",
            Kind = ProjectCatalogKind.Normal
        };
        var istinat = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId
        };
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var karot = new KarotEntry();

        service.ApplyProjectSelection(karot, istinat, [parent, istinat]);

        Assert.Equal(istinat.Id, karot.ProjectId);
        Assert.Equal("100-1", karot.AdaParsel);
        Assert.Equal("Fahrettin Gençgün", karot.YapiSahibi);
        Assert.Equal("77", karot.YibfNo);
        Assert.Equal("Fahrettin Gençgün", karot.Muteahhit);
    }

    [Fact]
    public void Search_MatchesIstinatViaParentOwner()
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
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());

        var results = service.Search([parent, istinat], "Fahrettin");

        Assert.Contains(results, item => item.Id == parent.Id);
        Assert.Contains(results, item => item.Id == istinat.Id);
    }

    [Fact]
    public void ApplyProjectSelection_Tadilat_Istinat_UsesEffectiveJobName()
    {
        var parentId = Guid.NewGuid();
        var parent = new ProjectCatalogEntry
        {
            Id = parentId,
            DisplayName = "100-1 Fahrettin Gençgün",
            AdaParsel = "100-1",
            YapiSahibi = "Fahrettin Gençgün",
            Kind = ProjectCatalogKind.Normal
        };
        var istinat = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId
        };
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var tadilat = new TadilatEntry();

        service.ApplyProjectSelection(tadilat, istinat, [parent, istinat]);

        Assert.Equal(istinat.Id, tadilat.ProjectId);
        Assert.Equal("İstinat · Fahrettin Gençgün", tadilat.JobName);
    }

    [Fact]
    public void ApplyProjectSelection_ActionAndMissing_Istinat_UsesParentIdentity()
    {
        var parentId = Guid.NewGuid();
        var parent = new ProjectCatalogEntry
        {
            Id = parentId,
            AdaParsel = "100-1",
            YapiSahibi = "Fahrettin Gençgün",
            Kind = ProjectCatalogKind.Normal
        };
        var istinat = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "İstinat",
            Kind = ProjectCatalogKind.Istinat,
            ParentProjectId = parentId
        };
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var action = new ActionEntry();
        var missing = new MissingProjectEntry();

        service.ApplyProjectSelection(action, istinat, [parent, istinat]);
        service.ApplyProjectSelection(missing, istinat, [parent, istinat]);

        Assert.Equal("100-1 Fahrettin Gençgün", action.OwnerParcelText);
        Assert.Equal("100-1", missing.AdaParsel);
        Assert.Equal("Fahrettin Gençgün", missing.YapiSahibi);
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

    [Fact]
    public void OverwriteLinkedIdentityFields_Previews_And_Replaces_Only_Identity_Fields()
    {
        var service = new ProjectCatalogService(new InMemoryProjectCatalogRepository());
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "Yeni Proje",
            AdaParsel = "10/2",
            YapiSahibi = "Yeni Sahip",
            YibfNo = "99",
            Muteahhit = "Yeni Müteahhit"
        };
        var karot = new KarotEntry { ProjectId = project.Id, AdaParsel = "Eski", Aciklama = "Korunacak" };
        var missing = new MissingProjectEntry { ProjectId = project.Id, AdaParsel = "Eski", Description = "Korunacak" };
        var action = new ActionEntry { ProjectId = project.Id, OwnerParcelText = "Eski", WorkText = "Korunacak" };
        var tadilat = new TadilatEntry { ProjectId = project.Id, JobName = "Eski", Description1 = "Korunacak" };
        var yibf = new YibfIsTakibiEntry { WorkGroupId = project.Id, WorkIdentityId = project.Id, JobName = "Eski" };
        var anaBilgi = new YibfAnaBilgiEntry
        {
            Id = project.Id,
            WorkGroupId = project.Id,
            WorkIdentityId = project.Id,
            AdaParsel = "Eski",
            YapiSahibi = "Eski Sahip",
            YibfNo = "1",
            Idare = "Eski Belediye",
            Muteahhit = "Eski Müteahhit"
        };

        var preview = service.PreviewLinkedIdentityOverwrite(project, [anaBilgi], [karot], [missing], [action], [tadilat], [yibf]);
        var applied = service.OverwriteLinkedIdentityFields(project, [anaBilgi], [karot], [missing], [action], [tadilat], [yibf]);

        Assert.Equal(6, preview.TotalCount);
        Assert.Equal(6, applied.TotalCount);
        Assert.Equal("10/2", anaBilgi.AdaParsel);
        Assert.Equal("Yeni Sahip", anaBilgi.YapiSahibi);
        Assert.Equal("99", anaBilgi.YibfNo);
        Assert.Equal(string.Empty, anaBilgi.Idare);
        Assert.Equal("Yeni Müteahhit", anaBilgi.Muteahhit);
        Assert.Equal("10/2", karot.AdaParsel);
        Assert.Equal("Yeni Sahip", karot.YapiSahibi);
        Assert.Equal("99", karot.YibfNo);
        Assert.Equal("Yeni Müteahhit", karot.Muteahhit);
        Assert.Equal("10/2 Yeni Sahip", action.OwnerParcelText);
        Assert.Equal("10/2 Yeni Sahip", tadilat.JobName);
        Assert.Equal("10/2 Yeni Sahip", yibf.JobName);
        Assert.Equal("Korunacak", karot.Aciklama);
        Assert.Equal("Korunacak", missing.Description);
        Assert.Equal("Korunacak", action.WorkText);
        Assert.Equal("Korunacak", tadilat.Description1);
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
