using RizaCanKilicIsTakibi.ViewModels;
using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class QuickTaskTemplateWindow : Window
{
    public QuickTaskTemplateWindow(QuickTaskTemplateDialogViewModel viewModel)
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
