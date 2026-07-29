using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class ContextQueryServiceTests
{
    [Fact]
    public void ExtractMatch_Detects_AdaParsel_And_Avoids_FalsePositive()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.MissingProjectEntry,
                TargetTab = MainNavigationTab.EksikProje,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Eksik Proje",
                Title = "1642-250",
                Summary = "Yanlış pozitif olmamalı",
                SearchText = "1642-250 yanlış pozitif olmamalı"
            },
            new()
            {
                Kind = SearchResultKind.MissingProjectEntry,
                TargetTab = MainNavigationTab.EksikProje,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Eksik Proje",
                Title = "642-25",
                Summary = "Doğru kayıt",
                SearchText = "642-25 doğru kayıt"
            }
        };

        var match = service.ExtractMatch("642-25 ada parselde ne eksik var", items);

        Assert.True(match.HasMatch);
        Assert.Equal(ContextQueryMatchType.AdaParsel, match.MatchType);
        Assert.Equal(ContextQueryIntentType.OpenIssues, match.IntentType);
        Assert.Equal("642-25", match.MatchedKey);
    }

    [Fact]
    public void ExtractMatch_Detects_YibfNo()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.YibfAnaBilgiEntry,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Proje Takibi",
                Title = "1855397",
                Summary = "YİBF",
                SearchText = "1855397 ORSA ENERJI"
            }
        };

        var match = service.ExtractMatch("1855397", items);

        Assert.Equal(ContextQueryMatchType.YibfNo, match.MatchType);
        Assert.Equal(ContextQueryIntentType.GeneralStatus, match.IntentType);
        Assert.Equal("1855397", match.MatchedKey);
    }

    [Fact]
    public void ExtractMatch_Detects_Name_Query()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.YibfAnaBilgiEntry,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Proje Takibi",
                Title = "235-1 ORSA ENERJİ",
                Summary = "SEKVAN İNŞAAT",
                SearchText = "235-1 ORSA ENERJİ SEKVAN İNŞAAT"
            }
        };

        var match = service.ExtractMatch("Orsa Enerji", items);

        Assert.Equal(ContextQueryMatchType.YapiSahibi, match.MatchType);
        Assert.Equal(ContextQueryIntentType.GeneralStatus, match.IntentType);
        Assert.Null(match.PrimaryRole);
        Assert.Empty(match.AllowedRoles);
        Assert.Equal("Orsa Enerji", match.MatchedKey);
    }

    [Fact]
    public void ExtractMatch_Detects_Muteahhit_Query()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Karot",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ SEKVAN İNŞAAT"
            }
        };

        var match = service.ExtractMatch("SEKVAN müteahhit", items);

        Assert.Equal(ContextQueryMatchType.Muteahhit, match.MatchType);
        Assert.Equal(ContextQueryIntentType.GeneralStatus, match.IntentType);
        Assert.Equal(ContextQueryRole.Muteahhit, match.PrimaryRole);
        Assert.Equal(2, match.AllowedRoles.Count);
        Assert.Contains(ContextQueryRole.Muteahhit, match.AllowedRoles);
        Assert.Contains(ContextQueryRole.YapiSahibi, match.AllowedRoles);
        Assert.Equal("SEKVAN", match.MatchedKey);
    }

    [Fact]
    public void ExtractMatch_Detects_YapiSahibi_Query_With_Single_Allowed_Role()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.MissingProjectEntry,
                TargetTab = MainNavigationTab.EksikProje,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Eksik Proje",
                Title = "642-25",
                Summary = "ALAADDİN BEYAZ",
                SearchText = "642-25 ALAADDİN BEYAZ STATİK PROJE"
            }
        };

        var match = service.ExtractMatch("ALAADDİN BEYAZ yapı sahibi", items);

        Assert.Equal(ContextQueryMatchType.YapiSahibi, match.MatchType);
        Assert.Equal(ContextQueryRole.YapiSahibi, match.PrimaryRole);
        Assert.Single(match.AllowedRoles);
        Assert.Contains(ContextQueryRole.YapiSahibi, match.AllowedRoles);
    }

    [Fact]
    public void ExtractMatch_Detects_Completed_Intent()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Karot",
                Title = "642-25",
                Summary = "Tamamlandi",
                SearchText = "642-25 ALAADDIN BEYAZ"
            }
        };

        var match = service.ExtractMatch("642-25 tamamlanan var mı", items);

        Assert.Equal(ContextQueryIntentType.CompletedOnly, match.IntentType);
        Assert.Equal(ContextQueryMatchType.AdaParsel, match.MatchType);
    }

    [Fact]
    public void ExtractMatch_Detects_Pending_Intent()
    {
        var service = new ContextQueryService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.KarotEntry,
                TargetTab = MainNavigationTab.KarotTakibi,
                ItemId = Guid.NewGuid(),
                BoardLabel = "Karot",
                Title = "642-25",
                Summary = "Bekleyen",
                SearchText = "642-25 ALAADDIN BEYAZ"
            }
        };

        var match = service.ExtractMatch("642-25 ne bekliyor", items);

        Assert.Equal(ContextQueryIntentType.PendingOnly, match.IntentType);
        Assert.Equal(ContextQueryMatchType.AdaParsel, match.MatchType);
    }

    [Fact]
    public void ExtractMatch_Ignores_Short_Noise_And_FallsBack_Empty_When_Needed()
    {
        var service = new ContextQueryService();

        var match = service.ExtractMatch("ne var", Array.Empty<SearchResultItem>());

        Assert.False(match.HasMatch);
    }
}
