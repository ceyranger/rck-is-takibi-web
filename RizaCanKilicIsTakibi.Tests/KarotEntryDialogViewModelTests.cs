using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class KarotEntryDialogViewModelTests
{
    [Fact]
    public void BuildEntry_Maps_All_Optional_Column_Fields()
    {
        var catalogService = new ProjectCatalogService(new StubProjectCatalogRepository());
        var vm = new KarotEntryDialogViewModel(
            KarotSubTab.Bekleyen,
            catalogEntries: [],
            catalogService);

        var sampleDate = new DateTime(2026, 7, 15);
        vm.SampleReceivedDate = sampleDate;
        vm.AdaParsel = " 10-2 ";
        vm.YapiSahibi = " Sahip ";
        vm.YibfNo = " Y-1 ";
        vm.Muteahhit = " Muteahhit ";
        vm.KatBilgisi = " Zemin ";
        vm.BetonSinifi = " C30 ";
        vm.TwentyEightDayResult = " 32.1 ";
        vm.BetonFirmasi = " Firma ";
        vm.Laboratuvar = " Lab ";
        vm.Aciklama = " Not ";

        var entry = vm.BuildEntry();

        Assert.Equal(sampleDate, entry.SampleReceivedDate);
        Assert.Equal("10-2", entry.AdaParsel);
        Assert.Equal("Sahip", entry.YapiSahibi);
        Assert.Equal("Y-1", entry.YibfNo);
        Assert.Equal("Muteahhit", entry.Muteahhit);
        Assert.Equal("Zemin", entry.KatBilgisi);
        Assert.Equal("C30", entry.BetonSinifi);
        Assert.Equal("32.1", entry.TwentyEightDayResult);
        Assert.Equal("Firma", entry.BetonFirmasi);
        Assert.Equal("Lab", entry.Laboratuvar);
        Assert.Equal("Not", entry.Aciklama);
        Assert.Equal(KarotStatus.KarotAlinacak, entry.Status);
    }

    [Fact]
    public void Selecting_Project_Fills_Identity_And_Hides_Fields()
    {
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "Proje A",
            AdaParsel = "12-3",
            YapiSahibi = "Ali",
            YibfNo = "Y-9",
            Muteahhit = "Muteahhit A",
            Kind = ProjectCatalogKind.Normal,
            IsActive = true
        };
        var catalogService = new ProjectCatalogService(new StubProjectCatalogRepository());
        var vm = new KarotEntryDialogViewModel(KarotSubTab.Bekleyen, [project], catalogService);

        vm.SelectedProjectId = project.Id;

        Assert.True(vm.HasSelectedProject);
        Assert.False(vm.ShowIdentityFields);
        Assert.False(vm.IsProjectIdentityIncomplete);
        Assert.Equal("12-3", vm.AdaParsel);
        Assert.Equal("Ali", vm.YapiSahibi);
        Assert.Equal("Y-9", vm.YibfNo);
        Assert.Equal("Muteahhit A", vm.Muteahhit);
        Assert.Contains("12-3", vm.ProjectSummaryText, StringComparison.Ordinal);
        Assert.Contains("Ali", vm.ProjectSummaryText, StringComparison.Ordinal);

        var entry = vm.BuildEntry();
        Assert.Equal(project.Id, entry.ProjectId);
        Assert.Equal("12-3", entry.AdaParsel);
        Assert.Equal("Ali", entry.YapiSahibi);
        Assert.Equal("Muteahhit A", entry.Muteahhit);
    }

    [Fact]
    public void Incomplete_Project_Opens_Manual_Identity_Fields()
    {
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "Boş Kimlik",
            Kind = ProjectCatalogKind.Normal,
            IsActive = true
        };
        var catalogService = new ProjectCatalogService(new StubProjectCatalogRepository());
        var vm = new KarotEntryDialogViewModel(KarotSubTab.Bekleyen, [project], catalogService);

        vm.SelectedProjectId = project.Id;

        Assert.True(vm.IsProjectIdentityIncomplete);
        Assert.True(vm.ShowIdentityFields);
        Assert.True(vm.IsIdentityManualEdit);
    }

    [Fact]
    public void Toggle_Elle_Duzenle_Shows_And_Restores_From_Project()
    {
        var project = new ProjectCatalogEntry
        {
            Id = Guid.NewGuid(),
            DisplayName = "Proje B",
            AdaParsel = "1-1",
            YapiSahibi = "Veli",
            Kind = ProjectCatalogKind.Normal,
            IsActive = true
        };
        var catalogService = new ProjectCatalogService(new StubProjectCatalogRepository());
        var vm = new KarotEntryDialogViewModel(KarotSubTab.Bekleyen, [project], catalogService);
        vm.SelectedProjectId = project.Id;

        Assert.False(vm.ShowIdentityFields);
        vm.ToggleIdentityManualEditCommand.Execute(null);
        Assert.True(vm.ShowIdentityFields);
        Assert.Equal("Projeden kullan", vm.IdentityEditToggleText);

        vm.AdaParsel = "manuel";
        vm.ToggleIdentityManualEditCommand.Execute(null);
        Assert.False(vm.ShowIdentityFields);
        Assert.Equal("1-1", vm.AdaParsel);
    }

    private sealed class StubProjectCatalogRepository : IProjectCatalogRepository
    {
        public Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectCatalogEntry>>([]);

        public Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
