using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Text.RegularExpressions;

namespace RizaCanKilicIsTakibi.Services;

public sealed class ContextQueryService : IContextQueryService
{
    private static readonly string[] OwnerPhrases =
    [
        "yapi sahibi",
        "yapı sahibi"
    ];

    private static readonly string[] ContractorPhrases =
    [
        "müteahhit",
        "muteahhit",
        "yüklenici",
        "yuklenici"
    ];

    private static readonly string[] CompletedIntentPhrases =
    [
        "tamamlanan var mi",
        "tamamlanan var mı",
        "tamamlandi mi",
        "tamamlandı mı",
        "tamamlandi",
        "tamamlandı",
        "sonuc ne",
        "sonuç ne",
        "olumlu mu"
    ];

    private static readonly string[] PendingIntentPhrases =
    [
        "ne bekliyor",
        "neler bekliyor",
        "bekleyen ne var",
        "bekleyenler",
        "bekleyen isler",
        "bekleyen işler",
        "sonuc bekleniyor mu",
        "sonuç bekleniyor mu"
    ];

    private static readonly string[] OpenIssueIntentPhrases =
    [
        "ne eksik var",
        "neler eksik",
        "ne eksik",
        "hangi eksik",
        "sorun ne",
        "sorun var mi",
        "sorun var mı",
        "yapilmamis",
        "yapılmamış",
        "hangi is yapilmamis",
        "hangi iş yapılmamış"
    ];

    private static readonly string[] NoisePhrases =
    [
        "ada parselde",
        "ada parsel",
        "yapi sahibi",
        "yapi sahibinde",
        "yapı sahibi",
        "yapı sahibinde",
        "müteahhit",
        "muteahhit",
        "yüklenici",
        "yuklenici",
        "yibf no",
        "yibf numarasi",
        "yibf numarası",
        "ne eksik var",
        "neler eksik",
        "ne eksik",
        "ne var",
        "durumu ne",
        "durumu",
        "hangi eksik",
        "hangi isler var",
        "hangi işler var",
        "hangi is yapilmamis",
        "hangi iş yapılmamış",
        "yapilmamis",
        "yapılmamış",
        "sorun ne",
        "sorun var mi",
        "sorun var mı",
        "bak",
        "goster",
        "göster",
        "diyelim ki",
        "de",
        "için",
        "var",
        "nedir",
        "nasil",
        "nasıl"
    ];

    public ContextQueryMatch ExtractMatch(string question, IEnumerable<SearchResultItem> corpus)
    {
        var sourceItems = corpus.ToList();
        if (string.IsNullOrWhiteSpace(question))
        {
            return new ContextQueryMatch();
        }

        var intentType = DetectIntent(question);
        var candidates = BuildCandidates(question).ToList();
        foreach (var candidate in candidates)
        {
            if (sourceItems.Any(item => MatchesCandidate(item.SearchText, candidate)))
            {
                return BuildMatch(candidate, question, intentType);
            }
        }

        var fallback = candidates.FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(fallback) ? new ContextQueryMatch() : BuildMatch(fallback, question, intentType);
    }

    private static IEnumerable<string> BuildCandidates(string question)
    {
        var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in Regex.Matches(question, "\"([^\"]+)\""))
        {
            if (TryAddCandidate(match.Groups[1].Value, yielded, out var candidate))
            {
                yield return candidate;
            }
        }

        foreach (Match match in Regex.Matches(question, @"\b\d+(?:[-/]\d+)+\b"))
        {
            if (TryAddCandidate(match.Value, yielded, out var candidate))
            {
                yield return candidate;
            }
        }

        var cleaned = question;
        foreach (var phrase in NoisePhrases)
        {
            cleaned = Regex.Replace(
                cleaned,
                $@"\b{Regex.Escape(phrase)}\b",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        cleaned = Regex.Replace(cleaned, @"[^\p{L}\p{N}\-\/ ]+", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (TryAddCandidate(cleaned, yielded, out var cleanedCandidate))
        {
            yield return cleanedCandidate;
        }

        var tokens = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 2)
            .ToList();

        for (var length = Math.Min(4, tokens.Count); length >= 2; length--)
        {
            for (var index = 0; index <= tokens.Count - length; index++)
            {
                var combined = string.Join(' ', tokens.Skip(index).Take(length));
                if (TryAddCandidate(combined, yielded, out var combinedCandidate))
                {
                    yield return combinedCandidate;
                }
            }
        }

        foreach (var token in tokens)
        {
            if (TryAddCandidate(token, yielded, out var tokenCandidate))
            {
                yield return tokenCandidate;
            }
        }
    }

    private static bool MatchesCandidate(string source, string candidate)
    {
        var match = BuildMatch(candidate);
        if (!match.HasMatch)
        {
            return false;
        }

        return match.MatchType switch
        {
            ContextQueryMatchType.AdaParsel => ContainsExactToken(source, match.MatchedKey),
            ContextQueryMatchType.YibfNo => ContainsExactToken(source, match.MatchedKey) || SearchTextNormalizer.StartsWith(source, match.MatchedKey),
            ContextQueryMatchType.YapiSahibi or ContextQueryMatchType.Muteahhit => ContainsNameMatch(source, match.NormalizedKey),
            _ => SearchTextNormalizer.Contains(source, match.MatchedKey)
        };
    }

    private static ContextQueryMatch BuildMatch(string? candidate)
        => BuildMatch(candidate, string.Empty, ContextQueryIntentType.GeneralStatus);

    private static ContextQueryMatch BuildMatch(string? candidate, string question, ContextQueryIntentType intentType)
    {
        var trimmed = candidate?.Trim() ?? string.Empty;
        var normalized = SearchTextNormalizer.Normalize(trimmed);
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new ContextQueryMatch();
        }

        var primaryRole = DetectPrimaryRole(question);
        var allowedRoles = BuildAllowedRoles(primaryRole);

        return new ContextQueryMatch
        {
            MatchedKey = trimmed,
            NormalizedKey = normalized,
            MatchType = DetectType(trimmed, question),
            IntentType = intentType,
            PrimaryRole = primaryRole,
            AllowedRoles = allowedRoles
        };
    }

    private static ContextQueryIntentType DetectIntent(string question)
    {
        if (ContainsAny(question, CompletedIntentPhrases))
        {
            return ContextQueryIntentType.CompletedOnly;
        }

        if (ContainsAny(question, PendingIntentPhrases))
        {
            return ContextQueryIntentType.PendingOnly;
        }

        if (ContainsAny(question, OpenIssueIntentPhrases))
        {
            return ContextQueryIntentType.OpenIssues;
        }

        return ContextQueryIntentType.GeneralStatus;
    }

    private static ContextQueryMatchType DetectType(string candidate, string question)
    {
        if (Regex.IsMatch(candidate, @"^\d+(?:[-/]\d+)+$"))
        {
            return ContextQueryMatchType.AdaParsel;
        }

        if (Regex.IsMatch(candidate, @"^\d{4,}$"))
        {
            return ContextQueryMatchType.YibfNo;
        }

        if (ContainsAny(question, ContractorPhrases))
        {
            return ContextQueryMatchType.Muteahhit;
        }

        return ContextQueryMatchType.YapiSahibi;
    }

    private static ContextQueryRole? DetectPrimaryRole(string question)
    {
        if (ContainsAny(question, ContractorPhrases))
        {
            return ContextQueryRole.Muteahhit;
        }

        if (ContainsAny(question, OwnerPhrases))
        {
            return ContextQueryRole.YapiSahibi;
        }

        return null;
    }

    private static IReadOnlyList<ContextQueryRole> BuildAllowedRoles(ContextQueryRole? primaryRole)
        => primaryRole switch
        {
            ContextQueryRole.Muteahhit => [ContextQueryRole.Muteahhit, ContextQueryRole.YapiSahibi],
            ContextQueryRole.YapiSahibi => [ContextQueryRole.YapiSahibi],
            _ => Array.Empty<ContextQueryRole>()
        };

    private static bool ContainsExactToken(string source, string key)
    {
        var normalizedSource = SearchTextNormalizer.Normalize(source);
        var normalizedKey = SearchTextNormalizer.Normalize(key);
        if (string.IsNullOrWhiteSpace(normalizedSource) || string.IsNullOrWhiteSpace(normalizedKey))
        {
            return false;
        }

        return Regex.IsMatch(
            normalizedSource,
            $@"(?<![a-z0-9]){Regex.Escape(normalizedKey)}(?![a-z0-9])",
            RegexOptions.CultureInvariant);
    }

    private static bool ContainsNameMatch(string source, string normalizedKey)
    {
        var sourceTokens = Tokenize(SearchTextNormalizer.Normalize(source));
        var queryTokens = Tokenize(normalizedKey);
        if (sourceTokens.Length == 0 || queryTokens.Length == 0)
        {
            return false;
        }

        return queryTokens.All(queryToken =>
            queryToken.Length >= 3 &&
            sourceTokens.Any(sourceToken => sourceToken.StartsWith(queryToken, StringComparison.Ordinal)));
    }

    private static string[] Tokenize(string value)
        => value.Split([' ', '-', '/', ',', '.', ';', ':', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool ContainsAny(string question, IEnumerable<string> phrases)
    {
        foreach (var phrase in phrases)
        {
            if (Regex.IsMatch(
                    question,
                    $@"\b{Regex.Escape(phrase)}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryAddCandidate(string? value, ISet<string> yielded, out string candidate)
    {
        candidate = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var hasNumericIdentity = trimmed.Any(char.IsDigit) || trimmed.Contains('-') || trimmed.Contains('/');
        if ((!hasNumericIdentity && trimmed.Length < 3) || !yielded.Add(trimmed))
        {
            return false;
        }

        candidate = trimmed;
        return true;
    }
}
