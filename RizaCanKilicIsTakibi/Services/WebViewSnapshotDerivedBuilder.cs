using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Services;

public static class WebViewSnapshotDerivedBuilder
{
    public static WebViewSnapshotDerived Build(
        IEnumerable<EksikIsGroupViewModel> tumEksiklerGroups,
        IEnumerable<YibfPendingGroupViewModel> projeOnayGroups,
        IEnumerable<PersonnelGorevRowViewModel> personnelRows)
        => new()
        {
            TumEksikler = tumEksiklerGroups.Select(MapTumEksiklerGroup).ToList(),
            ProjeOnayItems = projeOnayGroups.Select(MapProjeOnayGroup).ToList(),
            PersonnelGorevItems = personnelRows.Select(MapPersonnelRow).ToList()
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

    private static WebViewPersonnelGorevRowDto MapPersonnelRow(PersonnelGorevRowViewModel row)
        => new()
        {
            PersonnelName = row.PersonnelName,
            ModuleLabel = row.ModuleLabel,
            Summary = row.Summary,
            FieldLabel = row.FieldLabel,
            ProjectIdentity = row.ProjectIdentity,
            PriorityLabel = row.PriorityLabel,
            StatusLabel = row.StatusLabel,
            AssignedAtText = row.AssignedAtText,
            IsOpen = row.IsOpen
        };
}
