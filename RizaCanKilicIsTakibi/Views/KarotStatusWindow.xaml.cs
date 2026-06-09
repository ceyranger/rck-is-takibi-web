using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class KarotStatusWindow : Window
{
    public KarotStatusWindow(KarotStatusDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.RequestClose += (_, result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
