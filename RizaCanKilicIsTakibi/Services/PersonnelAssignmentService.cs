using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class PersonnelAssignmentService : IPersonnelAssignmentService
{
    private readonly IPersonnelRepository _repository;
    private readonly object _sync = new();
    private List<Personnel> _personnel = [];
    private List<PersonnelAssignment> _assignments = [];

    public PersonnelAssignmentService(IPersonnelRepository repository)
    {
        _repository = repository;
        Reload();
    }

    public event EventHandler? Changed;

    public IReadOnlyList<Personnel> GetPersonnel()
    {
        lock (_sync)
        {
            return _personnel.Select(p => p.Clone()).ToList();
        }
    }

    public IReadOnlyList<PersonnelAssignment> GetAssignments()
    {
        lock (_sync)
        {
            return _assignments.Select(a => a.Clone()).ToList();
        }
    }

    public void Reload()
    {
        lock (_sync)
        {
            _personnel = _repository.GetAllPersonnel().Select(p => p.Clone()).ToList();
            _assignments = _repository.GetAllAssignments().Select(a => a.Clone()).ToList();
        }

        RaiseChanged();
    }

    public void ReplaceAll(IEnumerable<Personnel> personnel, IEnumerable<PersonnelAssignment> assignments)
    {
        var people = personnel.Select(p => p.Clone()).ToList();
        var list = assignments.Select(a => a.Clone()).ToList();
        _repository.ReplaceAll(people, list);
        lock (_sync)
        {
            _personnel = people;
            _assignments = list;
        }

        RaiseChanged();
    }

    public async Task<Personnel> AddPersonnelAsync(string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Personel adı boş olamaz.", nameof(name));
        }

        int sortOrder;
        lock (_sync)
        {
            sortOrder = _personnel.Count == 0 ? 0 : _personnel.Max(p => p.SortOrder) + 1;
        }

        var person = new Personnel
        {
            Id = Guid.NewGuid(),
            Name = trimmed,
            SortOrder = sortOrder,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        await _repository.SavePersonnelAsync(person, cancellationToken);
        lock (_sync)
        {
            _personnel.Add(person.Clone());
        }

        RaiseChanged();
        return person;
    }

    public async Task RenamePersonnelAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Personel adı boş olamaz.", nameof(name));
        }

        Personnel? person;
        lock (_sync)
        {
            person = _personnel.FirstOrDefault(p => p.Id == id);
            if (person is null)
            {
                return;
            }

            person.Name = trimmed;
            person.UpdatedAt = DateTime.Now;
        }

        await _repository.SavePersonnelAsync(person.Clone(), cancellationToken);
        RaiseChanged();
    }

    public async Task DeletePersonnelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeletePersonnelAsync(id, cancellationToken);
        lock (_sync)
        {
            _personnel.RemoveAll(p => p.Id == id);
            foreach (var assignment in _assignments.Where(a => a.PersonnelId == id))
            {
                assignment.PersonnelId = null;
            }
        }

        RaiseChanged();
    }

    public async Task AssignAsync(PersonnelAssignment draft, CancellationToken cancellationToken = default)
    {
        draft.ModuleLabelSnapshot = string.IsNullOrWhiteSpace(draft.ModuleLabelSnapshot)
            ? IPersonnelAssignmentService.ModuleLabel(draft.SourceModule)
            : draft.ModuleLabelSnapshot;
        draft.AssignedAt = draft.AssignedAt == default ? DateTime.Now : draft.AssignedAt;
        if (draft.Status == PersonnelAssignmentStatus.Completed && draft.CompletedAt is null)
        {
            draft.CompletedAt = DateTime.Now;
        }

        await _repository.UpsertAssignmentAsync(draft, cancellationToken);

        lock (_sync)
        {
            var key = MakeKey(draft.SourceModule, draft.SourceEntryId, draft.SourceColumnKey);
            var existing = _assignments.FirstOrDefault(a => MakeKey(a.SourceModule, a.SourceEntryId, a.SourceColumnKey) == key);
            if (existing is null)
            {
                _assignments.Add(draft.Clone());
            }
            else
            {
                var index = _assignments.IndexOf(existing);
                draft.Id = existing.Id;
                _assignments[index] = draft.Clone();
            }
        }

        RaiseChanged();
    }

    public async Task AssignManyAsync(IEnumerable<PersonnelAssignment> drafts, CancellationToken cancellationToken = default)
    {
        foreach (var draft in drafts)
        {
            await AssignAsync(draft, cancellationToken);
        }
    }

    public async Task UpdateAssignmentAsync(PersonnelAssignment updated, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updated);

        PersonnelAssignment? existing;
        lock (_sync)
        {
            existing = _assignments.FirstOrDefault(a => a.Id == updated.Id);
            if (existing is null)
            {
                return;
            }
        }

        var next = existing.Clone();
        next.PersonnelId = updated.PersonnelId;
        next.Status = updated.Status;
        next.CompletedAt = updated.Status == PersonnelAssignmentStatus.Completed
            ? (updated.CompletedAt ?? existing.CompletedAt ?? DateTime.Now)
            : null;
        next.PrioritySnapshot = updated.PrioritySnapshot;
        next.FieldLabelSnapshot = updated.FieldLabelSnapshot?.Trim() ?? string.Empty;
        next.SummarySnapshot = updated.SummarySnapshot?.Trim() ?? string.Empty;
        next.ProjectIdentitySnapshot = updated.ProjectIdentitySnapshot?.Trim() ?? string.Empty;
        next.ModuleLabelSnapshot = string.IsNullOrWhiteSpace(updated.ModuleLabelSnapshot)
            ? IPersonnelAssignmentService.ModuleLabel(next.SourceModule)
            : updated.ModuleLabelSnapshot.Trim();
        next.AssignedAt = existing.AssignedAt;

        await _repository.UpsertAssignmentAsync(next, cancellationToken);

        lock (_sync)
        {
            var index = _assignments.FindIndex(a => a.Id == next.Id);
            if (index >= 0)
            {
                _assignments[index] = next.Clone();
            }
        }

        RaiseChanged();
    }

    public async Task RemoveAssignmentAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId, string? columnKey = null, CancellationToken cancellationToken = default)
    {
        PersonnelAssignment? existing;
        lock (_sync)
        {
            existing = FindLocked(module, sourceEntryId, columnKey);
        }

        if (existing is null)
        {
            return;
        }

        await _repository.DeleteAssignmentAsync(existing.Id, cancellationToken);
        lock (_sync)
        {
            _assignments.RemoveAll(a => a.Id == existing.Id);
        }

        RaiseChanged();
    }

    public async Task RemoveAssignmentsForSourceAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAssignmentsForSourceAsync(module, sourceEntryId, cancellationToken);
        lock (_sync)
        {
            _assignments.RemoveAll(a => a.SourceModule == module && a.SourceEntryId == sourceEntryId);
        }

        RaiseChanged();
    }

    public async Task SetStatusAsync(Guid assignmentId, PersonnelAssignmentStatus status, CancellationToken cancellationToken = default)
    {
        PersonnelAssignment? assignment;
        lock (_sync)
        {
            assignment = _assignments.FirstOrDefault(a => a.Id == assignmentId);
            if (assignment is null)
            {
                return;
            }

            assignment.Status = status;
            assignment.CompletedAt = status == PersonnelAssignmentStatus.Completed ? DateTime.Now : null;
        }

        await _repository.UpsertAssignmentAsync(assignment.Clone(), cancellationToken);
        RaiseChanged();
    }

    public PersonnelAssignment? Find(PersonnelAssignmentSourceModule module, Guid sourceEntryId, string? columnKey = null)
    {
        lock (_sync)
        {
            return FindLocked(module, sourceEntryId, columnKey)?.Clone();
        }
    }

    public string GetBadgeText(PersonnelAssignmentSourceModule module, Guid sourceEntryId)
    {
        lock (_sync)
        {
            var matches = _assignments
                .Where(a => a.SourceModule == module && a.SourceEntryId == sourceEntryId && a.Status == PersonnelAssignmentStatus.Open)
                .ToList();
            if (matches.Count == 0)
            {
                return string.Empty;
            }

            var names = matches
                .Select(a => GetPersonnelNameLocked(a.PersonnelId) ?? "Atanmamış")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (names.Count == 0)
            {
                return string.Empty;
            }

            if (names.Count == 1)
            {
                var extra = matches.Count > 1 ? $" +{matches.Count - 1}" : string.Empty;
                return names[0] + extra;
            }

            return $"{names[0]} +{names.Count - 1}";
        }
    }

    public string? GetPersonnelName(Guid? personnelId)
    {
        lock (_sync)
        {
            return GetPersonnelNameLocked(personnelId);
        }
    }

    public void SyncCompletionFromSources(
        IEnumerable<TaskItem> tasks,
        IEnumerable<ActionEntry> actions,
        IEnumerable<MissingProjectEntry> missingProjects,
        IEnumerable<KarotEntry> karotEntries,
        IEnumerable<TadilatEntry> tadilatEntries,
        IEnumerable<TadilatCellState> tadilatCellStates,
        IEnumerable<YibfAnaBilgiEvent> yibfEvents,
        IEnumerable<YibfIsTakibiEntry> yibfEntries,
        IEnumerable<YibfCellState> yibfCellStates)
    {
        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var actionIds = actions.Select(a => a.Id).ToHashSet();
        var missingIds = missingProjects.Select(m => m.Id).ToHashSet();
        var karotById = karotEntries.ToDictionary(e => e.Id);
        var tadilatById = tadilatEntries.ToDictionary(e => e.Id);
        var tadilatCells = tadilatCellStates.ToLookup(c => c.EntryId);
        var yibfById = yibfEntries.ToDictionary(e => e.Id);
        var yibfCells = yibfCellStates.ToLookup(c => c.EntryId);
        var eventsById = yibfEvents.ToDictionary(e => e.Id);

        List<PersonnelAssignment> toUpdate = [];
        List<Guid> toDelete = [];
        lock (_sync)
        {
            foreach (var assignment in _assignments.ToList())
            {
                if (IsSourceMissing(assignment, taskIds, actionIds, missingIds, karotById, tadilatById, yibfById, eventsById))
                {
                    toDelete.Add(assignment.Id);
                    _assignments.RemoveAll(a => a.Id == assignment.Id);
                    continue;
                }

                if (assignment.Status != PersonnelAssignmentStatus.Open)
                {
                    continue;
                }

                if (!ShouldAutoComplete(assignment, karotById, tadilatById, tadilatCells, yibfById, yibfCells, eventsById))
                {
                    continue;
                }

                assignment.Status = PersonnelAssignmentStatus.Completed;
                assignment.CompletedAt = DateTime.Now;
                toUpdate.Add(assignment.Clone());
            }
        }

        if (toDelete.Count == 0 && toUpdate.Count == 0)
        {
            return;
        }

        foreach (var id in toDelete)
        {
            _repository.DeleteAssignmentAsync(id).GetAwaiter().GetResult();
        }

        foreach (var item in toUpdate)
        {
            _repository.UpsertAssignmentAsync(item).GetAwaiter().GetResult();
        }

        RaiseChanged();
    }

    private static bool IsSourceMissing(
        PersonnelAssignment assignment,
        IReadOnlySet<Guid> taskIds,
        IReadOnlySet<Guid> actionIds,
        IReadOnlySet<Guid> missingIds,
        IReadOnlyDictionary<Guid, KarotEntry> karotById,
        IReadOnlyDictionary<Guid, TadilatEntry> tadilatById,
        IReadOnlyDictionary<Guid, YibfIsTakibiEntry> yibfById,
        IReadOnlyDictionary<Guid, YibfAnaBilgiEvent> eventsById)
        => assignment.SourceModule switch
        {
            PersonnelAssignmentSourceModule.GenelTask or PersonnelAssignmentSourceModule.AcilTask
                => !taskIds.Contains(assignment.SourceEntryId),
            PersonnelAssignmentSourceModule.Action
                => !actionIds.Contains(assignment.SourceEntryId),
            PersonnelAssignmentSourceModule.MissingProject
                => !missingIds.Contains(assignment.SourceEntryId),
            PersonnelAssignmentSourceModule.Karot
                => !karotById.ContainsKey(assignment.SourceEntryId),
            PersonnelAssignmentSourceModule.Tadilat
                => !tadilatById.ContainsKey(assignment.SourceEntryId),
            PersonnelAssignmentSourceModule.YibfIsTakibi
                => !yibfById.ContainsKey(assignment.SourceEntryId),
            PersonnelAssignmentSourceModule.YibfAnaBilgiEvent
                => !eventsById.ContainsKey(assignment.SourceEntryId),
            _ => false
        };

    private static bool ShouldAutoComplete(
        PersonnelAssignment assignment,
        IReadOnlyDictionary<Guid, KarotEntry> karotById,
        IReadOnlyDictionary<Guid, TadilatEntry> tadilatById,
        ILookup<Guid, TadilatCellState> tadilatCells,
        IReadOnlyDictionary<Guid, YibfIsTakibiEntry> yibfById,
        ILookup<Guid, YibfCellState> yibfCells,
        IReadOnlyDictionary<Guid, YibfAnaBilgiEvent> eventsById)
    {
        switch (assignment.SourceModule)
        {
            case PersonnelAssignmentSourceModule.Karot:
                if (!karotById.TryGetValue(assignment.SourceEntryId, out var karot))
                {
                    return false;
                }

                return !IPersonnelAssignmentService.IsAssignableKarotStatus(karot.Status);

            case PersonnelAssignmentSourceModule.YibfIsTakibi:
                if (!yibfById.TryGetValue(assignment.SourceEntryId, out var yibf))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(assignment.SourceColumnKey))
                {
                    return !IsYibfCellPending(yibf, assignment.SourceColumnKey, yibfCells[assignment.SourceEntryId]);
                }

                return !YibfHasAnyPending(yibf, yibfCells[assignment.SourceEntryId]);

            case PersonnelAssignmentSourceModule.Tadilat:
                if (!tadilatById.TryGetValue(assignment.SourceEntryId, out var tadilat))
                {
                    return false;
                }

                if (tadilat.SubTab != TadilatSubTab.Aktif)
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(assignment.SourceColumnKey))
                {
                    return !IsTadilatCellPending(tadilat, assignment.SourceColumnKey, tadilatCells[assignment.SourceEntryId]);
                }

                return !TadilatHasAnyPending(tadilat, tadilatCells[assignment.SourceEntryId]);

            case PersonnelAssignmentSourceModule.YibfAnaBilgiEvent:
                if (!eventsById.TryGetValue(assignment.SourceEntryId, out var evt))
                {
                    return false;
                }

                if (YibfAnaBilgiApprovalStatuses.IsApproved(evt.ApprovalStatus) || YibfAnaBilgiApprovalStatuses.IsPassive(evt.ApprovalStatus))
                {
                    return true;
                }

                return !PersonnelPendingColorHelper.IsPendingColor(evt.BackgroundColor);

            default:
                return false;
        }
    }

    private static bool YibfHasAnyPending(YibfIsTakibiEntry entry, IEnumerable<YibfCellState> cells)
    {
        foreach (var key in GetYibfChecklistKeys())
        {
            if (IsYibfCellPending(entry, key, cells))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TadilatHasAnyPending(TadilatEntry entry, IEnumerable<TadilatCellState> cells)
    {
        foreach (var key in GetTadilatChecklistKeys())
        {
            if (IsTadilatCellPending(entry, key, cells))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsYibfCellPending(YibfIsTakibiEntry entry, string columnKey, IEnumerable<YibfCellState> cells)
    {
        var color = cells.FirstOrDefault(c => string.Equals(c.ColumnKey, columnKey, StringComparison.Ordinal))?.BackgroundColor;
        if (PersonnelPendingColorHelper.IsPendingColor(color))
        {
            return true;
        }

        var value = GetYibfFieldValue(entry, columnKey);
        return string.IsNullOrWhiteSpace(value) && IsYibfRequired(columnKey);
    }

    private static bool IsTadilatCellPending(TadilatEntry entry, string columnKey, IEnumerable<TadilatCellState> cells)
    {
        var color = cells.FirstOrDefault(c => string.Equals(c.ColumnKey, columnKey, StringComparison.Ordinal))?.BackgroundColor;
        if (PersonnelPendingColorHelper.IsPendingColor(color))
        {
            return true;
        }

        var value = GetTadilatFieldValue(entry, columnKey);
        return string.IsNullOrWhiteSpace(value) && IsTadilatRequired(columnKey);
    }

    private static bool IsYibfRequired(string columnKey)
        => GetYibfChecklistKeys().Contains(columnKey, StringComparer.Ordinal);

    private static bool IsTadilatRequired(string columnKey)
        => GetTadilatChecklistKeys().Contains(columnKey, StringComparer.Ordinal);

    private static IEnumerable<string> GetYibfChecklistKeys() =>
    [
        YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi,
        YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi,
        YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi,
        YibfIsTakibiColumnKeys.EvraklarTamMi,
        YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi,
        YibfIsTakibiColumnKeys.DekontAlindiMi,
        YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi,
        YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi,
        YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi,
        YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi,
        YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi,
        YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi
    ];

    private static IEnumerable<string> GetTadilatChecklistKeys() =>
    [
        TadilatColumnKeys.DigitalReceived,
        TadilatColumnKeys.InspectorApproved,
        TadilatColumnKeys.OutputAndReportArrived,
        TadilatColumnKeys.OfficialLetterSubmitted,
        TadilatColumnKeys.ArchivedFromMunicipality
    ];

    public static string GetYibfFieldLabel(string columnKey) => columnKey switch
    {
        YibfIsTakibiColumnKeys.JobName => "İşin ismi",
        YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => "Müellif bilgileri geldi mi?",
        YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => "Denetçi atamaları yapıldı mı?",
        YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => "Tüm projelerin dijitali var mı?",
        YibfIsTakibiColumnKeys.EvraklarTamMi => "Evraklar tam mı?",
        YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => "YİBF sözleşme/taahhütname hazırlandı mı?",
        YibfIsTakibiColumnKeys.DekontAlindiMi => "Dekont alındı mı?",
        YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => "Ruhsat başvurusu yapıldı mı?",
        YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => "Ruhsat nüshası alındı mı?",
        YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => "İşyeri teslim tutanağı hazırlandı mı?",
        YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => "İSG yazısı hazırlandı mı?",
        YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => "Sağlık güvenlik planı geldi mi?",
        YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => "Temel topraklama tutanağı hazırlandı mı?",
        _ => columnKey
    };

    public static string GetTadilatFieldLabel(string columnKey) => columnKey switch
    {
        TadilatColumnKeys.DigitalReceived => "Projenin dijitali geldi mi?",
        TadilatColumnKeys.InspectorApproved => "Projeyi ilgili denetçi onayladı mı?",
        TadilatColumnKeys.OutputAndReportArrived => "Çıktı ve tadilat raporu büroya geldi mi?",
        TadilatColumnKeys.OfficialLetterSubmitted => "Üst yazı belediyeye teslim edildi mi?",
        TadilatColumnKeys.ArchivedFromMunicipality => "Projeler belediyeden alınıp arşive konuldu mu?",
        TadilatColumnKeys.JobName => "İşin ismi",
        TadilatColumnKeys.ProjectType => "Proje türü",
        TadilatColumnKeys.Description1 => "Açıklama 1",
        TadilatColumnKeys.Description2 => "Açıklama 2",
        _ => columnKey
    };

    private static string GetYibfFieldValue(YibfIsTakibiEntry entry, string columnKey) => columnKey switch
    {
        YibfIsTakibiColumnKeys.JobName => entry.JobName,
        YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => entry.MuellifBilgileriGeldiMi,
        YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => entry.DenetciAtamalariYapildiMi,
        YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => entry.TumProjelerinDijitaliVarMi,
        YibfIsTakibiColumnKeys.EvraklarTamMi => entry.EvraklarTamMi,
        YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => entry.YibfSozlesmeHazirlandiMi,
        YibfIsTakibiColumnKeys.DekontAlindiMi => entry.DekontAlindiMi,
        YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => entry.RuhsatBasvurusuYapildiMi,
        YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => entry.RuhsatNushasiAlindiMi,
        YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => entry.IsyeriTeslimTutangiHazirlandiMi,
        YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => entry.IsgYazisiHazirlandiMi,
        YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => entry.SaglikGuvenlikPlaniGeldiMi,
        YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => entry.TemelTopraklamaTutanagiHazirlandiMi,
        _ => string.Empty
    };

    private static string GetTadilatFieldValue(TadilatEntry entry, string columnKey) => columnKey switch
    {
        TadilatColumnKeys.JobName => entry.JobName,
        TadilatColumnKeys.ProjectType => entry.ProjectType,
        TadilatColumnKeys.DigitalReceived => entry.DigitalReceived,
        TadilatColumnKeys.InspectorApproved => entry.InspectorApproved,
        TadilatColumnKeys.OutputAndReportArrived => entry.OutputAndReportArrived,
        TadilatColumnKeys.OfficialLetterSubmitted => entry.OfficialLetterSubmitted,
        TadilatColumnKeys.ArchivedFromMunicipality => entry.ArchivedFromMunicipality,
        TadilatColumnKeys.Description1 => entry.Description1,
        TadilatColumnKeys.Description2 => entry.Description2,
        _ => string.Empty
    };

    private PersonnelAssignment? FindLocked(PersonnelAssignmentSourceModule module, Guid sourceEntryId, string? columnKey)
    {
        var key = MakeKey(module, sourceEntryId, columnKey);
        return _assignments.FirstOrDefault(a => MakeKey(a.SourceModule, a.SourceEntryId, a.SourceColumnKey) == key);
    }

    private string? GetPersonnelNameLocked(Guid? personnelId)
    {
        if (!personnelId.HasValue)
        {
            return null;
        }

        return _personnel.FirstOrDefault(p => p.Id == personnelId.Value)?.Name;
    }

    private static string MakeKey(PersonnelAssignmentSourceModule module, Guid entryId, string? columnKey)
        => $"{(int)module}|{entryId:N}|{columnKey ?? string.Empty}";

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
