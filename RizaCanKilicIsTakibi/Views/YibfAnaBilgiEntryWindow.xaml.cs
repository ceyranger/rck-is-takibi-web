using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class YibfAnaBilgiEntryWindow : Window
{
    public YibfAnaBilgiEntryWindow(YibfAnaBilgiEntryDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += (_, result) =>
        {
            DialogResult = result is not null;
            Close();
        };
    }
}