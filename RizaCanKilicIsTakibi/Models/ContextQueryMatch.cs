namespace RizaCanKilicIsTakibi.Models;

public sealed class ContextQueryMatch
{
    public string MatchedKey { get; init; } = string.Empty;
    public string NormalizedKey { get; init; } = string.Empty;
    public ContextQueryMatchType MatchType { get; init; }
    public ContextQueryIntentType IntentType { get; init; }
    public ContextQueryRole? PrimaryRole { get; init; }
    public IReadOnlyList<ContextQueryRole> AllowedRoles { get; init; } = Array.Empty<ContextQueryRole>();

    public bool HasMatch => !string.IsNullOrWhiteSpace(MatchedKey);
}
