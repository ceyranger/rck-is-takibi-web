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

    private sealed class StubProjectCatalogRepository : IProjectCatalogRepository
    {
        public Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectCatalogEntry>>([]);

        public Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
