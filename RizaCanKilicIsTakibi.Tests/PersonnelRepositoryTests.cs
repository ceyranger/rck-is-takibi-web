using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services;

namespace RizaCanKilicIsTakibi.Tests;

public sealed class PersonnelRepositoryTests
{
    [Fact]
    public async Task UpsertAssignment_ReplacesSameSourceKey()
    {
        var path = Path.Combine(Path.GetTempPath(), $"personnel-test-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqlitePersonnelRepository(path);
            var person = new Personnel { Name = "Ali", SortOrder = 0 };
            await repo.SavePersonnelAsync(person);

            var first = new PersonnelAssignment
            {
                PersonnelId = person.Id,
                SourceModule = PersonnelAssignmentSourceModule.GenelTask,
                SourceEntryId = Guid.NewGuid(),
                SummarySnapshot = "iş 1"
            };
            await repo.UpsertAssignmentAsync(first);

            var secondPerson = new Personnel { Name = "Veli", SortOrder = 1 };
            await repo.SavePersonnelAsync(secondPerson);

            var replacement = new PersonnelAssignment
            {
                PersonnelId = secondPerson.Id,
                SourceModule = first.SourceModule,
                SourceEntryId = first.SourceEntryId,
                SummarySnapshot = "iş 1 güncel"
            };
            await repo.UpsertAssignmentAsync(replacement);

            var assignments = repo.GetAllAssignments();
            Assert.Single(assignments);
            Assert.Equal(secondPerson.Id, assignments[0].PersonnelId);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore locked temp db */ }
        }
    }

    [Fact]
    public async Task DeletePersonnel_ClearsAssignmentPersonnelId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"personnel-test-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqlitePersonnelRepository(path);
            var person = new Personnel { Name = "Ali" };
            await repo.SavePersonnelAsync(person);
            await repo.UpsertAssignmentAsync(new PersonnelAssignment
            {
                PersonnelId = person.Id,
                SourceModule = PersonnelAssignmentSourceModule.Karot,
                SourceEntryId = Guid.NewGuid()
            });

            await repo.DeletePersonnelAsync(person.Id);

            Assert.Empty(repo.GetAllPersonnel());
            var assignment = Assert.Single(repo.GetAllAssignments());
            Assert.Null(assignment.PersonnelId);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
}
