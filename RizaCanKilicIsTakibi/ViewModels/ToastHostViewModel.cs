using RizaCanKilicIsTakibi.Helpers;
using RizaCanKilicIsTakibi.Models;
using RizaCanKilicIsTakibi.Services.Abstractions;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace RizaCanKilicIsTakibi.ViewModels;

public sealed class ToastHostViewModel : ViewModelBase
{
    private readonly Dispatcher _dispatcher;

    public ToastHostViewModel(INotificationService notificationService)
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        Toasts = new ObservableCollection<ToastMessage>();
        notificationService.ToastRequested += OnToastRequested;
        notificationService.ToastActionInvoked += OnToastActionInvoked;
    }

    public ObservableCollection<ToastMessage> Toasts { get; }

    private void OnToastRequested(object? sender, ToastMessage toast)
    {
        if (!_dispatcher.CheckAccess())
        {
            _ = _dispatcher.BeginInvoke(new Action(() => OnToastRequested(sender, toast)));
            return;
        }

        Toasts.Add(toast);

        var timer = new DispatcherTimer
        {
            Interval = toast.Duration
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Toasts.Remove(toast);
        };

        timer.Start();
    }

    private void OnToastActionInvoked(object? sender, ToastMessage toast)
    {
        if (!_dispatcher.CheckAccess())
        {
            _ = _dispatcher.BeginInvoke(new Action(() => OnToastActionInvoked(sender, toast)));
            return;
        }

        Toasts.Remove(toast);
    }
}
