using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class SearchOverlayViewModel : ViewModelBase
{
    private bool _isOpen;
    private string _query = string.Empty;
    private string _assistantQuery = string.Empty;
    private string _assistantAnswer = string.Empty;
    private string _assistantExplanation = string.Empty;
    private string _assistantExplanationLabel = string.Empty;
    private string _assistantMatchedKey = string.Empty;
    private SearchScope _selectedScope = SearchScope.All;
    private SearchOverlayMode _selectedMode = SearchOverlayMode.Classic;
    private bool _isScopeSelectionVisible = true;
    private string _headerTitle = "Arama";
    private int _focusRequestToken;
    private SearchResultItem? _selectedResult;

    public SearchOverlayViewModel()
    {
        Results = new ObservableCollection<SearchResultItem>();
        PrimaryResults = new ObservableCollection<SearchResultItem>();
        SecondaryResults = new ObservableCollection<SearchResultItem>();
        AssistantSections = new ObservableCollection<QueryInsightSection>();
        AssistantSources = new ObservableCollection<SearchResultItem>();
        DirectAssistantSources = new ObservableCollection<SearchResultItem>();
        ContextAssistantSources = new ObservableCollection<SearchResultItem>();
        ClearCommand = new RelayCommand(Clear);
        SelectScopeCommand = new RelayCommand<SearchScope>(scope => SelectedScope = scope);
        SelectModeCommand = new RelayCommand<SearchOverlayMode>(mode => SelectedMode = mode);
    }

    public event EventHandler<string>? QueryChanged;
    public event EventHandler<SearchScope>? ScopeChanged;
    public event EventHandler<SearchOverlayMode>? ModeChanged;

    public bool IsOpen
    {
        get => _isOpen;
        set => SetProperty(ref _isOpen, value);
    }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
            {
                OnPropertyChanged(nameof(HasNoClassicResults));
                OnPropertyChanged(nameof(ShowClassicEmptyHint));
                QueryChanged?.Invoke(this, _query);
            }
        }
    }

    public SearchScope SelectedScope
    {
        get => _selectedScope;
        set
        {
            if (SetProperty(ref _selectedScope, value))
            {
                ScopeChanged?.Invoke(this, _selectedScope);
            }
        }
    }

    public SearchOverlayMode SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(IsClassicMode));
                OnPropertyChanged(nameof(IsAssistantMode));
                OnPropertyChanged(nameof(IsScopeBarVisible));
                OnPropertyChanged(nameof(HasNoClassicResults));
                OnPropertyChanged(nameof(ShowClassicEmptyHint));
                ModeChanged?.Invoke(this, _selectedMode);
            }
        }
    }

    public string AssistantQuery
    {
        get => _assistantQuery;
        set => SetProperty(ref _assistantQuery, value);
    }

    public string AssistantAnswer
    {
        get => _assistantAnswer;
        set
        {
            if (SetProperty(ref _assistantAnswer, value))
            {
                OnPropertyChanged(nameof(HasAssistantAnswer));
            }
        }
    }

    public string AssistantExplanation
    {
        get => _assistantExplanation;
        set
        {
            if (SetProperty(ref _assistantExplanation, value))
            {
                OnPropertyChanged(nameof(HasAssistantExplanation));
            }
        }
    }

    public string AssistantExplanationLabel
    {
        get => _assistantExplanationLabel;
        set => SetProperty(ref _assistantExplanationLabel, value);
    }

    public string AssistantMatchedKey
    {
        get => _assistantMatchedKey;
        set => SetProperty(ref _assistantMatchedKey, value);
    }

    public ObservableCollection<SearchResultItem> Results { get; }
    public ObservableCollection<SearchResultItem> PrimaryResults { get; }
    public ObservableCollection<SearchResultItem> SecondaryResults { get; }
    public ObservableCollection<QueryInsightSection> AssistantSections { get; }
    public ObservableCollection<SearchResultItem> AssistantSources { get; }
    public ObservableCollection<SearchResultItem> DirectAssistantSources { get; }
    public ObservableCollection<SearchResultItem> ContextAssistantSources { get; }

    public bool HasPrimaryResults => PrimaryResults.Count > 0;
    public bool HasSecondaryResults => SecondaryResults.Count > 0;
    public bool HasNoClassicResults => IsClassicMode && !HasPrimaryResults && !string.IsNullOrWhiteSpace(Query);
    public bool ShowClassicEmptyHint => IsClassicMode && !HasPrimaryResults && string.IsNullOrWhiteSpace(Query);
    public bool HasAssistantAnswer => !string.IsNullOrWhiteSpace(AssistantAnswer);
    public bool HasAssistantExplanation => !string.IsNullOrWhiteSpace(AssistantExplanation);
    public bool HasAssistantSections => AssistantSections.Count > 0;
    public bool HasAssistantSources => AssistantSources.Count > 0;
    public bool HasDirectAssistantSources => DirectAssistantSources.Count > 0;
    public bool HasContextAssistantSources => ContextAssistantSources.Count > 0;
    public bool IsClassicMode => SelectedMode == SearchOverlayMode.Classic;
    public bool IsAssistantMode => SelectedMode == SearchOverlayMode.Assistant;

    public bool IsScopeSelectionVisible
    {
        get => _isScopeSelectionVisible;
        private set
        {
            if (SetProperty(ref _isScopeSelectionVisible, value))
            {
                OnPropertyChanged(nameof(IsScopeBarVisible));
            }
        }
    }

    public string HeaderTitle
    {
        get => _headerTitle;
        private set => SetProperty(ref _headerTitle, value);
    }

    public int FocusRequestToken
    {
        get => _focusRequestToken;
        private set => SetProperty(ref _focusRequestToken, value);
    }

    public bool IsScopeBarVisible => IsScopeSelectionVisible && IsClassicMode;

    public SearchResultItem? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (SetProperty(ref _selectedResult, value))
            {
                OnPropertyChanged(nameof(HasSelectedResult));
            }
        }
    }

    public bool HasSelectedResult => SelectedResult is not null;

    public RelayCommand ClearCommand { get; }
    public RelayCommand<SearchScope> SelectScopeCommand { get; }
    public RelayCommand<SearchOverlayMode> SelectModeCommand { get; }

    public void Open() => OpenGlobal();

    public void OpenGlobal()
    {
        IsScopeSelectionVisible = true;
        HeaderTitle = "Arama";
        SelectedScope = SearchScope.All;
        IsOpen = true;
    }

    public void OpenForTab(SearchScope scope)
    {
        IsScopeSelectionVisible = false;
        HeaderTitle = "Bu sekmede ara";
        SelectedScope = scope;
        IsOpen = true;
        RequestFocus();
    }

    public void PrepareFullPageSearch()
    {
        var wasTabLocked = !IsScopeSelectionVisible;
        IsOpen = false;
        IsScopeSelectionVisible = true;
        HeaderTitle = "Arama";
        if (wasTabLocked)
        {
            SelectedScope = SearchScope.All;
        }
    }

    public void RequestFocus() => FocusRequestToken++;

    public void Close()
    {
        IsOpen = false;
        Query = string.Empty;
        AssistantQuery = string.Empty;
        AssistantAnswer = string.Empty;
        AssistantExplanation = string.Empty;
        AssistantExplanationLabel = string.Empty;
        AssistantMatchedKey = string.Empty;
        SelectedScope = SearchScope.All;
        SelectedMode = SearchOverlayMode.Classic;
        IsScopeSelectionVisible = true;
        HeaderTitle = "Arama";
        Results.Clear();
        PrimaryResults.Clear();
        SecondaryResults.Clear();
        AssistantSections.Clear();
        AssistantSources.Clear();
        DirectAssistantSources.Clear();
        ContextAssistantSources.Clear();
        SelectedResult = null;
        OnPropertyChanged(nameof(HasPrimaryResults));
        OnPropertyChanged(nameof(HasSecondaryResults));
        OnPropertyChanged(nameof(HasAssistantSections));
        OnPropertyChanged(nameof(HasAssistantSources));
        OnPropertyChanged(nameof(HasDirectAssistantSources));
        OnPropertyChanged(nameof(HasContextAssistantSources));
    }

    public void SetResults(IEnumerable<SearchResultItem> items)
    {
        var resultList = items.ToList();

        Results.Clear();
        PrimaryResults.Clear();
        SecondaryResults.Clear();

        foreach (var item in resultList)
        {
            Results.Add(item);
        }

        foreach (var item in resultList.Take(8))
        {
            PrimaryResults.Add(item);
        }

        foreach (var item in resultList.Skip(8))
        {
            SecondaryResults.Add(item);
        }

        if (_selectedResult is not null
            && resultList.Any(item => item.Kind == _selectedResult.Kind && item.ItemId == _selectedResult.ItemId))
        {
            SelectedResult = resultList.First(item => item.Kind == _selectedResult.Kind && item.ItemId == _selectedResult.ItemId);
        }
        else
        {
            SelectedResult = resultList.FirstOrDefault();
        }

        OnPropertyChanged(nameof(HasPrimaryResults));
        OnPropertyChanged(nameof(HasSecondaryResults));
        OnPropertyChanged(nameof(HasNoClassicResults));
        OnPropertyChanged(nameof(ShowClassicEmptyHint));
    }

    public void SetAssistantResult(QueryInsightResult result)
    {
        AssistantMatchedKey = result.MatchedKey;
        AssistantAnswer = string.IsNullOrWhiteSpace(result.SummaryText) ? result.AnswerText : result.SummaryText;
        AssistantExplanation = result.ExplanationText;
        AssistantExplanationLabel = string.IsNullOrWhiteSpace(result.ExplanationText)
            ? string.Empty
            : result.Sources.Any(source => string.Equals(source.MatchOriginLabel, "BAĞLAM", StringComparison.Ordinal))
                ? "BAĞLAM"
                : "DOĞRUDAN";
        AssistantSections.Clear();
        AssistantSources.Clear();
        DirectAssistantSources.Clear();
        ContextAssistantSources.Clear();

        foreach (var section in result.Sections)
        {
            AssistantSections.Add(section);
        }

        foreach (var item in result.Sources)
        {
            AssistantSources.Add(item);
            if (string.Equals(item.MatchOriginLabel, "BAĞLAM", StringComparison.Ordinal))
            {
                ContextAssistantSources.Add(item);
            }
            else
            {
                DirectAssistantSources.Add(item);
            }
        }

        SelectedResult = result.Sources.FirstOrDefault();
        OnPropertyChanged(nameof(HasAssistantSections));
        OnPropertyChanged(nameof(HasAssistantSources));
        OnPropertyChanged(nameof(HasDirectAssistantSources));
        OnPropertyChanged(nameof(HasContextAssistantSources));
    }

    private void Clear()
    {
        if (IsAssistantMode)
        {
            AssistantQuery = string.Empty;
            AssistantAnswer = string.Empty;
            AssistantExplanation = string.Empty;
            AssistantExplanationLabel = string.Empty;
            AssistantMatchedKey = string.Empty;
            AssistantSections.Clear();
            AssistantSources.Clear();
            DirectAssistantSources.Clear();
            ContextAssistantSources.Clear();
            SelectedResult = null;
            OnPropertyChanged(nameof(HasAssistantSections));
            OnPropertyChanged(nameof(HasAssistantSources));
            OnPropertyChanged(nameof(HasDirectAssistantSources));
            OnPropertyChanged(nameof(HasContextAssistantSources));
            return;
        }

        Query = string.Empty;
        SelectedResult = null;
    }
}
