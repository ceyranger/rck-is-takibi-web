using RizaCanKilicIsTakibi.ViewModels;
using RizaCanKilicIsTakibi.Helpers;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _allowCloseWithoutPrompt;
    private bool _isSavingOnClose;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        ConfigureColumnFilters();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowCloseWithoutPrompt || _isSavingOnClose || (Application.Current as App)?.IsFatalShutdownRequested == true)
        {
            return;
        }

        PendingEditCommitHelper.FlushFocusedEditor();

        if (!_viewModel.HasAnyUnsavedChanges)
        {
            _viewModel.ClearSessionRecoveryArtifacts();
            return;
        }

        // Write recovery BEFORE the confirmation dialog so Task Manager / End Task
        // during the prompt still leaves a recoverable snapshot.
        e.Cancel = true;
        _isSavingOnClose = true;
        try
        {
            await _viewModel.FlushSessionRecoveryAsync();

            var result = MessageBox.Show(
                "Kaydedilmemiş değişiklikler var. Çıkmadan önce kaydetmek ister misiniz?",
                "Çıkış Onayı",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                var saved = await _viewModel.SaveAllTabsSafelyAsync();
                if (!saved)
                {
                    MessageBox.Show(
                        "Bazı değişiklikler kaydedilemedi. Lütfen tekrar deneyin.",
                        "Kaydetme Hatası",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                _viewModel.ClearSessionRecoveryArtifacts();
                _allowCloseWithoutPrompt = true;
                _ = Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.Normal);
                return;
            }

            // Explicit discard: user chose not to keep unsaved work.
            _viewModel.ClearSessionRecoveryArtifacts();
            _allowCloseWithoutPrompt = true;
            _ = Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.Normal);
        }
        finally
        {
            _isSavingOnClose = false;
        }
    }

    private void ConfigureColumnFilters()
    {
        ConfigureTaskBoardFilters(UrgentGrid, _viewModel.UrgentBoard);
        ConfigureTaskBoardFilters(GeneralGrid, _viewModel.GeneralBoard);
    }

    private static void ConfigureTaskBoardFilters(DataGrid grid, TaskBoardViewModel board)
    {
        // Column 0 = drag handle, Column 1 = title (selectable display / editable).
        if (grid.Columns.Count < 2)
        {
            return;
        }

        grid.Columns[1].Header = board.TitleColumnFilter;
    }
}
