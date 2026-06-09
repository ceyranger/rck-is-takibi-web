using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Tests;

public class ContextQueryExplanationBuilderTests
{
    [Fact]
    public void Build_Returns_Token_Explanation_For_Single_Name_Token()
    {
        var explanation = ContextQueryExplanationBuilder.Build(new ContextQueryMatch
        {
            MatchedKey = "ORSA",
            MatchType = ContextQueryMatchType.YapiSahibi
        });

        Assert.Contains("isim tokenı", explanation);
    }

    [Fact]
    public void Build_Returns_Contractor_Explanation()
    {
        var explanation = ContextQueryExplanationBuilder.Build(new ContextQueryMatch
        {
            MatchedKey = "SEKVAN",
            MatchType = ContextQueryMatchType.Muteahhit
        });

        Assert.Contains("müteahhit", explanation);
        Assert.Contains("yapı sahibi", explanation);
    }

    [Fact]
    public void Build_Returns_Parcel_Explanation()
    {
        var explanation = ContextQueryExplanationBuilder.Build(new ContextQueryMatch
        {
            MatchedKey = "235-1",
            MatchType = ContextQueryMatchType.AdaParsel
        });

        Assert.Contains("ada parsel", explanation);
    }
}
