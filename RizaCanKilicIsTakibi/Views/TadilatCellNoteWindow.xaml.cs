using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class TadilatCellNoteWindow : Window
{
    public TadilatCellNoteWindow(TadilatCellNoteDialogViewModel viewModel)
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
