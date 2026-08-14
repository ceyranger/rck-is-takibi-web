using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public enum PersonnelCellScopeChoice
{
    ThisCell = 0,
    AllRedYellowOnRow = 1,
    Cancel = 2
}

public interface IPersonnelAssignmentService
{
    event EventHandler? Changed;

    IReadOnlyList<Personnel> GetPersonnel();
    IReadOnlyList<PersonnelAssignment> GetAssignments();
    void Reload();
    void ReplaceAll(IEnumerable<Personnel> personnel, IEnumerable<PersonnelAssignment> assignments);

    Task<Personnel> AddPersonnelAsync(string name, CancellationToken cancellationToken = default);
    Task RenamePersonnelAsync(Guid id, string name, CancellationToken cancellationToken = default);
    Task DeletePersonnelAsync(Guid id, CancellationToken cancellationToken = default);

    Task AssignAsync(PersonnelAssignment draft, CancellationToken cancellationToken = default);
    Task AssignManyAsync(IEnumerable<PersonnelAssignment> drafts, CancellationToken cancellationToken = default);
    Task RemoveAssignmentAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId, string? columnKey = null, CancellationToken cancellationToken = default);
    Task RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId, CancellationToken cancellationToken = default);
    Task SetStatusAsync(Guid assignmentId, PersonnelAssignmentStatus status, CancellationToken cancellationToken = default);

    PersonnelAssignment? Find(PersonnelAssignmentSourceModule module, Guid sourceEntryId, string? columnKey = null);
    string GetBadgeText(PersonnelAssignmentSourceModule module, Guid sourceEntryId);
    string? GetPersonnelName(Guid? personnelId);

    void SyncCompletionFromSources(
        IEnumerable<TaskItem> tasks,
        IEnumerable<ActionEntry> actions,
        IEnumerable<MissingProjectEntry> missingProjects,
        IEnumerable<KarotEntry> karotEntries,
        IEnumerable<TadilatEntry> tadilatEntries,
        IEnumerable<TadilatCellState> tadilatCellStates,
        IEnumerable<YibfAnaBilgiEvent> yibfEvents,
        IEnumerable<YibfIsTakibiEntry> yibfEntries,
        IEnumerable<YibfCellState> yibfCellStates);

    static string ModuleLabel(PersonnelAssignmentSourceModule module) => module switch
    {
        PersonnelAssignmentSourceModule.GenelTask => "Genel İş Takibi",
        PersonnelAssignmentSourceModule.AcilTask => "Acil İş Takibi",
        PersonnelAssignmentSourceModule.Action => "Aksiyon",
        PersonnelAssignmentSourceModule.MissingProject => "Eksik Proje",
        PersonnelAssignmentSourceModule.Karot => "Karot Takibi",
        PersonnelAssignmentSourceModule.Tadilat => "Tadilat Takibi",
        PersonnelAssignmentSourceModule.YibfAnaBilgiEvent => "Proje Takibi",
        PersonnelAssignmentSourceModule.YibfIsTakibi => "YİBF İş Takibi",
        _ => module.ToString()
    };

    static string PriorityLabel(PersonnelAssignmentPriority priority) => priority switch
    {
        PersonnelAssignmentPriority.Critical => "Kritik",
        PersonnelAssignmentPriority.Warning => "Uyarı",
        PersonnelAssignmentPriority.Urgent => "Acil",
        _ => string.Empty
    };

    static bool IsAssignableKarotStatus(KarotStatus status)
        => status is KarotStatus.KarotAlinacak
            or KarotStatus.KarotAlindiSonucBekleniyor
            or KarotStatus.KarotAlindiOlumsuz;
}
