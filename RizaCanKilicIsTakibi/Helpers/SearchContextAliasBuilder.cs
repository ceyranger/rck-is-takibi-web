using System.Text.RegularExpressions;

namespace RizaCanKilicIsTakibi.Helpers;

public sealed record SearchContextIdentitySeed(
    string? AdaParsel,
    string? YapiSahibi = null,
    string? Muteahhit = null,
    string? YibfNo = null);

public sealed class SearchContextAliasLookup
{
    public IReadOnlyDictionary<string, string> ParcelAliases { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    public IReadOnlyList<SearchContextExactAliasRule> ExactMatchRules { get; init; } = Array.Empty<SearchContextExactAliasRule>();
}

public sealed record SearchContextExactAliasRule(string TriggerText, string AliasText, bool IsNumericToken);

public static class SearchContextAliasBuilder
{
    private static readonly Regex ParcelTokenRegex = new(@"\b\d{1,5}-\d{1,5}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> IgnoredNameTokens = new(StringComparer.Ordinal)
    {
        "insaat",
        "inşaat",
        "enerji",
        "yapi",
        "yapı",
        "sanayi",
        "ticaret",
        "limited",
        "ltd",
        "sti",
        "şti",
        "anonim",
        "as",
        "aş",
        "grup",
        "madencilik",
        "ins",
        "turizm"
    };

    public static SearchContextAliasLookup BuildAliasLookup(IEnumerable<SearchContextIdentitySeed> seeds)
    {
        var seedsList = seeds.ToList();
        var parcelLookup = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var exactRuleLookup = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var tokenRuleCandidates = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var tokenRuleParcels = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var seed in seedsList)
        {
            var normalizedParcel = SearchTextNormalizer.Normalize(seed.AdaParsel);
            if (string.IsNullOrWhiteSpace(normalizedParcel))
            {
                continue;
            }

            if (!parcelLookup.TryGetValue(normalizedParcel, out var aliases))
            {
                aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                parcelLookup[normalizedParcel] = aliases;
            }

            AddAlias(aliases, seed.AdaParsel);
            AddAlias(aliases, seed.YapiSahibi);
            AddAlias(aliases, seed.Muteahhit);
            AddAlias(aliases, seed.YibfNo);

            AddExactRule(exactRuleLookup, seed.YapiSahibi, aliases);
            AddExactRule(exactRuleLookup, seed.Muteahhit, aliases);
            AddExactRule(exactRuleLookup, seed.YibfNo, aliases);
            AddNameTokenRules(tokenRuleCandidates, tokenRuleParcels, normalizedParcel, seed.YapiSahibi, aliases);
            AddNameTokenRules(tokenRuleCandidates, tokenRuleParcels, normalizedParcel, seed.Muteahhit, aliases);
        }

        foreach (var pair in tokenRuleCandidates.Where(pair =>
                     tokenRuleParcels.TryGetValue(pair.Key, out var parcels) &&
                     parcels.Count == 1))
        {
            exactRuleLookup[pair.Key] = pair.Value;
        }

        return new SearchContextAliasLookup
        {
            ParcelAliases = parcelLookup.ToDictionary(
            pair => pair.Key,
            pair => string.Join(' ', pair.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
            StringComparer.Ordinal),
            ExactMatchRules = exactRuleLookup
                .Select(pair => new SearchContextExactAliasRule(
                    pair.Key,
                    string.Join(' ', pair.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
                    IsNumericToken(pair.Key)))
                .ToArray()
        };
    }

    public static string EnrichSearchText(string? searchText, SearchContextAliasLookup aliasLookup)
    {
        var baseText = searchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseText))
        {
            return baseText;
        }

        var aliases = new List<string>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ParcelTokenRegex.Matches(baseText))
        {
            var normalizedParcel = SearchTextNormalizer.Normalize(match.Value);
            if (string.IsNullOrWhiteSpace(normalizedParcel) ||
                !aliasLookup.ParcelAliases.TryGetValue(normalizedParcel, out var aliasText) ||
                string.IsNullOrWhiteSpace(aliasText) ||
                !added.Add(aliasText))
            {
                continue;
            }

            aliases.Add(aliasText);
        }

        var normalizedBaseText = SearchTextNormalizer.Normalize(baseText);
        foreach (var rule in aliasLookup.ExactMatchRules)
        {
            if (string.IsNullOrWhiteSpace(rule.AliasText) ||
                !ContainsExactMatch(normalizedBaseText, rule))
            {
                continue;
            }

            if (!added.Add(rule.AliasText))
            {
                continue;
            }

            aliases.Add(rule.AliasText);
        }

        return aliases.Count == 0
            ? baseText
            : $"{baseText} {string.Join(' ', aliases)}".Trim();
    }

    private static bool ContainsExactMatch(string normalizedBaseText, SearchContextExactAliasRule rule)
    {
        if (string.IsNullOrWhiteSpace(normalizedBaseText) || string.IsNullOrWhiteSpace(rule.TriggerText))
        {
            return false;
        }

        if (rule.IsNumericToken)
        {
            return Regex.IsMatch(
                normalizedBaseText,
                $@"(?<![a-z0-9]){Regex.Escape(rule.TriggerText)}(?![a-z0-9])",
                RegexOptions.CultureInvariant);
        }

        return normalizedBaseText.Contains(rule.TriggerText, StringComparison.Ordinal);
    }

    private static void AddExactRule(IDictionary<string, HashSet<string>> ruleLookup, string? triggerValue, IEnumerable<string> aliases)
    {
        var normalizedTrigger = SearchTextNormalizer.Normalize(triggerValue);
        if (string.IsNullOrWhiteSpace(normalizedTrigger))
        {
            return;
        }

        if (!ruleLookup.TryGetValue(normalizedTrigger, out var aliasSet))
        {
            aliasSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ruleLookup[normalizedTrigger] = aliasSet;
        }

        foreach (var alias in aliases)
        {
            aliasSet.Add(alias);
        }
    }

    private static void AddNameTokenRules(
        IDictionary<string, HashSet<string>> ruleLookup,
        IDictionary<string, HashSet<string>> parcelLookup,
        string normalizedParcel,
        string? triggerValue,
        IEnumerable<string> aliases)
    {
        var normalizedTrigger = SearchTextNormalizer.Normalize(triggerValue);
        if (string.IsNullOrWhiteSpace(normalizedTrigger))
        {
            return;
        }

        foreach (var token in Tokenize(normalizedTrigger))
        {
            if (token.Length < 4 || IgnoredNameTokens.Contains(token))
            {
                continue;
            }

            if (!ruleLookup.TryGetValue(token, out var aliasSet))
            {
                aliasSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ruleLookup[token] = aliasSet;
            }

            if (!parcelLookup.TryGetValue(token, out var parcelSet))
            {
                parcelSet = new HashSet<string>(StringComparer.Ordinal);
                parcelLookup[token] = parcelSet;
            }

            parcelSet.Add(normalizedParcel);

            foreach (var alias in aliases)
            {
                aliasSet.Add(alias);
            }
        }
    }

    private static bool IsNumericToken(string normalizedValue)
        => normalizedValue.All(ch => char.IsDigit(ch));

    private static IEnumerable<string> Tokenize(string value)
        => value.Split([' ', '-', '/', ',', '.', ';', ':', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void AddAlias(ISet<string> aliases, string? value)
    {
        var trimmed = value?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            aliases.Add(trimmed);
        }
    }
}
