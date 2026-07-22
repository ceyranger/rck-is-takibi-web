using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Helpers;

public static class ProjectCatalogKindLabels
{
    public static string ToLabel(ProjectCatalogKind kind)
        => kind switch
        {
            ProjectCatalogKind.Normal => "Normal",
            ProjectCatalogKind.Istinat => "İstinat",
            ProjectCatalogKind.Special => "Özel iş",
            _ => "Bilinmeyen"
        };

    public static IReadOnlyList<ProjectCatalogKind> AllKinds { get; } =
        [ProjectCatalogKind.Normal, ProjectCatalogKind.Istinat, ProjectCatalogKind.Special];

    public static IReadOnlyList<ProjectCatalogKindChoice> AllChoices { get; } =
        AllKinds.Select(kind => new ProjectCatalogKindChoice(kind)).ToList();
}

public sealed record ProjectCatalogKindChoice(ProjectCatalogKind Kind)
{
    public string Label => ProjectCatalogKindLabels.ToLabel(Kind);

    public override string ToString() => Label;
}
