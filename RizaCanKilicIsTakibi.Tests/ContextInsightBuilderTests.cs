using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class ContextInsightBuilderTests
{
    [Fact]
    public void Build_Returns_Record_Not_Found_Message_When_No_Record_Matches()
    {
        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "642-25",
            NormalizedKey = "642-25",
            MatchType = ContextQueryMatchType.AdaParsel,
            IntentType = ContextQueryIntentType.GeneralStatus
        };

        var result = builder.Build(
            match,
            Array.Empty<SearchResultItem>(),
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            Array.Empty<KarotEntry>(),
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Equal("642-25 için kayıt bulunamadı.", result.AnswerText);
        Assert.Empty(result.Sections);
        Assert.Empty(result.Sources);
    }

    [Fact]
    public void Build_Returns_No_Open_Issue_Message_When_Record_Exists_But_No_Pending_Item()
    {
        var entryId = Guid.NewGuid();
        var corpus = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.YibfAnaBilgiEntry,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = entryId,
                BoardLabel = "YİBF Ana Bilgi",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ 1855397"
            }
        };

        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "642-25",
            NormalizedKey = "642-25",
            MatchType = ContextQueryMatchType.AdaParsel,
            IntentType = ContextQueryIntentType.GeneralStatus
        };

        var result = builder.Build(
            match,
            corpus,
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            Array.Empty<KarotEntry>(),
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            new[]
            {
                new YibfAnaBilgiEntry
                {
                    Id = entryId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    YibfNo = "1855397"
                }
            },
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Equal("642-25 için kayıt bulundu, ancak açık eksik veya bekleyen konu görünmüyor.", result.AnswerText);
        Assert.Empty(result.Sections);
    }

    [Fact]
    public void Build_Returns_Insights_And_Deduplicated_Sources_For_Open_Items()
    {
        var missingId = Guid.NewGuid();
        var karotId = Guid.NewGuid();
        var corpus = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.MissingProjectEntry,
                TargetTab = MainNavigationTab.EksikProje,
                ItemId = missingId,
                BoardLabel = "Eksik Proje",
                Title = "642-25",
                Summary = "STATİK PROJE",
                SearchText = "642-25 ALAADDİN BEYAZ STATİK PROJE"
            },
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = karotId,
                BoardLabel = "Karot / Bekleyen",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ SEKVAN"
            }
        };

        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "642-25",
            NormalizedKey = "642-25",
            MatchType = ContextQueryMatchType.AdaParsel,
            IntentType = ContextQueryIntentType.GeneralStatus
        };

        var result = builder.Build(
            match,
            corpus,
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            new[]
            {
                new MissingProjectEntry
                {
                    Id = missingId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    MissingProjectText = "STATİK PROJE"
                }
            },
            new[]
            {
                new KarotEntry
                {
                    Id = karotId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    Muteahhit = "SEKVAN",
                    KatBilgisi = "BODRUM",
                    Status = KarotStatus.KarotAlinacak
                }
            },
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Contains("Eksik Proje:", result.AnswerText);
        Assert.Contains("Karot:", result.AnswerText);
        Assert.Equal(2, result.Sections.Count);
        Assert.Equal("Eksik Proje", result.Sections[0].Title);
        Assert.Equal("Karot", result.Sections[1].Title);
        Assert.Equal(2, result.Sources.Count);
        Assert.All(result.Sources, source => Assert.Equal("DOĞRUDAN", source.MatchOriginLabel));
    }

    [Fact]
    public void Build_Marks_Source_As_Context_When_Only_Enriched_SearchText_Matches()
    {
        var taskId = Guid.NewGuid();
        var corpus = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.GeneralTask,
                TargetTab = MainNavigationTab.GenelIsTakibi,
                ItemId = taskId,
                BoardType = TaskBoardType.Genel,
                BoardLabel = "Genel İş Takibi / Genel İşler",
                Title = "ORSA evrak istenecek",
                Summary = "ORSA evrak istenecek",
                RawSearchText = "ORSA evrak istenecek",
                SearchText = "ORSA evrak istenecek 235-1 ORSA ENERJİ 111"
            }
        };

        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "235-1",
            NormalizedKey = "235-1",
            MatchType = ContextQueryMatchType.AdaParsel,
            IntentType = ContextQueryIntentType.GeneralStatus
        };

        var result = builder.Build(
            match,
            corpus,
            new[]
            {
                new TaskItem
                {
                    Id = taskId,
                    Title = "ORSA evrak istenecek",
                    BoardType = TaskBoardType.Genel
                }
            },
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            Array.Empty<KarotEntry>(),
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Single(result.Sources);
        Assert.Equal("BAĞLAM", result.Sources[0].MatchOriginLabel);
    }

    [Fact]
    public void Build_Returns_Completed_Karot_Result_For_Completed_Intent()
    {
        var karotId = Guid.NewGuid();
        var corpus = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = karotId,
                BoardLabel = "Karot",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ SEKVAN"
            }
        };

        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "642-25",
            NormalizedKey = "642-25",
            MatchType = ContextQueryMatchType.AdaParsel,
            IntentType = ContextQueryIntentType.CompletedOnly
        };

        var result = builder.Build(
            match,
            corpus,
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            new[]
            {
                new KarotEntry
                {
                    Id = karotId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    Muteahhit = "SEKVAN",
                    KatBilgisi = "BODRUM",
                    Status = KarotStatus.KarotAlindiOlumlu
                }
            },
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Contains("tamamlanan kayıt bulundu", result.AnswerText);
        Assert.Contains("KAROT OLUMLU", result.AnswerText);
        Assert.Single(result.Sections);
        Assert.Equal("Karot", result.Sections[0].Title);
        Assert.Single(result.Sources);
    }

    [Fact]
    public void Build_Returns_No_Pending_Message_For_Pending_Intent_When_Only_Completed_Record_Exists()
    {
        var karotId = Guid.NewGuid();
        var corpus = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = karotId,
                BoardLabel = "Karot",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ SEKVAN"
            }
        };

        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "642-25",
            NormalizedKey = "642-25",
            MatchType = ContextQueryMatchType.AdaParsel,
            IntentType = ContextQueryIntentType.PendingOnly
        };

        var result = builder.Build(
            match,
            corpus,
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            new[]
            {
                new KarotEntry
                {
                    Id = karotId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    Muteahhit = "SEKVAN",
                    KatBilgisi = "BODRUM",
                    Status = KarotStatus.KarotAlindiOlumlu
                }
            },
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Equal("642-25 için bekleyen konu görünmüyor.", result.AnswerText);
        Assert.Empty(result.Sections);
        Assert.Empty(result.Sources);
    }

    [Fact]
    public void Build_Matches_Muteahhit_Only_On_Relevant_Modules()
    {
        var karotId = Guid.NewGuid();
        var anaBilgiEventId = Guid.NewGuid();
        var anaBilgiEntryId = Guid.NewGuid();
        var corpus = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = karotId,
                BoardLabel = "Karot",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ SEKVAN"
            },
            new()
            {
                Kind = SearchResultKind.YibfAnaBilgiEvent,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = anaBilgiEventId,
                ParentItemId = anaBilgiEntryId,
                BoardLabel = "YİBF Ana Bilgi",
                Title = "642-25",
                Summary = "SEKVAN",
                SearchText = "642-25 ALAADDİN BEYAZ SEKVAN eksik evrak"
            }
        };

        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "SEKVAN",
            NormalizedKey = "sekvan",
            MatchType = ContextQueryMatchType.Muteahhit,
            IntentType = ContextQueryIntentType.GeneralStatus,
            PrimaryRole = ContextQueryRole.Muteahhit,
            AllowedRoles = [ContextQueryRole.Muteahhit, ContextQueryRole.YapiSahibi]
        };

        var result = builder.Build(
            match,
            corpus,
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            new[]
            {
                new KarotEntry
                {
                    Id = karotId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    Muteahhit = "SEKVAN",
                    KatBilgisi = "BODRUM",
                    Status = KarotStatus.KarotAlinacak
                }
            },
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            new[]
            {
                new YibfAnaBilgiEntry
                {
                    Id = anaBilgiEntryId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    Muteahhit = "SEKVAN",
                    YibfNo = "1855397"
                }
            },
            new[]
            {
                new YibfAnaBilgiEvent
                {
                    Id = anaBilgiEventId,
                    EntryId = anaBilgiEntryId,
                    Description = "Eksik evrak",
                    BackgroundColor = "#FFFF0000"
                }
            },
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Equal(2, result.Sections.Count);
        Assert.Equal("Karot", result.Sections[0].Title);
        Assert.Equal("YİBF Ana Bilgi", result.Sections[1].Title);
        Assert.Equal(1, result.Sections[0].SourceCount);
        Assert.Equal(1, result.Sections[1].SourceCount);
        Assert.Contains("[MÜTEAHHİT]", result.Sections[0].Items[0]);
        Assert.Contains("[MÜTEAHHİT]", result.Sections[1].Items[0]);
        Assert.Contains("[MÜTEAHHİT]", result.Sources[0].Summary + " " + result.Sources[1].Summary);
    }

    [Fact]
    public void Build_Expands_Muteahhit_Query_To_YapiSahibi_Based_Modules()
    {
        var missingId = Guid.NewGuid();
        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "SEKVAN",
            NormalizedKey = "sekvan",
            MatchType = ContextQueryMatchType.Muteahhit,
            IntentType = ContextQueryIntentType.GeneralStatus,
            PrimaryRole = ContextQueryRole.Muteahhit,
            AllowedRoles = [ContextQueryRole.Muteahhit, ContextQueryRole.YapiSahibi]
        };

        var result = builder.Build(
            match,
            new[]
            {
                new SearchResultItem
                {
                    Kind = SearchResultKind.MissingProjectEntry,
                    TargetTab = MainNavigationTab.EksikProje,
                    ItemId = missingId,
                    BoardLabel = "Eksik Proje",
                    Title = "642-25",
                    Summary = "SEKVAN",
                    SearchText = "642-25 SEKVAN STATİK PROJE"
                }
            },
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            new[]
            {
                new MissingProjectEntry
                {
                    Id = missingId,
                    AdaParsel = "642-25",
                    YapiSahibi = "SEKVAN",
                    MissingProjectText = "STATİK PROJE"
                }
            },
            Array.Empty<KarotEntry>(),
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Single(result.Sections);
        Assert.Equal("Eksik Proje", result.Sections[0].Title);
        Assert.Equal(1, result.Sections[0].SourceCount);
        Assert.Contains("[YAPI SAHİBİ]", result.Sections[0].Items[0]);
        Assert.Contains("[YAPI SAHİBİ]", result.Sources[0].Summary);
    }

    [Fact]
    public void Build_Does_Not_Expand_YapiSahibi_Query_Back_To_Muteahhit()
    {
        var karotId = Guid.NewGuid();
        var builder = new ContextInsightBuilder(new SearchService());
        var match = new ContextQueryMatch
        {
            MatchedKey = "SEKVAN",
            NormalizedKey = "sekvan",
            MatchType = ContextQueryMatchType.YapiSahibi,
            IntentType = ContextQueryIntentType.GeneralStatus,
            PrimaryRole = ContextQueryRole.YapiSahibi,
            AllowedRoles = [ContextQueryRole.YapiSahibi]
        };

        var result = builder.Build(
            match,
            new[]
            {
                new SearchResultItem
                {
                    Kind = SearchResultKind.KarotEntry,
                    TargetTab = MainNavigationTab.KarotTakibi,
                    ItemId = karotId,
                    BoardLabel = "Karot",
                    Title = "642-25",
                    Summary = "SEKVAN",
                    SearchText = "642-25 ALAADDİN BEYAZ SEKVAN"
                }
            },
            Array.Empty<TaskItem>(),
            Array.Empty<ActionEntry>(),
            Array.Empty<MissingProjectEntry>(),
            new[]
            {
                new KarotEntry
                {
                    Id = karotId,
                    AdaParsel = "642-25",
                    YapiSahibi = "ALAADDİN BEYAZ",
                    Muteahhit = "SEKVAN",
                    KatBilgisi = "BODRUM",
                    Status = KarotStatus.KarotAlinacak
                }
            },
            Array.Empty<TadilatEntry>(),
            Array.Empty<TadilatCellState>(),
            Array.Empty<YibfAnaBilgiEntry>(),
            Array.Empty<YibfAnaBilgiEvent>(),
            Array.Empty<YibfIsTakibiEntry>(),
            Array.Empty<YibfCellState>());

        Assert.Empty(result.Sections);
    }
}
