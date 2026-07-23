using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class DialogAndImportValidationTests
{
    [Fact]
    public void YibfAnaBilgiEntryDialogViewModel_Requires_YapiSahibi()
    {
        var catalogService = new ProjectCatalogService(new EmptyCatalogRepository());
        var viewModel = new YibfAnaBilgiEntryDialogViewModel(
            Array.Empty<ProjectCatalogEntry>(),
            catalogService,
            new YibfAnaBilgiEntryDialogResult
            {
                AdaParsel = "123/4",
                YapiSahibi = string.Empty
            });

        viewModel.SaveCommand.Execute(null);

        Assert.Equal("Yapı Sahibi alanı zorunludur.", viewModel.ValidationMessage);
    }

    [Fact]
    public async Task YibfImport_Skips_Invalid_AnaBilgi_Entries()
    {
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new FakeYibfImportService(
                new YibfImportData
                {
                    AnaBilgiEntries =
                    [
                        new YibfAnaBilgiEntry { Id = Guid.NewGuid(), AdaParsel = "100/1", YapiSahibi = "Geçerli" },
                        new YibfAnaBilgiEntry { Id = Guid.NewGuid(), AdaParsel = "", YapiSahibi = "" }
                    ],
                    AnaBilgiEvents =
                    [
                        new YibfAnaBilgiEvent { Id = Guid.NewGuid(), EntryId = Guid.Empty, Description = "Bozuk" }
                    ],
                    IsTakibiEntries = Array.Empty<YibfIsTakibiEntry>(),
                    CellStates = Array.Empty<YibfCellState>()
                }),
            new FixedFileDialogService("dummy.xlsx"),
            new NotificationService(),
            new ConfirmationServiceStub(),
            new NoteDialogServiceStub(),
            new YibfEventDialogServiceStub(),
            new YibfEntryDialogServiceStub(),
            new UndoRedoService());

        await module.ImportExcelCommand.ExecuteAsync(null);

        Assert.Single(module.AnaBilgiEntries);
        Assert.Equal("100/1", module.AnaBilgiEntries[0].AdaParsel);
        Assert.Empty(module.AnaBilgiEvents);
    }

    [Fact]
    public async Task YibfImport_Skips_IsTakibi_Entries_With_Blank_JobName()
    {
        var module = new YibfModuleViewModel(
            new SqliteYibfRepository(BuildDatabasePath()),
            new FakeYibfImportService(
                new YibfImportData
                {
                    AnaBilgiEntries = Array.Empty<YibfAnaBilgiEntry>(),
                    AnaBilgiEvents = Array.Empty<YibfAnaBilgiEvent>(),
                    IsTakibiEntries =
                    [
                        new YibfIsTakibiEntry { Id = Guid.NewGuid(), JobName = "Ruhsat", DisplayOrder = 0 },
                        new YibfIsTakibiEntry { Id = Guid.NewGuid(), JobName = "", DisplayOrder = 1 }
                    ],
                    CellStates = Array.Empty<YibfCellState>()
                }),
            new FixedFileDialogService("dummy.xlsx"),
            new NotificationService(),
            new ConfirmationServiceStub(),
            new NoteDialogServiceStub(),
            new YibfEventDialogServiceStub(),
            new YibfEntryDialogServiceStub(),
            new UndoRedoService());

        await module.ImportExcelCommand.ExecuteAsync(null);

        Assert.Single(module.IsTakibiEntries);
        Assert.Equal("Ruhsat", module.IsTakibiEntries[0].JobName);
    }

    [Fact]
    public async Task TadilatImport_Skips_Entries_With_Blank_District()
    {
        var module = new TadilatModuleViewModel(
            new SqliteTadilatRepository(BuildDatabasePath()),
            new FakeTadilatImportService(
                new TadilatImportData
                {
                    Entries =
                    [
                        new TadilatEntry { Id = Guid.NewGuid(), District = "MERKEZ", SubTab = TadilatSubTab.Aktif },
                        new TadilatEntry { Id = Guid.NewGuid(), District = "", SubTab = TadilatSubTab.Aktif }
                    ],
                    CellStates =
                    [
                        new TadilatCellState { EntryId = Guid.NewGuid(), ColumnKey = TadilatColumnKeys.JobName, NoteText = "orphan" }
                    ]
                }),
            new FixedFileDialogService("dummy.xlsx"),
            new NotificationService(),
            new ConfirmationServiceStub(),
            new NoteDialogServiceStub(),
            new UndoRedoService());

        await module.ImportExcelCommand.ExecuteAsync(null);

        Assert.Single(module.AktifEntries);
        Assert.Equal("SİNOP", module.AktifEntries[0].District);
        Assert.Empty(module.CellStates);
    }

    private static string BuildDatabasePath()
        => Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", $"{Guid.NewGuid():N}.db");

    private sealed class FixedFileDialogService(string openPath) : IFileDialogService
    {
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => openPath;
    }

    private sealed class FakeYibfImportService(YibfImportData data) : IYibfImportService
    {
        public Task<YibfImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult(data);
    }

    private sealed class FakeTadilatImportService(TadilatImportData data) : ITadilatImportService
    {
        public Task<TadilatImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult(data);
    }

    private sealed class ConfirmationServiceStub : IConfirmationService
    {
        public bool Confirm(ConfirmationRequest request) => true;
    }

    private sealed class NoteDialogServiceStub : ITadilatCellNoteDialogService
    {
        public Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default)
            => Task.FromResult<TadilatCellNoteDialogResult?>(null);
    }

    private sealed class YibfEventDialogServiceStub : IYibfAnaBilgiEventDialogService
    {
        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, string approvalStatus = "", CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(null);
    }

    private sealed class YibfEntryDialogServiceStub : IYibfAnaBilgiEntryDialogService
    {
        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEntryDialogResult?>(null);
    }

    private sealed class EmptyCatalogRepository : IProjectCatalogRepository
    {
        public Task<IReadOnlyList<ProjectCatalogEntry>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ProjectCatalogEntry>>(Array.Empty<ProjectCatalogEntry>());

        public Task SaveManyAsync(IEnumerable<ProjectCatalogEntry> entries, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
