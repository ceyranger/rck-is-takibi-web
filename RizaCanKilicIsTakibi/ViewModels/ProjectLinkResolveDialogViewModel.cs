using CommunityToolkit.Mvvm.Input;
using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ProjectLinkResolveDialogViewModel : ViewModelBase
{
    private readonly IProjectCatalogEntryDialogService _catalogEntryDialogService;
    private readonly IConfirmationService _confirmationService;
    private readonly IReadOnlyList<ProjectCatalogEntry> _catalog;
    private readonly Dictionary<Guid, ProjectCatalogEntry> _createdEntries = new();

    public ProjectLinkResolveDialogViewModel(
        IReadOnlyList<UnresolvedProjectLinkItem> unresolved,
        IReadOnlyList<ProjectCatalogEntry> catalog,
        IProjectCatalogService catalogService,
        IProjectCatalogEntryDialogService catalogEntryDialogService,
        IConfirmationService confirmationService)
    {
        _catalog = catalog;
        _catalogEntryDialogService = catalogEntryDialogService;
        _confirmationService = confirmationService;
        Items = new ObservableCollection<ProjectLinkResolveItemViewModel>(
            unresolved.Select(item => new ProjectLinkResolveItemViewModel(item, catalog, catalogService)));

        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, false));
        ApplyBestCandidatesCommand = new RelayCommand(ApplyBestCandidates, () => EligibleBestCandidateCount > 0);
    }

    public event EventHandler<bool>? RequestClose;

    public ObservableCollection<ProjectLinkResolveItemViewModel> Items { get; }

    public int EligibleBestCandidateCount => Items.Count(item => item.HasBestCandidate);

    public AsyncRelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand ApplyBestCandidatesCommand { get; }

    public IReadOnlyList<UnresolvedLinkResolution> BuildResolutions()
        => Items.Select(item =>
            item.BuildResolution(_createdEntries.TryGetValue(item.Source.EntryId, out var created) ? created : null)).ToList();

    private void ApplyBestCandidates()
    {
        var count = EligibleBestCandidateCount;
        if (count == 0)
        {
            return;
        }

        if (!_confirmationService.Confirm(new ConfirmationRequest
            {
                Kind = ConfirmationKind.Save,
                Title = "En İyi Adaylara Bağla",
                Message = $"Adayı olan {count} kayıt, listedeki en yüksek skorlu projeye bağlanacak şekilde işaretlenecek.\n\nHenüz uygulanmaz; Tamam + Bağlantıları uygula ile kalıcı olur. Devam edilsin mi?"
            }))
        {
            return;
        }

        foreach (var item in Items.Where(item => item.HasBestCandidate))
        {
            item.ApplyBestCandidate();
        }
    }

    private async Task SaveAsync()
    {
        _createdEntries.Clear();
        foreach (var item in Items)
        {
            if (item.ResolutionKind == UnresolvedLinkResolutionKind.LinkToProject
                && item.SelectedProjectId is null)
            {
                return;
            }

            if (item.ResolutionKind != UnresolvedLinkResolutionKind.CreateCatalogAndLink)
            {
                continue;
            }

            var created = await _catalogEntryDialogService.ShowDialogAsync(item.BuildSuggestedCatalogEntry(), _catalog);
            if (created is null)
            {
                return;
            }

            _createdEntries[item.Source.EntryId] = created;
            item.SelectedProjectId = created.Id;
        }

        RequestClose?.Invoke(this, true);
    }
}
