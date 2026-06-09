using RizaCanKilicIsTakibi.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Views.Sections;

public partial class YibfIsTakibiSectionView : UserControl
{
    private bool _initialBottomScrollApplied;
    private YibfModuleViewModel? _module;

    public YibfIsTakibiSectionView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        HookModuleSubscriptions();
        if (!TryScrollToPendingTarget())
        {
            TryScrollToBottom(initialOnly: true);
        }
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            HookModuleSubscriptions();
            if (!TryScrollToPendingTarget())
            {
                TryScrollToBottom(initialOnly: true);
            }
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UnhookModuleSubscriptions();
        _initialBottomScrollApplied = false;
        HookModuleSubscriptions();
        if (!TryScrollToPendingTarget())
        {
            TryScrollToBottom(initialOnly: true);
        }
    }

    private void HookModuleSubscriptions()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (ReferenceEquals(_module, vm.YibfModule))
        {
            return;
        }

        UnhookModuleSubscriptions();
        _module = vm.YibfModule;
        _module.IsTakibiRows.CollectionChanged += OnIsTakibiRowsCollectionChanged;
        _module.PropertyChanged += OnYibfModulePropertyChanged;
    }

    private void UnhookModuleSubscriptions()
    {
        if (_module is null)
        {
            return;
        }

        _module.IsTakibiRows.CollectionChanged -= OnIsTakibiRowsCollectionChanged;
        _module.PropertyChanged -= OnYibfModulePropertyChanged;
        _module = null;
    }

    private void OnYibfModulePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(YibfModuleViewModel.PendingIsTakibiScrollTargetId) ||
            e.PropertyName == nameof(YibfModuleViewModel.SelectedIsTakibiEntry))
        {
            TryScrollToPendingTarget();
        }
    }

    private void OnIsTakibiRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (TryScrollToPendingTarget())
        {
            return;
        }

        if (!_initialBottomScrollApplied || e.Action == NotifyCollectionChangedAction.Add)
        {
            TryScrollToBottom(initialOnly: false);
        }
    }

    private bool TryScrollToPendingTarget()
    {
        if (_module?.PendingIsTakibiScrollTargetId is not Guid targetId)
        {
            return false;
        }

        var targetRow = _module.IsTakibiRows.FirstOrDefault(row => row.Entry.Id == targetId);
        if (targetRow is null)
        {
            return false;
        }

        Dispatcher.BeginInvoke(() =>
        {
            YibfIsTakibiListBox.UpdateLayout();
            YibfIsTakibiListBox.ScrollIntoView(targetRow);
            _initialBottomScrollApplied = true;
            _module?.ClearPendingIsTakibiScrollTarget();
        }, DispatcherPriority.Loaded);

        return true;
    }

    private void TryScrollToBottom(bool initialOnly)
    {
        if (initialOnly && _initialBottomScrollApplied)
        {
            return;
        }

        if (YibfIsTakibiListBox.Items.Count == 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (YibfIsTakibiListBox.Items.Count == 0)
            {
                return;
            }

            var lastItem = YibfIsTakibiListBox.Items[YibfIsTakibiListBox.Items.Count - 1];
            YibfIsTakibiListBox.ScrollIntoView(lastItem);
            _initialBottomScrollApplied = true;
        }, DispatcherPriority.Loaded);
    }
}
