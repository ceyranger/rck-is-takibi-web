using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class YibfAnaBilgiEventWindow : Window
{
    public YibfAnaBilgiEventWindow(YibfAnaBilgiEventDialogViewModel viewModel)
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
