using RizaCanKilicIsTakibi.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RizaCanKilicIsTakibi.Behaviors;

public static class DataGridDragDropBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridDragDropBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty MoveTaskCommandProperty =
        DependencyProperty.RegisterAttached(
            "MoveTaskCommand",
            typeof(ICommand),
            typeof(DataGridDragDropBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TargetBoardTypeProperty =
        DependencyProperty.RegisterAttached(
            "TargetBoardType",
            typeof(TaskBoardType),
            typeof(DataGridDragDropBehavior),
            new PropertyMetadata(TaskBoardType.Genel));

    private static Point _dragStartPoint;

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ICommand? GetMoveTaskCommand(DependencyObject obj) => (ICommand?)obj.GetValue(MoveTaskCommandProperty);

    public static void SetMoveTaskCommand(DependencyObject obj, ICommand? value) => obj.SetValue(MoveTaskCommandProperty, value);

    public static TaskBoardType GetTargetBoardType(DependencyObject obj) => (TaskBoardType)obj.GetValue(TargetBoardTypeProperty);

    public static void SetTargetBoardType(DependencyObject obj, TaskBoardType value) => obj.SetValue(TargetBoardTypeProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid dataGrid)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            dataGrid.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            dataGrid.PreviewMouseMove += OnPreviewMouseMove;
            dataGrid.DragOver += OnDragOver;
            dataGrid.Drop += OnDrop;
            dataGrid.AllowDrop = true;
        }
        else
        {
            dataGrid.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            dataGrid.PreviewMouseMove -= OnPreviewMouseMove;
            dataGrid.DragOver -= OnDragOver;
            dataGrid.Drop -= OnDrop;
        }
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DataGrid dataGrid || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(null);
        var diff = _dragStartPoint - position;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row?.Item is not TaskItem task)
        {
            return;
        }

        DragDrop.DoDragDrop(dataGrid, task, DragDropEffects.Move);
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(TaskItem)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not DataGrid dataGrid)
        {
            return;
        }

        if (!e.Data.GetDataPresent(typeof(TaskItem)))
        {
            return;
        }

        var task = (TaskItem)e.Data.GetData(typeof(TaskItem));
        var command = GetMoveTaskCommand(dataGrid);
        var targetBoard = GetTargetBoardType(dataGrid);

        if (command is null)
        {
            return;
        }

        var request = new DragDropTaskMoveRequest
        {
            Task = task,
            TargetBoard = targetBoard
        };

        if (command.CanExecute(request))
        {
            command.Execute(request);
        }

        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T expected)
            {
                return expected;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
