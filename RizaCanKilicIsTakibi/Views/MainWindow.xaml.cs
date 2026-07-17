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
            return;
        }

        var result = MessageBox.Show(
            "Kaydedilmemiş değişiklikler var. Çıkmadan önce kaydetmek ister misiniz?",
            "Çıkış Onayı",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (result == MessageBoxResult.Yes)
        {
            e.Cancel = true;
            _isSavingOnClose = true;
            try
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

                _allowCloseWithoutPrompt = true;
                _ = Dispatcher.BeginInvoke(new Action(Close), DispatcherPriority.Normal);
                return;
            }
            finally
            {
                _isSavingOnClose = false;
            }
        }

        _allowCloseWithoutPrompt = true;
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
