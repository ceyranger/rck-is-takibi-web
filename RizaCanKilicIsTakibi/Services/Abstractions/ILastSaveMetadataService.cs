namespace RizaCanKilicIsTakibi.Services.Abstractions;

public interface ILastSaveMetadataService
{
    DateTime? LoadLastSuccessfulSaveAt();
    Task SaveLastSuccessfulSaveAtAsync(DateTime timestamp, CancellationToken cancellationToken = default);
}
