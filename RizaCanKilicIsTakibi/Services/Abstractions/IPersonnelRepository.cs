using RizaCanKilicIsTakibi.Models;

namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface IPersonnelRepository
{
    IReadOnlyList<Personnel> GetAllPersonnel();
    IReadOnlyList<PersonnelAssignment> GetAllAssignments();
    Task<IReadOnlyList<Personnel>> GetAllPersonnelAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PersonnelAssignment>> GetAllAssignmentsAsync(CancellationToken cancellationToken = default);
    void ReplaceAll(IEnumerable<Personnel> personnel, IEnumerable<PersonnelAssignment> assignments);
    Task SavePersonnelAsync(Personnel person, CancellationToken cancellationToken = default);
    Task DeletePersonnelAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpsertAssignmentAsync(PersonnelAssignment assignment, CancellationToken cancellationToken = default);
    Task DeleteAssignmentAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAssignmentsForSourceAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId, CancellationToken cancellationToken = default);
    Task ClearPersonnelIdAsync(Guid personnelId, CancellationToken cancellationToken = default);
}
