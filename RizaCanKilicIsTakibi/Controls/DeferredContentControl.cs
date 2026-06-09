using System.Windows;
using System.Windows.Controls;

namespace RizaCanKilicIsTakibi.Controls;

public sealed class DeferredContentControl : ContentControl
{
    public static readonly DependencyProperty DeferredTemplateProperty =
        DependencyProperty.Register(
            nameof(DeferredTemplate),
            typeof(DataTemplate),
            typeof(DeferredContentControl),
            new PropertyMetadata(null, OnDeferredPropertyChanged));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(DeferredContentControl),
            new PropertyMetadata(false, OnDeferredPropertyChanged));

    public static readonly DependencyProperty UnloadWhenInactiveProperty =
        DependencyProperty.Register(
            nameof(UnloadWhenInactive),
            typeof(bool),
            typeof(DeferredContentControl),
            new PropertyMetadata(false, OnDeferredPropertyChanged));

    public DataTemplate? DeferredTemplate
    {
        get => (DataTemplate?)GetValue(DeferredTemplateProperty);
        set => SetValue(DeferredTemplateProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool UnloadWhenInactive
    {
        get => (bool)GetValue(UnloadWhenInactiveProperty);
        set => SetValue(UnloadWhenInactiveProperty, value);
    }

    private static void OnDeferredPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DeferredContentControl control)
        {
            control.TryLoadContent();
        }
    }

    private void TryLoadContent()
    {
        if (!IsActive)
        {
            if (UnloadWhenInactive && (Content is not null || ContentTemplate is not null))
            {
                Content = null;
                ContentTemplate = null;
            }

            return;
        }

        if (ContentTemplate is not null || DeferredTemplate is null)
        {
            return;
        }

        Content = DataContext ?? this;
        ContentTemplate = DeferredTemplate;
    }
}
