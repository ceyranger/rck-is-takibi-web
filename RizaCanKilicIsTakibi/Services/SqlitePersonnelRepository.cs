using Microsoft.Data.Sqlite;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.IO;

namespace RizaCanKilicIsTakibi.Services;

public sealed class SqlitePersonnelRepository : IPersonnelRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqlitePersonnelRepository(string databasePath)
    {
        _databasePath = databasePath;
        _connectionString = SqliteConnectionSettings.BuildConnectionString(_databasePath);
        EnsureDatabase();
    }

    public IReadOnlyList<Personnel> GetAllPersonnel()
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        return ReadPersonnel(connection);
    }

    public IReadOnlyList<PersonnelAssignment> GetAllAssignments()
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        return ReadAssignments(connection);
    }

    public Task<IReadOnlyList<Personnel>> GetAllPersonnelAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetAllPersonnel());

    public Task<IReadOnlyList<PersonnelAssignment>> GetAllAssignmentsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GetAllAssignments());

    public void ReplaceAll(IEnumerable<Personnel> personnel, IEnumerable<PersonnelAssignment> assignments)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var transaction = connection.BeginTransaction();

        using (var deleteAssignments = connection.CreateCommand())
        {
            deleteAssignments.Transaction = transaction;
            deleteAssignments.CommandText = "DELETE FROM PersonnelAssignments;";
            deleteAssignments.ExecuteNonQuery();
        }

        using (var deletePersonnel = connection.CreateCommand())
        {
            deletePersonnel.Transaction = transaction;
            deletePersonnel.CommandText = "DELETE FROM Personnel;";
            deletePersonnel.ExecuteNonQuery();
        }

        foreach (var person in personnel)
        {
            UpsertPersonnel(connection, transaction, person);
        }

        foreach (var assignment in assignments)
        {
            UpsertAssignment(connection, transaction, assignment);
        }

        transaction.Commit();
    }

    public Task SavePersonnelAsync(Personnel person, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var transaction = connection.BeginTransaction();
        UpsertPersonnel(connection, transaction, person);
        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task DeletePersonnelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var transaction = connection.BeginTransaction();

        using (var clearCmd = connection.CreateCommand())
        {
            clearCmd.Transaction = transaction;
            clearCmd.CommandText = "UPDATE PersonnelAssignments SET PersonnelId = NULL WHERE PersonnelId = $id;";
            clearCmd.Parameters.AddWithValue("$id", id.ToString());
            clearCmd.ExecuteNonQuery();
        }

        using (var deleteCmd = connection.CreateCommand())
        {
            deleteCmd.Transaction = transaction;
            deleteCmd.CommandText = "DELETE FROM Personnel WHERE Id = $id;";
            deleteCmd.Parameters.AddWithValue("$id", id.ToString());
            deleteCmd.ExecuteNonQuery();
        }

        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task UpsertAssignmentAsync(PersonnelAssignment assignment, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var transaction = connection.BeginTransaction();

        // Uniqueness: one assignment per (module, entry, column)
        using (var findCmd = connection.CreateCommand())
        {
            findCmd.Transaction = transaction;
            findCmd.CommandText = @"
SELECT Id FROM PersonnelAssignments
WHERE SourceModule = $module
  AND SourceEntryId = $entryId
  AND IFNULL(SourceColumnKey, '') = IFNULL($columnKey, '');";
            findCmd.Parameters.AddWithValue("$module", (int)assignment.SourceModule);
            findCmd.Parameters.AddWithValue("$entryId", assignment.SourceEntryId.ToString());
            findCmd.Parameters.AddWithValue("$columnKey", (object?)assignment.SourceColumnKey ?? DBNull.Value);
            var existingId = findCmd.ExecuteScalar() as string;
            if (!string.IsNullOrWhiteSpace(existingId) && Guid.TryParse(existingId, out var parsed) && parsed != assignment.Id)
            {
                assignment.Id = parsed;
            }
        }

        UpsertAssignment(connection, transaction, assignment);
        transaction.Commit();
        return Task.CompletedTask;
    }

    public Task DeleteAssignmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM PersonnelAssignments WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task DeleteAssignmentsForSourceAsync(PersonnelAssignmentSourceModule module, Guid sourceEntryId, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM PersonnelAssignments WHERE SourceModule = $module AND SourceEntryId = $entryId;";
        cmd.Parameters.AddWithValue("$module", (int)module);
        cmd.Parameters.AddWithValue("$entryId", sourceEntryId.ToString());
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task ClearPersonnelIdAsync(Guid personnelId, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE PersonnelAssignments SET PersonnelId = NULL WHERE PersonnelId = $id;";
        cmd.Parameters.AddWithValue("$id", personnelId.ToString());
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private void EnsureDatabase()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = SqliteConnectionSettings.Open(_connectionString);
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
CREATE TABLE IF NOT EXISTS Personnel (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    SortOrder INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS PersonnelAssignments (
    Id TEXT PRIMARY KEY,
    PersonnelId TEXT NULL,
    SourceModule INTEGER NOT NULL,
    SourceEntryId TEXT NOT NULL,
    SourceColumnKey TEXT NULL,
    Status INTEGER NOT NULL,
    AssignedAt TEXT NOT NULL,
    CompletedAt TEXT NULL,
    PrioritySnapshot INTEGER NOT NULL DEFAULT 0,
    FieldLabelSnapshot TEXT NOT NULL DEFAULT '',
    SummarySnapshot TEXT NOT NULL DEFAULT '',
    ProjectIdentitySnapshot TEXT NOT NULL DEFAULT '',
    ModuleLabelSnapshot TEXT NOT NULL DEFAULT ''
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_PersonnelAssignments_Source
ON PersonnelAssignments (SourceModule, SourceEntryId, IFNULL(SourceColumnKey, ''));";
        createCommand.ExecuteNonQuery();
    }

    private static IReadOnlyList<Personnel> ReadPersonnel(SqliteConnection connection)
    {
        var result = new List<Personnel>();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, Name, SortOrder, CreatedAt, UpdatedAt
FROM Personnel
ORDER BY SortOrder, Name COLLATE NOCASE;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Personnel
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                SortOrder = reader.GetInt32(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                UpdatedAt = DateTime.Parse(reader.GetString(4))
            });
        }

        return result;
    }

    private static IReadOnlyList<PersonnelAssignment> ReadAssignments(SqliteConnection connection)
    {
        var result = new List<PersonnelAssignment>();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, PersonnelId, SourceModule, SourceEntryId, SourceColumnKey, Status, AssignedAt, CompletedAt,
       PrioritySnapshot, FieldLabelSnapshot, SummarySnapshot, ProjectIdentitySnapshot, ModuleLabelSnapshot
FROM PersonnelAssignments
ORDER BY AssignedAt DESC;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PersonnelAssignment
            {
                Id = Guid.Parse(reader.GetString(0)),
                PersonnelId = reader.IsDBNull(1) ? null : Guid.Parse(reader.GetString(1)),
                SourceModule = (PersonnelAssignmentSourceModule)reader.GetInt32(2),
                SourceEntryId = Guid.Parse(reader.GetString(3)),
                SourceColumnKey = reader.IsDBNull(4) ? null : reader.GetString(4),
                Status = (PersonnelAssignmentStatus)reader.GetInt32(5),
                AssignedAt = DateTime.Parse(reader.GetString(6)),
                CompletedAt = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                PrioritySnapshot = (PersonnelAssignmentPriority)reader.GetInt32(8),
                FieldLabelSnapshot = reader.GetString(9),
                SummarySnapshot = reader.GetString(10),
                ProjectIdentitySnapshot = reader.GetString(11),
                ModuleLabelSnapshot = reader.GetString(12)
            });
        }

        return result;
    }

    private static void UpsertPersonnel(SqliteConnection connection, SqliteTransaction transaction, Personnel person)
    {
        var createdAt = person.CreatedAt == default ? DateTime.Now : person.CreatedAt;
        var updatedAt = person.UpdatedAt == default ? DateTime.Now : person.UpdatedAt;

        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
INSERT INTO Personnel (Id, Name, SortOrder, CreatedAt, UpdatedAt)
VALUES ($id, $name, $sortOrder, $createdAt, $updatedAt)
ON CONFLICT(Id) DO UPDATE SET
    Name = excluded.Name,
    SortOrder = excluded.SortOrder,
    UpdatedAt = excluded.UpdatedAt;";
        cmd.Parameters.AddWithValue("$id", person.Id.ToString());
        cmd.Parameters.AddWithValue("$name", person.Name.Trim());
        cmd.Parameters.AddWithValue("$sortOrder", person.SortOrder);
        cmd.Parameters.AddWithValue("$createdAt", createdAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAt", updatedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static void UpsertAssignment(SqliteConnection connection, SqliteTransaction transaction, PersonnelAssignment assignment)
    {
        var assignedAt = assignment.AssignedAt == default ? DateTime.Now : assignment.AssignedAt;

        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
INSERT INTO PersonnelAssignments (
    Id, PersonnelId, SourceModule, SourceEntryId, SourceColumnKey, Status, AssignedAt, CompletedAt,
    PrioritySnapshot, FieldLabelSnapshot, SummarySnapshot, ProjectIdentitySnapshot, ModuleLabelSnapshot)
VALUES (
    $id, $personnelId, $module, $entryId, $columnKey, $status, $assignedAt, $completedAt,
    $priority, $fieldLabel, $summary, $projectIdentity, $moduleLabel)
ON CONFLICT(Id) DO UPDATE SET
    PersonnelId = excluded.PersonnelId,
    SourceModule = excluded.SourceModule,
    SourceEntryId = excluded.SourceEntryId,
    SourceColumnKey = excluded.SourceColumnKey,
    Status = excluded.Status,
    AssignedAt = excluded.AssignedAt,
    CompletedAt = excluded.CompletedAt,
    PrioritySnapshot = excluded.PrioritySnapshot,
    FieldLabelSnapshot = excluded.FieldLabelSnapshot,
    SummarySnapshot = excluded.SummarySnapshot,
    ProjectIdentitySnapshot = excluded.ProjectIdentitySnapshot,
    ModuleLabelSnapshot = excluded.ModuleLabelSnapshot;";
        cmd.Parameters.AddWithValue("$id", assignment.Id.ToString());
        cmd.Parameters.AddWithValue("$personnelId", assignment.PersonnelId.HasValue ? assignment.PersonnelId.Value.ToString() : DBNull.Value);
        cmd.Parameters.AddWithValue("$module", (int)assignment.SourceModule);
        cmd.Parameters.AddWithValue("$entryId", assignment.SourceEntryId.ToString());
        cmd.Parameters.AddWithValue("$columnKey", (object?)assignment.SourceColumnKey ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$status", (int)assignment.Status);
        cmd.Parameters.AddWithValue("$assignedAt", assignedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$completedAt", assignment.CompletedAt.HasValue ? assignment.CompletedAt.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$priority", (int)assignment.PrioritySnapshot);
        cmd.Parameters.AddWithValue("$fieldLabel", assignment.FieldLabelSnapshot ?? string.Empty);
        cmd.Parameters.AddWithValue("$summary", assignment.SummarySnapshot ?? string.Empty);
        cmd.Parameters.AddWithValue("$projectIdentity", assignment.ProjectIdentitySnapshot ?? string.Empty);
        cmd.Parameters.AddWithValue("$moduleLabel", assignment.ModuleLabelSnapshot ?? string.Empty);
        cmd.ExecuteNonQuery();
    }
}
