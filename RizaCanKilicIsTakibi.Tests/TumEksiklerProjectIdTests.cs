using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class TumEksiklerProjectIdTests
{
    [Fact]
    public void RefreshFrom_GroupsKarotByProjectId_BeforeTextFallback()
    {
        var workGroupId = Guid.NewGuid();
        var anaBilgi = new List<YibfAnaBilgiEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                WorkGroupId = workGroupId,
                WorkIdentityId = workGroupId,
                AdaParsel = "10-1",
                YapiSahibi = "Ana",
                YibfNo = "1"
            }
        };
        var karot = new List<KarotEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = workGroupId,
                AdaParsel = "tamamen-farklı",
                YapiSahibi = "eşleşmez",
                YibfNo = string.Empty,
                Status = KarotStatus.KarotAlinacak
            }
        };

        var vm = new TumEksiklerViewModel();
        vm.RefreshFrom(anaBilgi, [], [], [], [], [], [], karot);

        var matched = Assert.Single(vm.Groups, group => group.MatchStatus == EksikMatchStatus.Matched);
        Assert.Equal(workGroupId, matched.WorkGroupId);
        Assert.Contains(matched.Items, item => item.SourceModule == "Karot");
    }
}
