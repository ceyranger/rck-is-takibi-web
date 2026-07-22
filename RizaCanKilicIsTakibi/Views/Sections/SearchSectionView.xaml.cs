using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.ViewModels;

namespace RizaCanKilicIsTakibi.Views.Sections;

public partial class SearchSectionView : UserControl
{
    private SearchOverlayViewModel? _searchOverlay;

    public SearchSectionView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachSearchOverlay();
        Loaded += (_, _) => FocusActiveQueryBox();
        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
            {
                FocusActiveQueryBox();
            }
        };
    }

    private void AttachSearchOverlay()
    {
        if (_searchOverlay is not null)
        {
            _searchOverlay.PropertyChanged -= OnSearchOverlayPropertyChanged;
        }

        _searchOverlay = (DataContext as MainViewModel)?.SearchOverlay;
        if (_searchOverlay is not null)
        {
            _searchOverlay.PropertyChanged += OnSearchOverlayPropertyChanged;
        }
    }

    private void OnSearchOverlayPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchOverlayViewModel.FocusRequestToken))
        {
            FocusActiveQueryBox();
        }
    }

    private void FocusActiveQueryBox()
    {
        if (!IsVisible)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            var target = _searchOverlay?.IsAssistantMode == true
                ? (Control?)AssistantQueryBox
                : SearchQueryBox;
            if (target is null || !target.IsVisible || !target.IsEnabled)
            {
                return;
            }

            target.Focus();
            Keyboard.Focus(target);
            if (target is TextBox textBox)
            {
                textBox.CaretIndex = textBox.Text?.Length ?? 0;
            }
        }, DispatcherPriority.ContextIdle);
    }
}
