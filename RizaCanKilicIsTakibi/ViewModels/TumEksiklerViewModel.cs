using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class TumEksiklerViewModel : ViewModelBase
{
    private const string StrongRedColor = "#FFFF0000";
    private const string StrongYellowColor = "#FFFFFF00";
    private const string LegacyPaleRedColor = "#FFF4C4C4";
    private const string LegacyPaleYellowColor = "#FFF7EDB3";
    private const string AllFilter = "Tümü";
    private const string CriticalFilter = "Kritik";
    private const string WarningFilter = "Uyarı";
    private const string BlankRequiredFilter = "Boş Zorunlu";
    private const string UnmatchedGroupTitle = "Eşleşmeyen Eksikler";
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly IReadOnlyList<RequiredFieldDefinition<YibfIsTakibiEntry>> RequiredYibfFields =
    [
        new(YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi, "Müellif bilgileri geldi mi?", entry => entry.MuellifBilgileriGeldiMi),
        new(YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi, "Denetçi atamaları yapıldı mı?", entry => entry.DenetciAtamalariYapildiMi),
        new(YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi, "Tüm projelerin dijitali var mı?", entry => entry.TumProjelerinDijitaliVarMi),
        new(YibfIsTakibiColumnKeys.EvraklarTamMi, "Evraklar tam mı?", entry => entry.EvraklarTamMi),
        new(YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi, "YİBF sözleşme/taahhütname hazırlandı mı?", entry => entry.YibfSozlesmeHazirlandiMi),
        new(YibfIsTakibiColumnKeys.DekontAlindiMi, "Dekont alındı mı?", entry => entry.DekontAlindiMi),
        new(YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi, "Ruhsat başvurusu yapıldı mı?", entry => entry.RuhsatBasvurusuYapildiMi),
        new(YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi, "Ruhsat nüshası alındı mı?", entry => entry.RuhsatNushasiAlindiMi),
        new(YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi, "İşyeri teslim tutanağı hazırlandı mı?", entry => entry.IsyeriTeslimTutangiHazirlandiMi),
        new(YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi, "İSG yazısı hazırlandı mı?", entry => entry.IsgYazisiHazirlandiMi),
        new(YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi, "Sağlık güvenlik planı geldi mi?", entry => entry.SaglikGuvenlikPlaniGeldiMi),
        new(YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi, "Temel topraklama tutanağı hazırlandı mı?", entry => entry.TemelTopraklamaTutanagiHazirlandiMi)
    ];

    private static readonly IReadOnlyList<RequiredFieldDefinition<TadilatEntry>> RequiredTadilatFields =
    [
        new(TadilatColumnKeys.DigitalReceived, "Projenin dijitali geldi mi?", entry => entry.DigitalReceived),
        new(TadilatColumnKeys.InspectorApproved, "Projeyi ilgili denetçi onayladı mı?", entry => entry.InspectorApproved),
        new(TadilatColumnKeys.OutputAndReportArrived, "Çıktı ve tadilat raporu büroya geldi mi?", entry => entry.OutputAndReportArrived),
        new(TadilatColumnKeys.OfficialLetterSubmitted, "Üst yazı belediyeye teslim edildi mi?", entry => entry.OfficialLetterSubmitted),
        new(TadilatColumnKeys.ArchivedFromMunicipality, "Projeler belediyeden alınıp arşive konuldu mu?", entry => entry.ArchivedFromMunicipality)
    ];

    private readonly List<EksikIsGroupViewModel> _allGroups = [];
    private string _searchQuery = string.Empty;
    private string _selectedSourceFilter = AllFilter;
    private string _selectedSeverityFilter = AllFilter;
    private bool _showUnmatched = true;
    private bool _showBlankRequired = true;

    public TumEksiklerViewModel()
    {
        SourceFilters = [AllFilter, "YİBF Ana Bilgi", "YİBF İş Takibi", "Tadilat", "Eksik Proje", "Karot"];
        SeverityFilters = [AllFilter, CriticalFilter, WarningFilter, BlankRequiredFilter];
        Groups = [];
        RefreshFiltersCommand = new RelayCommand(ApplyFilters);
    }

    public ObservableRangeCollection<EksikIsGroupViewModel> Groups { get; }
    public ObservableCollection<string> SourceFilters { get; }
    public ObservableCollection<string> SeverityFilters { get; }
    public RelayCommand RefreshFiltersCommand { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedSourceFilter
    {
        get => _selectedSourceFilter;
        set
        {
            if (SetProperty(ref _selectedSourceFilter, string.IsNullOrWhiteSpace(value) ? AllFilter : value))
            {
                ApplyFilters();
            }
        }
    }

    public string SelectedSeverityFilter
    {
        get => _selectedSeverityFilter;
        set
        {
            if (SetProperty(ref _selectedSeverityFilter, string.IsNullOrWhiteSpace(value) ? AllFilter : value))
            {
                ApplyFilters();
            }
        }
    }

    public bool ShowUnmatched
    {
        get => _showUnmatched;
        set
        {
            if (SetProperty(ref _showUnmatched, value))
            {
                ApplyFilters();
            }
        }
    }

    public bool ShowBlankRequired
    {
        get => _showBlankRequired;
        set
        {
            if (SetProperty(ref _showBlankRequired, value))
            {
                ApplyFilters();
            }
        }
    }

    public int TotalGroupCount => Groups.Count;
    public int TotalItemCount => Groups.Sum(group => group.EksikCount);
    public int CriticalItemCount => Groups.Sum(group => group.CriticalCount);
    public int UnmatchedItemCount => Groups.Where(group => group.MatchStatus == EksikMatchStatus.Unmatched).Sum(group => group.EksikCount);
    public bool HasItems => TotalItemCount > 0;

    public void RefreshFrom(
        IEnumerable<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<YibfAnaBilgiEvent> anaBilgiEvents,
        IEnumerable<YibfIsTakibiEntry> isTakibiEntries,
        IEnumerable<YibfCellState> yibfCellStates,
        IEnumerable<TadilatEntry> aktifTadilatEntries,
        IEnumerable<TadilatCellState> tadilatCellStates,
        IEnumerable<MissingProjectEntry> missingProjectEntries,
        IEnumerable<KarotEntry> karotEntries)
    {
        var groups = BuildAllGroups(
            anaBilgiEntries.ToList(),
            anaBilgiEvents.ToList(),
            isTakibiEntries.ToList(),
            yibfCellStates.ToList(),
            aktifTadilatEntries.ToList(),
            tadilatCellStates.ToList(),
            missingProjectEntries.ToList(),
            karotEntries.ToList());

        _allGroups.Clear();
        _allGroups.AddRange(groups);
        ApplyFilters();
    }

    private List<EksikIsGroupViewModel> BuildAllGroups(
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IReadOnlyList<YibfAnaBilgiEvent> anaBilgiEvents,
        IReadOnlyList<YibfIsTakibiEntry> isTakibiEntries,
        IReadOnlyList<YibfCellState> yibfCellStates,
        IReadOnlyList<TadilatEntry> aktifTadilatEntries,
        IReadOnlyList<TadilatCellState> tadilatCellStates,
        IReadOnlyList<MissingProjectEntry> missingProjectEntries,
        IReadOnlyList<KarotEntry> karotEntries)
    {
        var groupsByEntryId = anaBilgiEntries.ToDictionary(
            entry => entry.Id,
            entry => new EksikIsGroupViewModel(
                entry.Id,
                FirstNonEmpty(entry.AdaParsel, "(Ada parsel yok)"),
                FirstNonEmpty(entry.YapiSahibi, "(Yapı sahibi yok)"),
                entry.YibfNo,
                entry.Muteahhit,
                EksikMatchStatus.Matched));

        var unmatched = new EksikIsGroupViewModel(null, UnmatchedGroupTitle, string.Empty, string.Empty, string.Empty, EksikMatchStatus.Unmatched);
        var yibfStateLookup = yibfCellStates.ToDictionary(state => BuildCellStateKey(state.EntryId, state.ColumnKey), StringComparer.OrdinalIgnoreCase);
        var tadilatStateLookup = tadilatCellStates.ToDictionary(state => BuildCellStateKey(state.EntryId, state.ColumnKey), StringComparer.OrdinalIgnoreCase);

        AppendYibfAnaBilgiEvents(groupsByEntryId, anaBilgiEntries, anaBilgiEvents);
        AppendYibfIsTakibi(groupsByEntryId, unmatched, anaBilgiEntries, isTakibiEntries, yibfStateLookup);
        AppendTadilat(groupsByEntryId, unmatched, anaBilgiEntries, aktifTadilatEntries, tadilatStateLookup);
        AppendMissingProject(groupsByEntryId, unmatched, anaBilgiEntries, missingProjectEntries);
        AppendKarot(groupsByEntryId, unmatched, anaBilgiEntries, karotEntries);

        var result = groupsByEntryId.Values
            .Where(group => group.Items.Count > 0)
            .Concat(unmatched.Items.Count > 0 ? [unmatched] : [])
            .Select(group => group.WithOrderedItems())
            .OrderBy(group => group.MatchStatus == EksikMatchStatus.Unmatched ? 1 : 0)
            .ThenByDescending(group => group.CriticalCount)
            .ThenByDescending(group => group.EksikCount)
            .ThenByDescending(group => group.LatestUpdatedAt)
            .ThenBy(group => group.HeaderText, StringComparer.Create(TurkishCulture, ignoreCase: true))
            .ToList();

        return result;
    }

    private static void AppendYibfAnaBilgiEvents(
        IReadOnlyDictionary<Guid, EksikIsGroupViewModel> groupsByEntryId,
        IReadOnlyList<YibfAnaBilgiEntry> entries,
        IEnumerable<YibfAnaBilgiEvent> events)
    {
        var entryLookup = entries.ToDictionary(entry => entry.Id);
        foreach (var item in events.Where(item => IsPendingColor(item.BackgroundColor)))
        {
            if (!groupsByEntryId.TryGetValue(item.EntryId, out var group) || !entryLookup.TryGetValue(item.EntryId, out var entry))
            {
                continue;
            }

            var severity = IsRedColor(item.BackgroundColor) ? EksikSeverity.Critical : EksikSeverity.Warning;
            group.Items.Add(new EksikItemViewModel(
                "YİBF Ana Bilgi",
                "Olay Akışı",
                FirstNonEmpty(item.Description, item.NoteText, "İşaretli olay"),
                FirstNonEmpty(item.Description, "-"),
                item.NoteText,
                severity,
                item.EventDate ?? entry.UpdatedAt,
                MainNavigationTab.YibfAnaBilgi,
                item.Id,
                SearchResultKind.YibfAnaBilgiEvent,
                item.EntryId));
        }
    }

    private static void AppendYibfIsTakibi(
        IReadOnlyDictionary<Guid, EksikIsGroupViewModel> groupsByEntryId,
        EksikIsGroupViewModel unmatched,
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<YibfIsTakibiEntry> entries,
        IReadOnlyDictionary<string, YibfCellState> stateLookup)
    {
        foreach (var entry in entries)
        {
            var group = ResolveGroup(anaBilgiEntries, groupsByEntryId, unmatched, CombineSearchText(GetYibfIsTakibiValues(entry)));
            foreach (var field in RequiredYibfFields)
            {
                stateLookup.TryGetValue(BuildCellStateKey(entry.Id, field.ColumnKey), out var state);
                AddFieldIssue(
                    group,
                    "YİBF İş Takibi",
                    field.Label,
                    BuildYibfReason(field.ColumnKey),
                    field.ReadValue(entry),
                    state?.BackgroundColor,
                    state?.NoteText,
                    entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt,
                    MainNavigationTab.YibfIsTakibi,
                    entry.Id,
                    SearchResultKind.YibfIsTakibiEntry);
            }
        }
    }

    private static void AppendTadilat(
        IReadOnlyDictionary<Guid, EksikIsGroupViewModel> groupsByEntryId,
        EksikIsGroupViewModel unmatched,
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<TadilatEntry> entries,
        IReadOnlyDictionary<string, TadilatCellState> stateLookup)
    {
        foreach (var entry in entries)
        {
            var group = ResolveGroup(anaBilgiEntries, groupsByEntryId, unmatched, CombineSearchText(entry.JobName, entry.ProjectType, entry.Description1, entry.Description2));
            foreach (var field in RequiredTadilatFields)
            {
                stateLookup.TryGetValue(BuildCellStateKey(entry.Id, field.ColumnKey), out var state);
                AddFieldIssue(
                    group,
                    "Tadilat",
                    field.Label,
                    BuildTadilatReason(field.ColumnKey),
                    field.ReadValue(entry),
                    state?.BackgroundColor,
                    state?.NoteText,
                    entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt,
                    MainNavigationTab.TadilatTakibi,
                    entry.Id,
                    SearchResultKind.TadilatEntry);
            }
        }
    }

    private static void AppendMissingProject(
        IReadOnlyDictionary<Guid, EksikIsGroupViewModel> groupsByEntryId,
        EksikIsGroupViewModel unmatched,
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<MissingProjectEntry> entries)
    {
        foreach (var entry in entries)
        {
            var group = ResolveGroup(anaBilgiEntries, groupsByEntryId, unmatched, CombineSearchText(entry.AdaParsel, entry.YapiSahibi));
            group.Items.Add(new EksikItemViewModel(
                "Eksik Proje",
                "Eksik Proje",
                FirstNonEmpty(entry.MissingProjectText, entry.Description, "Eksik proje kaydı"),
                FirstNonEmpty(entry.MissingProjectText, entry.Description, "-"),
                string.Empty,
                EksikSeverity.Info,
                entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt,
                MainNavigationTab.EksikProje,
                entry.Id,
                SearchResultKind.MissingProjectEntry));
        }
    }

    private static void AppendKarot(
        IReadOnlyDictionary<Guid, EksikIsGroupViewModel> groupsByEntryId,
        EksikIsGroupViewModel unmatched,
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IEnumerable<KarotEntry> entries)
    {
        foreach (var entry in entries)
        {
            var severity = entry.Status switch
            {
                KarotStatus.KarotAlindiOlumsuz => EksikSeverity.Critical,
                KarotStatus.KarotAlinacak or KarotStatus.KarotAlindiSonucBekleniyor => EksikSeverity.Warning,
                _ => (EksikSeverity?)null
            };

            if (severity is null)
            {
                continue;
            }

            var group = ResolveGroup(anaBilgiEntries, groupsByEntryId, unmatched, CombineSearchText(entry.YibfNo, entry.AdaParsel, entry.YapiSahibi));
            var reason = entry.Status switch
            {
                KarotStatus.KarotAlindiOlumsuz => "Karot sonucu olumsuz",
                KarotStatus.KarotAlindiSonucBekleniyor => "Karot sonucu bekleniyor",
                _ => "Karot alınacak"
            };
            var katBilgisiText = string.IsNullOrWhiteSpace(entry.KatBilgisi)
                ? string.Empty
                : $"Kat Bilgisi: {entry.KatBilgisi.Trim()}";
            var reasonWithKatBilgisi = string.IsNullOrWhiteSpace(katBilgisiText)
                ? reason
                : $"{reason} - {katBilgisiText}";

            group.Items.Add(new EksikItemViewModel(
                "Karot",
                "Karot Durumu",
                reasonWithKatBilgisi,
                FirstNonEmpty(entry.KatBilgisi, entry.Aciklama, "-"),
                entry.Aciklama,
                severity.Value,
                entry.UpdatedAt == default ? entry.CreatedAt : entry.UpdatedAt,
                MainNavigationTab.KarotTakibi,
                entry.Id,
                SearchResultKind.KarotEntry));
        }
    }

    private static void AddFieldIssue(
        EksikIsGroupViewModel group,
        string sourceModule,
        string fieldLabel,
        string reason,
        string? currentValue,
        string? backgroundColor,
        string? noteText,
        DateTime updatedAt,
        MainNavigationTab targetTab,
        Guid targetId,
        SearchResultKind targetKind)
    {
        var normalizedColor = NormalizeColor(backgroundColor);
        var hasPendingColor = IsPendingColor(normalizedColor);
        var isBlank = string.IsNullOrWhiteSpace(currentValue);
        if (!hasPendingColor && !isBlank)
        {
            return;
        }

        var severity = hasPendingColor
            ? IsRedColor(normalizedColor) ? EksikSeverity.Critical : EksikSeverity.Warning
            : EksikSeverity.BlankRequired;

        group.Items.Add(new EksikItemViewModel(
            sourceModule,
            fieldLabel,
            isBlank && !hasPendingColor ? "Boş takip alanı" : reason,
            string.IsNullOrWhiteSpace(currentValue) ? "(Boş)" : currentValue.Trim(),
            noteText?.Trim() ?? string.Empty,
            severity,
            updatedAt,
            targetTab,
            targetId,
            targetKind));
    }

    private static EksikIsGroupViewModel ResolveGroup(
        IReadOnlyList<YibfAnaBilgiEntry> anaBilgiEntries,
        IReadOnlyDictionary<Guid, EksikIsGroupViewModel> groupsByEntryId,
        EksikIsGroupViewModel unmatched,
        string sourceText)
    {
        var match = anaBilgiEntries
            .Where(entry => IsIdentityMatch(sourceText, entry.YibfNo))
            .Take(2)
            .ToList();

        if (match.Count != 1)
        {
            match = anaBilgiEntries
                .Where(entry => IsIdentityMatch(sourceText, entry.AdaParsel))
                .Take(2)
                .ToList();
        }

        return match.Count == 1 && groupsByEntryId.TryGetValue(match[0].Id, out var group) ? group : unmatched;
    }

    private void ApplyFilters()
    {
        var query = SearchQuery.Trim();
        var filteredGroups = new List<EksikIsGroupViewModel>();
        foreach (var group in _allGroups)
        {
            if (group.MatchStatus == EksikMatchStatus.Unmatched && !ShowUnmatched)
            {
                continue;
            }

            var items = group.Items
                .Where(MatchesItemFilters)
                .Where(item => string.IsNullOrWhiteSpace(query) || MatchesSearch(group, item, query))
                .OrderBy(item => item.SeverityRank)
                .ThenByDescending(item => item.UpdatedAt)
                .ThenBy(item => item.SourceModule, StringComparer.Create(TurkishCulture, ignoreCase: true))
                .ThenBy(item => item.FieldLabel, StringComparer.Create(TurkishCulture, ignoreCase: true))
                .ToList();

            if (items.Count > 0)
            {
                filteredGroups.Add(group.WithItems(items));
            }
        }

        Groups.ReplaceRange(filteredGroups
            .OrderBy(group => group.MatchStatus == EksikMatchStatus.Unmatched ? 1 : 0)
            .ThenByDescending(group => group.CriticalCount)
            .ThenByDescending(group => group.EksikCount)
            .ThenByDescending(group => group.LatestUpdatedAt)
            .ThenBy(group => group.HeaderText, StringComparer.Create(TurkishCulture, ignoreCase: true)));

        OnPropertyChanged(nameof(TotalGroupCount));
        OnPropertyChanged(nameof(TotalItemCount));
        OnPropertyChanged(nameof(CriticalItemCount));
        OnPropertyChanged(nameof(UnmatchedItemCount));
        OnPropertyChanged(nameof(HasItems));
    }

    private bool MatchesItemFilters(EksikItemViewModel item)
    {
        if (!string.Equals(SelectedSourceFilter, AllFilter, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(item.SourceModule, SelectedSourceFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!ShowBlankRequired && item.Severity == EksikSeverity.BlankRequired)
        {
            return false;
        }

        return SelectedSeverityFilter switch
        {
            CriticalFilter => item.Severity == EksikSeverity.Critical,
            WarningFilter => item.Severity == EksikSeverity.Warning,
            BlankRequiredFilter => item.Severity == EksikSeverity.BlankRequired,
            _ => true
        };
    }

    private static bool MatchesSearch(EksikIsGroupViewModel group, EksikItemViewModel item, string query)
        => Contains(group.AdaParsel, query)
           || Contains(group.YapiSahibi, query)
           || Contains(group.YibfNo, query)
           || Contains(group.Muteahhit, query)
           || Contains(item.SourceModule, query)
           || Contains(item.FieldLabel, query)
           || Contains(item.Reason, query)
           || Contains(item.CurrentValue, query)
           || Contains(item.NoteText, query);

    private static bool Contains(string? source, string query)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private static bool IsIdentityMatch(string sourceText, string? identity)
    {
        var normalizedIdentity = NormalizeIdentity(identity);
        if (string.IsNullOrWhiteSpace(normalizedIdentity))
        {
            return false;
        }

        var normalizedSource = NormalizeIdentity(sourceText);
        return normalizedSource.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => string.Equals(token, normalizedIdentity, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeIdentity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToUpper(TurkishCulture))
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch is '/' or '-' ? ch : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string CombineSearchText(params string?[] values)
        => string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));

    private static string[] GetYibfIsTakibiValues(YibfIsTakibiEntry entry)
        =>
        [
            entry.JobName,
            entry.MuellifBilgileriGeldiMi,
            entry.DenetciAtamalariYapildiMi,
            entry.TumProjelerinDijitaliVarMi,
            entry.EvraklarTamMi,
            entry.YibfSozlesmeHazirlandiMi,
            entry.DekontAlindiMi,
            entry.RuhsatBasvurusuYapildiMi,
            entry.RuhsatNushasiAlindiMi,
            entry.IsyeriTeslimTutangiHazirlandiMi,
            entry.IsgYazisiHazirlandiMi,
            entry.SaglikGuvenlikPlaniGeldiMi,
            entry.TemelTopraklamaTutanagiHazirlandiMi
        ];

    private static string BuildCellStateKey(Guid entryId, string columnKey)
        => $"{entryId:N}|{columnKey}";

    private static bool IsPendingColor(string? color)
    {
        var normalized = NormalizeColor(color);
        return string.Equals(normalized, StrongRedColor, StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, StrongYellowColor, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRedColor(string? color)
        => string.Equals(NormalizeColor(color), StrongRedColor, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeColor(string? color)
    {
        var normalized = color?.Trim() ?? string.Empty;
        if (string.Equals(normalized, LegacyPaleRedColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongRedColor;
        }

        if (string.Equals(normalized, LegacyPaleYellowColor, StringComparison.OrdinalIgnoreCase))
        {
            return StrongYellowColor;
        }

        return normalized;
    }

    private static string FirstNonEmpty(params string?[] values)
        => StringHelpers.FirstNonEmpty(values);

    private static string BuildYibfReason(string columnKey)
        => columnKey switch
        {
            YibfIsTakibiColumnKeys.MuellifBilgileriGeldiMi => "Müellif bilgileri gelmedi",
            YibfIsTakibiColumnKeys.DenetciAtamalariYapildiMi => "Denetçi atamaları yapılmadı",
            YibfIsTakibiColumnKeys.TumProjelerinDijitaliVarMi => "Tüm projelerin dijitali yok",
            YibfIsTakibiColumnKeys.EvraklarTamMi => "Evraklar tam değil",
            YibfIsTakibiColumnKeys.YibfSozlesmeHazirlandiMi => "YİBF sözleşme/taahhütname hazır değil",
            YibfIsTakibiColumnKeys.DekontAlindiMi => "Dekont alınmadı",
            YibfIsTakibiColumnKeys.RuhsatBasvurusuYapildiMi => "Ruhsat başvurusu yapılmadı",
            YibfIsTakibiColumnKeys.RuhsatNushasiAlindiMi => "Ruhsat nüshası alınmadı",
            YibfIsTakibiColumnKeys.IsyeriTeslimTutangiHazirlandiMi => "İşyeri teslim tutanağı hazırlanmadı",
            YibfIsTakibiColumnKeys.IsgYazisiHazirlandiMi => "İSG yazısı hazırlanmadı",
            YibfIsTakibiColumnKeys.SaglikGuvenlikPlaniGeldiMi => "Sağlık güvenlik planı gelmedi",
            YibfIsTakibiColumnKeys.TemelTopraklamaTutanagiHazirlandiMi => "Temel topraklama tutanağı hazırlanmadı",
            _ => "İşaretli takip alanı"
        };

    private static string BuildTadilatReason(string columnKey)
        => columnKey switch
        {
            TadilatColumnKeys.DigitalReceived => "Projenin dijitali gelmedi",
            TadilatColumnKeys.InspectorApproved => "Denetçi onaylamadı",
            TadilatColumnKeys.OutputAndReportArrived => "Çıktı/rapor gelmedi",
            TadilatColumnKeys.OfficialLetterSubmitted => "Üst yazı teslim edilmedi",
            TadilatColumnKeys.ArchivedFromMunicipality => "Projeler arşive eklenmedi",
            _ => "İşaretli takip alanı"
        };

    private sealed record RequiredFieldDefinition<TEntry>(string ColumnKey, string Label, Func<TEntry, string> ReadValue);
}

public sealed class EksikIsGroupViewModel
{
    public EksikIsGroupViewModel(Guid? entryId, string adaParsel, string yapiSahibi, string yibfNo, string muteahhit, EksikMatchStatus matchStatus)
    {
        EntryId = entryId;
        AdaParsel = adaParsel;
        YapiSahibi = yapiSahibi;
        YibfNo = yibfNo;
        Muteahhit = muteahhit;
        MatchStatus = matchStatus;
        Items = [];
    }

    private EksikIsGroupViewModel(Guid? entryId, string adaParsel, string yapiSahibi, string yibfNo, string muteahhit, EksikMatchStatus matchStatus, IEnumerable<EksikItemViewModel> items)
        : this(entryId, adaParsel, yapiSahibi, yibfNo, muteahhit, matchStatus)
    {
        Items.ReplaceRange(items);
    }

    public Guid? EntryId { get; }
    public string AdaParsel { get; }
    public string YapiSahibi { get; }
    public string YibfNo { get; }
    public string Muteahhit { get; }
    public EksikMatchStatus MatchStatus { get; }
    public ObservableRangeCollection<EksikItemViewModel> Items { get; }
    public int EksikCount => Items.Count;
    public int CriticalCount => Items.Count(item => item.Severity == EksikSeverity.Critical);
    public DateTime LatestUpdatedAt => Items.Select(item => item.UpdatedAt).DefaultIfEmpty(DateTime.MinValue).Max();
    public string HeaderText => MatchStatus == EksikMatchStatus.Unmatched ? AdaParsel : $"{AdaParsel} - {YapiSahibi}";
    public string DetailText
    {
        get
        {
            var parts = new[] { string.IsNullOrWhiteSpace(YibfNo) ? null : $"YİBF: {YibfNo}", string.IsNullOrWhiteSpace(Muteahhit) ? null : $"Müteahhit: {Muteahhit}" }
                .Where(item => !string.IsNullOrWhiteSpace(item));
            return string.Join(" | ", parts);
        }
    }
    public string CountText => $"{EksikCount} eksik / {CriticalCount} kritik";

    public EksikIsGroupViewModel WithOrderedItems()
        => WithItems(Items.OrderBy(item => item.SeverityRank).ThenByDescending(item => item.UpdatedAt).ThenBy(item => item.SourceModule).ThenBy(item => item.FieldLabel));

    public EksikIsGroupViewModel WithItems(IEnumerable<EksikItemViewModel> items)
        => new(EntryId, AdaParsel, YapiSahibi, YibfNo, Muteahhit, MatchStatus, items);
}

public sealed class EksikItemViewModel
{
    public EksikItemViewModel(
        string sourceModule,
        string fieldLabel,
        string reason,
        string currentValue,
        string noteText,
        EksikSeverity severity,
        DateTime updatedAt,
        MainNavigationTab targetTab,
        Guid targetId,
        SearchResultKind targetKind,
        Guid? parentTargetId = null)
    {
        SourceModule = sourceModule;
        FieldLabel = fieldLabel;
        Reason = reason;
        CurrentValue = currentValue;
        NoteText = noteText;
        Severity = severity;
        UpdatedAt = updatedAt;
        TargetTab = targetTab;
        TargetId = targetId;
        TargetKind = targetKind;
        ParentTargetId = parentTargetId;
        NavigationTarget = new SearchResultItem
        {
            Kind = targetKind,
            TargetTab = targetTab,
            ItemId = targetId,
            ParentItemId = parentTargetId,
            BoardLabel = sourceModule,
            Title = fieldLabel,
            Summary = reason,
            SearchText = $"{sourceModule} {fieldLabel} {reason} {currentValue} {noteText}",
            RawSearchText = $"{sourceModule} {fieldLabel} {reason} {currentValue} {noteText}"
        };
    }

    public string SourceModule { get; }
    public string FieldLabel { get; }
    public string Reason { get; }
    public string CurrentValue { get; }
    public string NoteText { get; }
    public EksikSeverity Severity { get; }
    public DateTime UpdatedAt { get; }
    public MainNavigationTab TargetTab { get; }
    public Guid TargetId { get; }
    public SearchResultKind TargetKind { get; }
    public Guid? ParentTargetId { get; }
    public SearchResultItem NavigationTarget { get; }
    public bool HasNote => !string.IsNullOrWhiteSpace(NoteText);
    public int SeverityRank => Severity switch
    {
        EksikSeverity.Critical => 0,
        EksikSeverity.Warning => 1,
        EksikSeverity.BlankRequired => 2,
        _ => 3
    };
    public string SeverityLabel => Severity switch
    {
        EksikSeverity.Critical => "KRİTİK",
        EksikSeverity.Warning => "UYARI",
        EksikSeverity.BlankRequired => "BOŞ",
        _ => "BİLGİ"
    };
    public Brush SeverityBrush => Severity switch
    {
        EksikSeverity.Critical => Brushes.Firebrick,
        EksikSeverity.Warning => Brushes.Goldenrod,
        EksikSeverity.BlankRequired => Brushes.SlateGray,
        _ => Brushes.SteelBlue
    };
    public Brush SeverityForegroundBrush => Severity == EksikSeverity.Warning ? Brushes.Black : Brushes.White;
}

public enum EksikSeverity
{
    Critical,
    Warning,
    BlankRequired,
    Info
}

public enum EksikMatchStatus
{
    Matched,
    Unmatched
}
