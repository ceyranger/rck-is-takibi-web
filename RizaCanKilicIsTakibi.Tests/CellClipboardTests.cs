using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public class CellClipboardTests
{
    [Fact]
    public void ClipboardService_Serializes_And_Deserializes_Cell_Payload()
    {
        var payload = new CellClipboardPayload
        {
            Text = "Açıklama",
            BackgroundColor = "#FFFF0000",
            NoteText = "Not"
        };

        var serialized = ClipboardService.SerializePayload(payload);
        var deserialized = ClipboardService.DeserializePayload(serialized);

        Assert.NotNull(deserialized);
        Assert.Equal(payload.Text, deserialized!.Text);
        Assert.Equal(payload.BackgroundColor, deserialized.BackgroundColor);
        Assert.Equal(payload.NoteText, deserialized.NoteText);
    }

    [Fact]
    public void ClipboardService_Rejects_Invalid_Cell_Payload()
    {
        Assert.Null(ClipboardService.DeserializePayload("{ invalid json"));
        Assert.Null(ClipboardService.DeserializePayload(string.Empty));
    }

    [Fact]
    public async Task MissingProject_Copy_Then_Paste_Persists_Target_Cell_With_Color_And_Note()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var clipboard = new FakeClipboardService();
            var repository = new SqliteMissingProjectRepository(databasePath);
            var module = new MissingProjectModuleViewModel(
                repository,
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(clipboard),
                new UndoRedoService(),
                new AppSettings(),
                clipboard);

            module.LoadFromBackup(
            [
                new MissingProjectEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "101/1",
                    YapiSahibi = "Kaynak",
                    RecordMedium = MissingProjectMedium.Fiziki,
                    RecordMediumText = "Fiziksel",
                    MissingProjectText = "Statik",
                    Description = "Kaynak açıklama",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new MissingProjectEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "202/2",
                    YapiSahibi = "Hedef",
                    RecordMedium = MissingProjectMedium.Dijital,
                    RecordMediumText = "Dijital",
                    MissingProjectText = "Elektrik",
                    Description = "Hedef açıklama",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ], markDirty: false);

            var sourceCell = module.Rows[0].DescriptionCell;
            var targetCell = module.Rows[1].DescriptionCell;
            module.SetCellColorBlueCommand.Execute(sourceCell);
            clipboard.NextDialogNoteText = "Kaynak not";
            await module.EditCellNoteCommand.ExecuteAsync(sourceCell);

            sourceCell = module.Rows[0].DescriptionCell;
            targetCell = module.Rows[1].DescriptionCell;
            module.CopyCellCommand.Execute(sourceCell);
            module.PasteCellCommand.Execute(targetCell);

            var refreshedTargetCell = module.Rows.Single(row => row.Entry.Id == targetCell.Row.Entry.Id).DescriptionCell;
            Assert.True(module.HasUnsavedChanges);
            Assert.Equal("Kaynak açıklama", refreshedTargetCell.Text);
            Assert.Equal("#FF4F81BD", refreshedTargetCell.BackgroundColor);
            Assert.Equal("Kaynak not", refreshedTargetCell.NoteText);

            await module.PersistAsync();

            Assert.False(module.HasUnsavedChanges);
            var saved = await repository.GetAllAsync();
            Assert.Equal("Kaynak açıklama", saved.Single(item => item.Id == module.Rows[1].Entry.Id).Description);
            var states = await repository.GetCellStatesAsync();
            var targetState = Assert.Single(states, item => item.EntryId == module.Rows[1].Entry.Id && item.ColumnKey == MissingProjectColumnKeys.Description);
            Assert.Equal("#FF4F81BD", targetState.BackgroundColor);
            Assert.Equal("Kaynak not", targetState.NoteText);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    [Fact]
    public async Task Karot_Paste_Updates_Cell_And_Persists()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var clipboard = new FakeClipboardService { Text = "999/9" };
            var repository = new SqliteKarotRepository(databasePath);
            var module = new KarotModuleViewModel(
                repository,
                new TestKarotStatusDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new UndoRedoService(),
                clipboard);

            module.LoadFromBackup(
            [
                new KarotEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "101/1",
                    Status = KarotStatus.KarotAlinacak,
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ], markDirty: false);

            var cell = module.VisibleRows.Single().AdaParselCell;
            module.PasteCellCommand.Execute(cell);

            Assert.True(module.HasUnsavedChanges);
            Assert.Equal("999/9", cell.Text);

            await module.PersistAsync();

            Assert.False(module.HasUnsavedChanges);
            var saved = await repository.GetAllAsync();
            Assert.Equal("999/9", Assert.Single(saved).AdaParsel);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    [Fact]
    public void Karot_Begin_Commit_CellEdit_Updates_Model_Only_On_Commit()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var module = new KarotModuleViewModel(
                new SqliteKarotRepository(databasePath),
                new TestKarotStatusDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new UndoRedoService(),
                new FakeClipboardService());

            module.LoadFromBackup(
            [
                new KarotEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "101/1",
                    Status = KarotStatus.KarotAlinacak,
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ], markDirty: false);

            var cell = module.VisibleRows.Single().AdaParselCell;
            module.BeginCellEditCommand.Execute(cell);

            Assert.True(cell.IsEditing);
            cell.DraftText = "202/2";
            Assert.Equal("101/1", cell.Text);
            Assert.Equal("101/1", cell.Row.Entry.AdaParsel);
            Assert.False(module.HasUnsavedChanges);

            module.CommitCellEditCommand.Execute(cell);

            Assert.False(cell.IsEditing);
            Assert.Equal("202/2", cell.Text);
            Assert.Equal("202/2", cell.Row.Entry.AdaParsel);
            Assert.True(module.HasUnsavedChanges);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    [Fact]
    public void Karot_CommitPendingEdits_Commits_Open_Draft()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var module = new KarotModuleViewModel(
                new SqliteKarotRepository(databasePath),
                new TestKarotStatusDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(),
                new UndoRedoService(),
                new FakeClipboardService());

            module.LoadFromBackup(
            [
                new KarotEntry
                {
                    Id = Guid.NewGuid(),
                    YibfNo = "Y-1",
                    Status = KarotStatus.KarotAlinacak,
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ], markDirty: false);

            var cell = module.VisibleRows.Single().YibfNoCell;
            module.BeginCellEditCommand.Execute(cell);
            cell.DraftText = "Y-9";

            module.CommitPendingEdits();

            Assert.False(cell.IsEditing);
            Assert.Equal("Y-9", cell.Text);
            Assert.Equal("Y-9", cell.Row.Entry.YibfNo);
            Assert.True(module.HasUnsavedChanges);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    [Fact]
    public async Task MissingProject_Paste_Clears_Target_Color_And_Note_When_Source_Is_Empty()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var clipboard = new FakeClipboardService();
            var repository = new SqliteMissingProjectRepository(databasePath);
            var module = new MissingProjectModuleViewModel(
                repository,
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(clipboard),
                new UndoRedoService(),
                new AppSettings(),
                clipboard);

            module.LoadFromBackup(
            [
                new MissingProjectEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "101/1",
                    YapiSahibi = "Kaynak",
                    RecordMedium = MissingProjectMedium.Fiziki,
                    RecordMediumText = "Fiziksel",
                    MissingProjectText = "Statik",
                    Description = "Temiz kaynak",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new MissingProjectEntry
                {
                    Id = Guid.NewGuid(),
                    AdaParsel = "202/2",
                    YapiSahibi = "Hedef",
                    RecordMedium = MissingProjectMedium.Dijital,
                    RecordMediumText = "Dijital",
                    MissingProjectText = "Elektrik",
                    Description = "Hedef açıklama",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ], markDirty: false);

            var sourceCell = module.Rows[0].DescriptionCell;
            var targetCell = module.Rows[1].DescriptionCell;
            module.SetCellColorGreenCommand.Execute(targetCell);
            clipboard.NextDialogNoteText = "Eski not";
            await module.EditCellNoteCommand.ExecuteAsync(targetCell);

            sourceCell = module.Rows[0].DescriptionCell;
            targetCell = module.Rows[1].DescriptionCell;
            module.CopyCellCommand.Execute(sourceCell);
            module.PasteCellCommand.Execute(targetCell);

            var refreshedTargetCell = module.Rows.Single(row => row.Entry.Id == targetCell.Row.Entry.Id).DescriptionCell;
            Assert.Equal(string.Empty, refreshedTargetCell.BackgroundColor);
            Assert.Equal(string.Empty, refreshedTargetCell.NoteText);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    [Fact]
    public async Task Tadilat_Paste_Updates_Active_Cell_And_Persists_With_Color_And_Note()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var clipboard = new FakeClipboardService { Text = "Yeni İş" };
            var repository = new SqliteTadilatRepository(databasePath);
            var module = new TadilatModuleViewModel(
                repository,
                new TestTadilatImportService(),
                new TestFileDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(clipboard),
                new UndoRedoService(),
                clipboard);

            module.LoadFromBackup(
            [
                new TadilatEntry
                {
                    Id = Guid.NewGuid(),
                    District = "MERKEZ",
                    SubTab = TadilatSubTab.Aktif,
                    JobName = "Yeni İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ], Array.Empty<TadilatCellState>(), markDirty: false);

            var row = module.DistrictGroups.SelectMany(group => group.Rows).Single(item => !item.IsPlaceholder);
            module.SetCellColorYellowCommand.Execute(row.JobNameCell);
            clipboard.NextDialogNoteText = "Tadilat notu";
            await module.EditCellNoteCommand.ExecuteAsync(row.JobNameCell);

            var targetEntry = new TadilatEntry
            {
                Id = Guid.NewGuid(),
                District = "MERKEZ",
                SubTab = TadilatSubTab.Aktif,
                JobName = "Hedef İş",
                DisplayOrder = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            var sourceEntry = row.Entry ?? throw new InvalidOperationException("Expected a loaded tadilat row entry.");

            module.LoadFromBackup(
            [
                sourceEntry,
                targetEntry
            ],
            module.GetCellStatesSnapshot(),
            markDirty: false);

            var sourceRow = module.DistrictGroups.SelectMany(group => group.Rows).Where(item => !item.IsPlaceholder).First();
            var targetRow = module.DistrictGroups.SelectMany(group => group.Rows).Where(item => !item.IsPlaceholder).Last();
            module.CopyCellCommand.Execute(sourceRow.JobNameCell);
            module.PasteCellCommand.Execute(targetRow.JobNameCell);

            var refreshedTargetRow = module.DistrictGroups.SelectMany(group => group.Rows).Single(item => !item.IsPlaceholder && item.Entry is not null && item.Entry.Id == targetEntry.Id);
            Assert.True(module.HasUnsavedChanges);
            Assert.Equal("Yeni İş", refreshedTargetRow.JobNameCell.Text);
            Assert.Equal("#FFFFFF00", refreshedTargetRow.JobNameCell.BackgroundColor);
            Assert.Equal("Tadilat notu", refreshedTargetRow.JobNameCell.NoteText);

            await module.PersistAsync();

            Assert.False(module.HasUnsavedChanges);
            var saved = await repository.GetAllAsync();
            Assert.Equal("Yeni İş", saved.Single(item => item.Id == targetEntry.Id).JobName);
            var states = await repository.GetCellStatesAsync();
            var targetState = Assert.Single(states, item => item.EntryId == targetEntry.Id && item.ColumnKey == TadilatColumnKeys.JobName);
            Assert.Equal("#FFFFFF00", targetState.BackgroundColor);
            Assert.Equal("Tadilat notu", targetState.NoteText);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    [Fact]
    public void Tadilat_Paste_Is_Disabled_For_ReadOnly_Cells()
    {
        var clipboard = new FakeClipboardService { Text = "Deneme" };
        var module = new TadilatModuleViewModel(
            new SqliteTadilatRepository(BuildDatabasePath()),
            new TestTadilatImportService(),
            new TestFileDialogService(),
            new NotificationService(),
            new ConfirmationServiceStub(),
            new TestTadilatCellNoteDialogService(),
            new UndoRedoService(),
            clipboard);

        module.LoadFromBackup(
        [
            new TadilatEntry
            {
                Id = Guid.NewGuid(),
                District = "MERKEZ",
                SubTab = TadilatSubTab.Biten,
                JobName = "Tamam",
                DisplayOrder = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        ], Array.Empty<TadilatCellState>(), markDirty: false);

        module.SelectedSubTab = TadilatSubTab.Biten;
        var row = module.DistrictGroups.SelectMany(group => group.Rows).Single(item => !item.IsPlaceholder);
        Assert.False(row.JobNameCell.IsInteractive);
        Assert.False(module.PasteCellCommand.CanExecute(row.JobNameCell));
    }

    [Fact]
    public async Task Yibf_Paste_Updates_Cell_And_Persists_With_Color_And_Note()
    {
        var databasePath = BuildDatabasePath();

        try
        {
            var clipboard = new FakeClipboardService { Text = "Yeni YİBF İş" };
            var repository = new SqliteYibfRepository(databasePath);
            var module = new YibfModuleViewModel(
                repository,
                new TestYibfImportService(),
                new TestFileDialogService(),
                new NotificationService(),
                new ConfirmationServiceStub(),
                new TestTadilatCellNoteDialogService(clipboard),
                new TestYibfAnaBilgiEventDialogService(),
                new TestYibfAnaBilgiEntryDialogService(),
                new UndoRedoService(),
                clipboard);

            module.LoadFromBackup(
                Array.Empty<YibfAnaBilgiEntry>(),
                Array.Empty<YibfAnaBilgiEvent>(),
            [
                new YibfIsTakibiEntry
                {
                    Id = Guid.NewGuid(),
                    JobName = "Yeni YİBF İş",
                    DisplayOrder = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                },
                new YibfIsTakibiEntry
                {
                    Id = Guid.NewGuid(),
                    JobName = "Hedef YİBF İş",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            ],
                Array.Empty<YibfCellState>(),
                markDirty: false);

            var sourceRow = module.IsTakibiRows.First();
            var targetRow = module.IsTakibiRows.Last();
            module.SetCellColorRedCommand.Execute(sourceRow.JobNameCell);
            clipboard.NextDialogNoteText = "YİBF notu";
            await module.EditCellNoteCommand.ExecuteAsync(sourceRow.JobNameCell);

            sourceRow = module.IsTakibiRows.First();
            targetRow = module.IsTakibiRows.Last();
            module.CopyCellCommand.Execute(sourceRow.JobNameCell);
            module.PasteCellCommand.Execute(targetRow.JobNameCell);

            var refreshedTargetRow = module.IsTakibiRows.Single(item => item.Entry.Id == targetRow.Entry.Id);
            Assert.True(module.HasUnsavedChanges);
            Assert.Equal("Yeni YİBF İş", refreshedTargetRow.JobNameCell.Text);
            Assert.Equal("#FFFF0000", refreshedTargetRow.JobNameCell.BackgroundColor);
            Assert.Equal("YİBF notu", refreshedTargetRow.JobNameCell.NoteText);

            await module.PersistAsync();

            Assert.False(module.HasUnsavedChanges);
            var saved = await repository.GetIsTakibiEntriesAsync();
            Assert.Equal("Yeni YİBF İş", saved.Single(item => item.Id == targetRow.Entry.Id).JobName);
            var states = await repository.GetCellStatesAsync();
            var targetState = Assert.Single(states, item => item.EntryId == targetRow.Entry.Id && item.ColumnKey == YibfIsTakibiColumnKeys.JobName);
            Assert.Equal("#FFFF0000", targetState.BackgroundColor);
            Assert.Equal("YİBF notu", targetState.NoteText);
        }
        finally
        {
            DeleteDatabaseArtifacts(databasePath);
        }
    }

    private static string BuildDatabasePath()
        => Path.Combine(Path.GetTempPath(), "RizaCanKilicIsTakibiTests", $"{Guid.NewGuid():N}.db");

    private static void DeleteDatabaseArtifacts(string databasePath)
    {
        DeleteIfExists(databasePath);
        DeleteIfExists($"{databasePath}-wal");
        DeleteIfExists($"{databasePath}-shm");
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string? Text { get; set; }
        public CellClipboardPayload? Payload { get; set; }
        public string? NextDialogNoteText { get; set; }

        public bool ContainsText() => !string.IsNullOrEmpty(Text);

        public bool TryGetText(out string? text)
        {
            text = Text;
            return !string.IsNullOrEmpty(Text);
        }

        public bool TrySetText(string? text)
        {
            Text = text;
            return true;
        }

        public bool TryGetCellPayload(out CellClipboardPayload? payload)
        {
            payload = Payload;
            return payload is not null;
        }

        public bool TrySetCellPayload(CellClipboardPayload payload)
        {
            Payload = payload;
            Text = payload.Text;
            return true;
        }
    }

    private sealed class ConfirmationServiceStub : IConfirmationService
    {
        public bool Confirm(ConfirmationRequest request) => true;
    }

    private sealed class TestKarotStatusDialogService : IKarotStatusDialogService
    {
        public Task<KarotStatus?> ShowDialogAsync(KarotStatus currentStatus, CancellationToken cancellationToken = default)
            => Task.FromResult<KarotStatus?>(currentStatus);
    }

    private sealed class TestTadilatCellNoteDialogService : ITadilatCellNoteDialogService
    {
        private readonly FakeClipboardService? _clipboard;

        public TestTadilatCellNoteDialogService(FakeClipboardService? clipboard = null)
        {
            _clipboard = clipboard;
        }

        public Task<TadilatCellNoteDialogResult?> ShowDialogAsync(string currentNote, CancellationToken cancellationToken = default)
        {
            if (_clipboard?.NextDialogNoteText is null)
            {
                return Task.FromResult<TadilatCellNoteDialogResult?>(null);
            }

            var noteText = _clipboard.NextDialogNoteText;
            _clipboard.NextDialogNoteText = null;
            return Task.FromResult<TadilatCellNoteDialogResult?>(new TadilatCellNoteDialogResult
            {
                NoteText = noteText ?? string.Empty,
                DeleteRequested = false
            });
        }
    }

    private sealed class TestTadilatImportService : ITadilatImportService
    {
        public Task<TadilatImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new TadilatImportData());
    }

    private sealed class TestFileDialogService : IFileDialogService
    {
        public string? ShowSaveDialog(string title, string filter, string defaultExtension) => null;
        public string? ShowOpenDialog(string title, string filter, bool multiselect = false) => null;
        public string? ShowFolderDialog(string title) => null;
    }

    private sealed class TestYibfImportService : IYibfImportService
    {
        public Task<YibfImportData> ImportAsync(string filePath, CancellationToken cancellationToken = default)
            => Task.FromResult(new YibfImportData());
    }

    private sealed class TestYibfAnaBilgiEventDialogService : IYibfAnaBilgiEventDialogService
    {
        public Task<YibfAnaBilgiEventDialogResult?> ShowDialogAsync(DateTime? eventDate, string description, string backgroundColor, string noteText, string approvalStatus = "", CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEventDialogResult?>(null);
    }

    private sealed class TestYibfAnaBilgiEntryDialogService : IYibfAnaBilgiEntryDialogService
    {
        public Task<YibfAnaBilgiEntryDialogResult?> ShowDialogAsync(YibfAnaBilgiEntryDialogResult? initialValues = null, bool isEditMode = false, CancellationToken cancellationToken = default)
            => Task.FromResult<YibfAnaBilgiEntryDialogResult?>(null);
    }
}
