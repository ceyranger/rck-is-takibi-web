using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Services;

public static class WebViewSnapshotDerivedBuilder
{
    public static WebViewSnapshotDerived Build(
        IEnumerable<EksikIsGroupViewModel> tumEksiklerGroups,
        IEnumerable<YibfPendingGroupViewModel> projeOnayGroups,
        IEnumerable<WebViewPersonnelGorevRowDto> personnelRows,
        IEnumerable<AcilIsOzetItemViewModel> acilIsOzetItems)
        => new()
        {
            TumEksikler = tumEksiklerGroups.Select(MapTumEksiklerGroup).ToList(),
            ProjeOnayItems = projeOnayGroups.Select(MapProjeOnayGroup).ToList(),
            PersonnelGorevItems = personnelRows.ToList(),
            AcilIsOzetItems = acilIsOzetItems.Select(MapAcilIsOzetItem).ToList()
        };

    public static IReadOnlyList<EksikIsGroupViewModel> GetAllTumEksiklerGroups(TumEksiklerViewModel viewModel)
        => viewModel.GetAllGroupsSnapshot();

    private static WebViewTumEksiklerGroupDto MapTumEksiklerGroup(EksikIsGroupViewModel group)
        => new()
        {
            HeaderText = group.HeaderText,
            DetailText = group.DetailText,
            AdaParsel = group.AdaParsel,
            YapiSahibi = group.YapiSahibi,
            MatchStatus = group.MatchStatus.ToString(),
            EksikCount = group.EksikCount,
            CriticalCount = group.CriticalCount,
            Items = group.Items.Select(MapTumEksiklerItem).ToList()
        };

    private static WebViewTumEksiklerItemDto MapTumEksiklerItem(EksikItemViewModel item)
        => new()
        {
            SourceModule = item.SourceModule,
            FieldLabel = item.FieldLabel,
            Reason = item.Reason,
            CurrentValue = item.CurrentValue,
            NoteText = item.NoteText,
            SourceContext = item.SourceContext,
            AssignedPersonnelBadge = item.AssignedPersonnelBadge,
            Severity = item.Severity.ToString(),
            SeverityLabel = item.SeverityLabel,
            UpdatedAt = item.UpdatedAt
        };

    private static WebViewProjeOnayGroupDto MapProjeOnayGroup(YibfPendingGroupViewModel group)
        => new()
        {
            TitleText = group.TitleText,
            AdaParsel = group.Entry.AdaParsel,
            YapiSahibi = group.Entry.YapiSahibi,
            IsOverdue = group.IsOverdue,
            Events = group.AllEvents.Select(MapProjeOnayEvent).ToList()
        };

    private static WebViewProjeOnayEventDto MapProjeOnayEvent(YibfPendingItemViewModel item)
        => new()
        {
            StatusLabel = item.StatusLabel,
            FilterKey = item.FilterKey,
            Summary = item.Summary,
            EventDateText = item.EventDateText,
            DaysElapsedText = item.DaysElapsedText,
            IsOverdue = item.IsOverdue,
            CategoryColor = YibfAnaBilgiApprovalStatuses.GetDefaultColorForStatus(item.PendingEvent.ApprovalStatus)
        };

    public static IReadOnlyList<WebViewPersonnelGorevRowDto> MapPersonnelAssignments(
        IEnumerable<PersonnelAssignment> assignments,
        Func<Guid?, string?> getPersonnelName)
        => assignments
            .Where(a => a.Status == PersonnelAssignmentStatus.Open)
            .OrderBy(a => getPersonnelName(a.PersonnelId) ?? string.Empty)
            .ThenByDescending(a => a.AssignedAt)
            .Select(a => MapPersonnelAssignment(a, getPersonnelName(a.PersonnelId) ?? string.Empty))
            .ToList();

    private static WebViewPersonnelGorevRowDto MapPersonnelAssignment(PersonnelAssignment assignment, string personnelName)
        => new()
        {
            PersonnelName = string.IsNullOrWhiteSpace(personnelName) ? "Atanmamış" : personnelName,
            ModuleLabel = string.IsNullOrWhiteSpace(assignment.ModuleLabelSnapshot)
                ? IPersonnelAssignmentService.ModuleLabel(assignment.SourceModule)
                : assignment.ModuleLabelSnapshot,
            Summary = assignment.SummarySnapshot,
            FieldLabel = assignment.FieldLabelSnapshot,
            ProjectIdentity = assignment.ProjectIdentitySnapshot,
            PriorityLabel = IPersonnelAssignmentService.PriorityLabel(assignment.PrioritySnapshot),
            StatusLabel = assignment.Status == PersonnelAssignmentStatus.Completed ? "Tamamlandı" : "Açık",
            AssignedAtText = assignment.AssignedAt.ToString("g"),
            IsOpen = assignment.Status == PersonnelAssignmentStatus.Open
        };

    private static WebViewAcilIsOzetItemDto MapAcilIsOzetItem(AcilIsOzetItemViewModel item)
        => new()
        {
            Category = item.Category,
            PriorityLabel = item.PriorityLabel,
            PriorityRank = item.PriorityRank,
            Summary = item.Summary
        };
}
