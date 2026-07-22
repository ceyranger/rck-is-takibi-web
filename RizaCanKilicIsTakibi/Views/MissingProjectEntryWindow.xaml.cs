using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class MissingProjectEntryWindow : Window
{
    public MissingProjectEntryWindow(MissingProjectEntryDialogViewModel viewModel)
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
