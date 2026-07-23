namespace RizaCanKilicIsTakibi.Models;

public enum CrashRecoveryWizardChoice
{
    Recover,
    Discard
}

public sealed class CrashRecoveryWizardRequest
{
    public DateTime? LastSuccessfulSaveAt { get; init; }
    public DateTime? RecoveryCreatedAt { get; init; }
    public IReadOnlyList<string> ChangeLines { get; init; } = Array.Empty<string>();
}
