using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public static class MissingProjectMediumLabelProvider
{
    public static string GetLabel(MissingProjectMedium medium)
    {
        return medium switch
        {
            MissingProjectMedium.Dijital => "Dijital",
            MissingProjectMedium.Fiziki => "Fiziksel",
            MissingProjectMedium.FizikiVeDijital => "Fiziksel + Dijital",
            _ => medium.ToString()
        };
    }
}

