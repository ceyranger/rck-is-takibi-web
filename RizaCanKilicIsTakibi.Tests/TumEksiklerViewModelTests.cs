using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class TumEksiklerViewModelTests
{
    [Fact]
    public void RefreshFrom_GroupsYibfAnaBilgiRedAndYellowEventsUnderEntry()
    {
        var entry = CreateAnaBilgiEntry();
        var criticalEvent = new YibfAnaBilgiEvent
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            Description = "Beton evrakı eksik",
            BackgroundColor = "#FFFF0000",
            EventDate = new DateTime(2026, 6, 1)
        };
        var warningEvent = new YibfAnaBilgiEvent
        {
            Id = Guid.NewGuid(),
            EntryId = entry.Id,
            Description = "İdare dönüş bekleniyor",
            BackgroundColor = "#FFFFFF00",
            EventDate = new DateTime(2026, 6, 2)
        };

        var vm = CreateViewModel([entry], [criticalEvent, warningEvent]);

        var group = Assert.Single(vm.Groups);
        Assert.Equal(2, group.EksikCount);
        Assert.Equal(1, group.CriticalCount);
        Assert.Contains(group.Items, item => item.Severity == EksikSeverity.Critical && item.NavigationTarget.Kind == SearchResultKind.YibfAnaBilgiEvent);
        Assert.Contains(group.Items, item => item.Severity == EksikSeverity.Warning && item.Reason.Contains("İdare", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RefreshFrom_CreatesYibfCellColorAndBlankRequiredIssues()
    {
        var entry = CreateAnaBilgiEntry(yibfNo: "12345");
        var isTakibi = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = "12345 ruhsat işi",
            MuellifBilgileriGeldiMi = "Geldi",
            UpdatedAt = new DateTime(2026, 6, 3)
        };
        var state = new YibfCellState
        {
            EntryId = isTakibi.Id,
            ColumnKey = YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi,
            BackgroundColor = "#FFFF0000",
            NoteText = "Müellif dosyasında eksik var"
        };

        var vm = CreateViewModel([entry], isTakibiEntries: [isTakibi], yibfCellStates: [state]);

        var group = Assert.Single(vm.Groups);
        Assert.Contains(group.Items, item => item.Severity == EksikSeverity.Critical && item.FieldLabel.Contains("Müellif", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(group.Items, item => item.Severity == EksikSeverity.BlankRequired && item.Reason == "Boş takip alanı");
        Assert.DoesNotContain(group.Items, item => item.FieldLabel == nameof(YibfIsTakibiEntry.JobName));
    }

    [Fact]
    public void RefreshFrom_CreatesOnlyActiveTadilatBlankAndColorIssues()
    {
        var entry = CreateAnaBilgiEntry(adaParsel: "10/20");
        var active = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            SubTab = TadilatSubTab.Aktif,
            District = "Merkez",
            JobName = "10/20 tadilat",
            ProjectType = "Tadilat Projesi",
            DigitalReceived = "Geldi",
            Description1 = "Rapor bekleniyor",
            UpdatedAt = new DateTime(2026, 6, 4)
        };
        var biten = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            SubTab = TadilatSubTab.Biten,
            JobName = "10/20 biten tadilat"
        };
        var state = new TadilatCellState
        {
            EntryId = active.Id,
            ColumnKey = TadilatColumnKeys.DigitalReceived,
            BackgroundColor = "#FFFFFF00",
            NoteText = "Dijital dosya kontrol edilecek"
        };

        var vm = CreateViewModel([entry], aktifTadilatEntries: [active], tadilatCellStates: [state]);

        var group = Assert.Single(vm.Groups);
        Assert.Contains(group.Items, item => item.SourceModule == "Tadilat" && item.Severity == EksikSeverity.Warning);
        Assert.Contains(group.Items, item => item.SourceModule == "Tadilat" && item.Severity == EksikSeverity.BlankRequired);
        Assert.Contains(group.Items, item => item.SourceModule == "Tadilat" &&
                                             item.SourceContext == "Satır: İlçe: Merkez | İş: 10/20 tadilat | Proje Türü: Tadilat Projesi | Açıklama: Rapor bekleniyor");
        Assert.DoesNotContain(group.Items, item => item.SourceModule == "Tadilat" && item.SourceContext.Contains("(boş)", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(group.Items, item => item.NavigationTarget.ItemId == biten.Id);
    }

    [Fact]
    public void RefreshFrom_AddsDistinctTadilatSourceContextForSameColumnIssues()
    {
        var entry = CreateAnaBilgiEntry(adaParsel: "10/20");
        var first = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            SubTab = TadilatSubTab.Aktif,
            District = "Merkez",
            JobName = "10/20 A Blok tadilat",
            DigitalReceived = "Geldi"
        };
        var second = new TadilatEntry
        {
            Id = Guid.NewGuid(),
            SubTab = TadilatSubTab.Aktif,
            District = "Çarşı",
            JobName = "10/20 B Blok tadilat",
            DigitalReceived = "Geldi"
        };
        var states = new[]
        {
            new TadilatCellState
            {
                EntryId = first.Id,
                ColumnKey = TadilatColumnKeys.DigitalReceived,
                BackgroundColor = "#FFFFFF00"
            },
            new TadilatCellState
            {
                EntryId = second.Id,
                ColumnKey = TadilatColumnKeys.DigitalReceived,
                BackgroundColor = "#FFFFFF00"
            }
        };

        var vm = CreateViewModel([entry], aktifTadilatEntries: [first, second], tadilatCellStates: states);

        var contexts = vm.Groups
            .Single()
            .Items
            .Where(item => item.SourceModule == "Tadilat" && item.FieldLabel == "Projenin dijitali geldi mi?" && item.Severity == EksikSeverity.Warning)
            .Select(item => item.SourceContext)
            .ToList();

        Assert.Contains("Satır: İlçe: Merkez | İş: 10/20 A Blok tadilat", contexts);
        Assert.Contains("Satır: İlçe: Çarşı | İş: 10/20 B Blok tadilat", contexts);
    }

    [Fact]
    public void RefreshFrom_PutsUnmatchedRecordsUnderSeparateGroup()
    {
        var entry = CreateAnaBilgiEntry(yibfNo: "12345", adaParsel: "10/20");
        var unmatched = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = "99999 30/40 başka iş"
        };

        var vm = CreateViewModel([entry], isTakibiEntries: [unmatched]);

        var group = Assert.Single(vm.Groups);
        Assert.Equal(EksikMatchStatus.Unmatched, group.MatchStatus);
        Assert.Equal("Eşleşmeyen Eksikler", group.AdaParsel);
        Assert.All(group.Items, item => Assert.Equal(MainNavigationTab.YibfIsTakibi, item.TargetTab));
    }

    [Fact]
    public void RefreshFrom_SortsCriticalGroupsBeforeWarningsAndBlanks()
    {
        var criticalEntry = CreateAnaBilgiEntry(adaParsel: "1/1", yapiSahibi: "Kritik", yibfNo: "11111");
        var blankEntry = CreateAnaBilgiEntry(adaParsel: "2/2", yapiSahibi: "Boş", yibfNo: "22222");
        var criticalEvent = new YibfAnaBilgiEvent
        {
            Id = Guid.NewGuid(),
            EntryId = criticalEntry.Id,
            Description = "Kritik olay",
            BackgroundColor = "#FFFF0000"
        };
        var blankRow = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = blankEntry.YibfNo
        };

        var vm = CreateViewModel([blankEntry, criticalEntry], [criticalEvent], [blankRow]);

        Assert.Equal(criticalEntry.Id, vm.Groups[0].EntryId);
        Assert.Equal(blankEntry.Id, vm.Groups[1].EntryId);
    }

    [Fact]
    public void RefreshFrom_IncludesMissingProjectAndKarotButSkipsPositiveKarot()
    {
        var entry = CreateAnaBilgiEntry(adaParsel: "10/20", yibfNo: "12345");
        var missingProject = new MissingProjectEntry
        {
            Id = Guid.NewGuid(),
            AdaParsel = "10/20",
            MissingProjectText = "Mimari proje eksik",
            UpdatedAt = new DateTime(2026, 6, 5)
        };
        var negativeKarot = new KarotEntry
        {
            Id = Guid.NewGuid(),
            YibfNo = "12345",
            KatBilgisi = "Bodrum Kat",
            Status = KarotStatus.KarotAlindiOlumsuz,
            UpdatedAt = new DateTime(2026, 6, 6)
        };
        var positiveKarot = new KarotEntry
        {
            Id = Guid.NewGuid(),
            YibfNo = "12345",
            Status = KarotStatus.KarotAlindiOlumlu
        };

        var vm = CreateViewModel([entry], missingProjectEntries: [missingProject], karotEntries: [negativeKarot, positiveKarot]);

        var group = Assert.Single(vm.Groups);
        Assert.Contains(group.Items, item => item.SourceModule == "Eksik Proje" && item.NavigationTarget.Kind == SearchResultKind.MissingProjectEntry);
        Assert.Contains(group.Items, item => item.SourceModule == "Karot" && item.Severity == EksikSeverity.Critical);
        Assert.Contains(group.Items, item => item.SourceModule == "Karot" && item.SourceContext.Contains("Kat Bilgisi: Bodrum Kat", StringComparison.Ordinal));
        Assert.DoesNotContain(group.Items, item => item.NavigationTarget.ItemId == positiveKarot.Id);
    }

    [Fact]
    public void SearchQuery_MatchesYibfIsTakibiSourceContext()
    {
        var entry = CreateAnaBilgiEntry(yibfNo: "12345");
        var isTakibi = new YibfIsTakibiEntry
        {
            Id = Guid.NewGuid(),
            JobName = "12345 ruhsat özel takip"
        };
        var vm = CreateViewModel([entry], isTakibiEntries: [isTakibi]);

        vm.SearchQuery = "özel takip";

        var group = Assert.Single(vm.Groups);
        Assert.All(group.Items, item => Assert.Contains("Satır: İş: 12345 ruhsat özel takip", item.SourceContext, StringComparison.Ordinal));
    }

    private static TumEksiklerViewModel CreateViewModel(
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IReadOnlyList<YibfAnaBilgiEvent>? anaBilgiEvents = null,
        IReadOnlyList<YibfIsTakibiEntry>? isTakibiEntries = null,
        IReadOnlyList<YibfCellState>? yibfCellStates = null,
        IReadOnlyList<TadilatEntry>? aktifTadilatEntries = null,
        IReadOnlyList<TadilatCellState>? tadilatCellStates = null,
        IReadOnlyList<MissingProjectEntry>? missingProjectEntries = null,
        IReadOnlyList<KarotEntry>? karotEntries = null)
    {
        var vm = new TumEksiklerViewModel();
        vm.RefreshFrom(
            anaBilgiEntries,
            anaBilgiEvents ?? [],
            isTakibiEntries ?? [],
            yibfCellStates ?? [],
            aktifTadilatEntries ?? [],
            tadilatCellStates ?? [],
            missingProjectEntries ?? [],
            karotEntries ?? []);
        return vm;
    }

    private static YibfAnaBilgiEntry CreateAnaBilgiEntry(
        string adaParsel = "10/20",
        string yapiSahibi = "Ali Veli",
        string yibfNo = "12345")
        => new()
        {
            Id = Guid.NewGuid(),
            AdaParsel = adaParsel,
            YapiSahibi = yapiSahibi,
            YibfNo = yibfNo,
            Muteahhit = "Örnek Müteahhit",
            UpdatedAt = new DateTime(2026, 6, 1)
        };
}
