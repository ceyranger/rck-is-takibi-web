using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public class SearchServiceTests
{
    [Fact]
    public void SearchAll_Finds_Title_Description_And_Notes()
    {
        var service = new SearchService();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.GeneralTask,
                TargetTab = MainNavigationTab.GenelIsTakibi,
                ItemId = Guid.NewGuid(),
                BoardType = TaskBoardType.Genel,
                BoardLabel = "Genel İş Takibi / Genel İşler",
                Title = "Elektrik takibi",
                Summary = "Ana pano kontrol",
                SearchText = "Elektrik takibi Ana pano kontrol"
            },
            new()
            {
                Kind = SearchResultKind.GeneralTask,
                TargetTab = MainNavigationTab.GenelIsTakibi,
                ItemId = Guid.NewGuid(),
                BoardType = TaskBoardType.Acil,
                BoardLabel = "Genel İş Takibi / Acil İşler",
                Title = "Toplantı",
                Summary = "Sözleşme imzası",
                SearchText = "Toplantı Sözleşme imzası"
            }
        };

        var byTitle = service.SearchAll(items, "elektrik");
        var byNote = service.SearchAll(items, "imza");

        Assert.Single(byTitle);
        Assert.Single(byNote);
        Assert.Equal(TaskBoardType.Acil, byNote[0].BoardType);
    }

    [Fact]
    public void SearchAll_Prioritizes_Title_Matches_And_Applies_Scope()
    {
        var service = new SearchService();
        var exactId = Guid.NewGuid();
        var items = new List<SearchResultItem>
        {
            new()
            {
                Kind = SearchResultKind.YibfIsTakibiEntry,
                TargetTab = MainNavigationTab.YibfIsTakibi,
                ItemId = exactId,
                BoardLabel = "YİBF İş Takibi",
                Title = "ŞEVKET İŞİ",
                Summary = "İlk sonuç olmalı",
                SearchText = "ŞEVKET İŞİ İlk sonuç olmalı"
            },
            new()
            {
                Kind = SearchResultKind.YibfAnaBilgiEntry,
                TargetTab = MainNavigationTab.YibfAnaBilgi,
                ItemId = Guid.NewGuid(),
                BoardLabel = "YİBF Ana Bilgi",
                Title = "Başka kayıt",
                Summary = "ŞEVKET içinde geçiyor",
                SearchText = "Başka kayıt ŞEVKET içinde geçiyor"
            }
        };

        var filtered = service.SearchAll(items, "şevket", SearchScope.YibfIsTakibi);

        Assert.Single(filtered);
        Assert.Equal(exactId, filtered[0].ItemId);
    }
}
