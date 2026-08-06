using System.Windows;

namespace RizaCanKilicIsTakibi.Views;

public partial class TaskDetailWindow : Window
{
    public TaskDetailWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
