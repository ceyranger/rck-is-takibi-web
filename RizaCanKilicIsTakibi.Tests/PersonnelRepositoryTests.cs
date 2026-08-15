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

    [Fact]
    public async Task SyncCompletion_RemovesAssignmentsWhenSourceMissing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"personnel-test-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqlitePersonnelRepository(path);
            var service = new PersonnelAssignmentService(repo);
            var person = await service.AddPersonnelAsync("Ali");
            var missingSourceId = Guid.NewGuid();
            await service.AssignAsync(new PersonnelAssignment
            {
                PersonnelId = person.Id,
                SourceModule = PersonnelAssignmentSourceModule.Karot,
                SourceEntryId = missingSourceId,
                Status = PersonnelAssignmentStatus.Open
            });

            Assert.Single(service.GetAssignments());

            service.SyncCompletionFromSources(
                tasks: [],
                actions: [],
                missingProjects: [],
                karotEntries: [],
                tadilatEntries: [],
                tadilatCellStates: [],
                yibfEvents: [],
                yibfEntries: [],
                yibfCellStates: []);

            Assert.Empty(service.GetAssignments());
            Assert.Empty(repo.GetAllAssignments());
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public async Task UpdateAssignment_UpdatesEditableFieldsById()
    {
        var path = Path.Combine(Path.GetTempPath(), $"personnel-test-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqlitePersonnelRepository(path);
            var service = new PersonnelAssignmentService(repo);
            var ali = await service.AddPersonnelAsync("Ali");
            var veli = await service.AddPersonnelAsync("Veli");
            var sourceId = Guid.NewGuid();
            await service.AssignAsync(new PersonnelAssignment
            {
                PersonnelId = ali.Id,
                SourceModule = PersonnelAssignmentSourceModule.GenelTask,
                SourceEntryId = sourceId,
                SummarySnapshot = "eski",
                PrioritySnapshot = PersonnelAssignmentPriority.None,
                Status = PersonnelAssignmentStatus.Open
            });

            var existing = Assert.Single(service.GetAssignments());
            existing.PersonnelId = veli.Id;
            existing.SummarySnapshot = "yeni özet";
            existing.PrioritySnapshot = PersonnelAssignmentPriority.Critical;
            existing.Status = PersonnelAssignmentStatus.Completed;
            existing.FieldLabelSnapshot = "Alan";
            existing.ProjectIdentitySnapshot = "1/2";

            await service.UpdateAssignmentAsync(existing);

            var updated = Assert.Single(service.GetAssignments());
            Assert.Equal(veli.Id, updated.PersonnelId);
            Assert.Equal("yeni özet", updated.SummarySnapshot);
            Assert.Equal(PersonnelAssignmentPriority.Critical, updated.PrioritySnapshot);
            Assert.Equal(PersonnelAssignmentStatus.Completed, updated.Status);
            Assert.Equal("Alan", updated.FieldLabelSnapshot);
            Assert.Equal("1/2", updated.ProjectIdentitySnapshot);
            Assert.NotNull(updated.CompletedAt);
            Assert.Equal(sourceId, updated.SourceEntryId);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public async Task SyncCompletion_RemovesWhenKarotResolved_AndRestoreWorks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"personnel-test-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqlitePersonnelRepository(path);
            var service = new PersonnelAssignmentService(repo);
            var person = await service.AddPersonnelAsync("Ali");
            var karotId = Guid.NewGuid();
            await service.AssignAsync(new PersonnelAssignment
            {
                PersonnelId = person.Id,
                SourceModule = PersonnelAssignmentSourceModule.Karot,
                SourceEntryId = karotId,
                Status = PersonnelAssignmentStatus.Open,
                SummarySnapshot = "karot işi"
            });

            var karot = new KarotEntry
            {
                Id = karotId,
                Status = KarotStatus.KarotAlindiOlumlu
            };

            var removed = service.SyncCompletionFromSources(
                tasks: [],
                actions: [],
                missingProjects: [],
                karotEntries: [karot],
                tadilatEntries: [],
                tadilatCellStates: [],
                yibfEvents: [],
                yibfEntries: [],
                yibfCellStates: []);

            Assert.Single(removed);
            Assert.Empty(service.GetAssignments());

            await service.RestoreAssignmentsAsync(removed);
            var restored = Assert.Single(service.GetAssignments());
            Assert.Equal(karotId, restored.SourceEntryId);
            Assert.Equal(PersonnelAssignmentStatus.Open, restored.Status);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public async Task ManualAssignment_IsNotRemovedBySync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"personnel-test-{Guid.NewGuid():N}.db");
        try
        {
            var repo = new SqlitePersonnelRepository(path);
            var service = new PersonnelAssignmentService(repo);
            var person = await service.AddPersonnelAsync("Ali");
            await service.AssignAsync(new PersonnelAssignment
            {
                PersonnelId = person.Id,
                SourceModule = PersonnelAssignmentSourceModule.Manual,
                SourceEntryId = Guid.NewGuid(),
                Status = PersonnelAssignmentStatus.Open,
                SummarySnapshot = "manuel iş"
            });

            var removed = service.SyncCompletionFromSources(
                tasks: [],
                actions: [],
                missingProjects: [],
                karotEntries: [],
                tadilatEntries: [],
                tadilatCellStates: [],
                yibfEvents: [],
                yibfEntries: [],
                yibfCellStates: []);

            Assert.Empty(removed);
            Assert.Single(service.GetAssignments());
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }
}
