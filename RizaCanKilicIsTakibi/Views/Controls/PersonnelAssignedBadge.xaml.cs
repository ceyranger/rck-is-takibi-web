using System.Windows;
using System.Windows.Controls;

namespace RizaCanKilicIsTakibi.Views.Controls;

public enum PersonnelIconVariant
{
    SinglePerson = 0,
    TwoPeople = 1,
    CirclePerson = 2
}

public partial class PersonnelAssignedBadge : UserControl
{
    public static readonly DependencyProperty BadgeTextProperty =
        DependencyProperty.Register(
            nameof(BadgeText),
            typeof(string),
            typeof(PersonnelAssignedBadge),
            new PropertyMetadata(string.Empty, OnBadgeChanged));

    public static readonly DependencyProperty IconVariantProperty =
        DependencyProperty.Register(
            nameof(IconVariant),
            typeof(PersonnelIconVariant),
            typeof(PersonnelAssignedBadge),
            new PropertyMetadata(PersonnelIconVariant.TwoPeople, OnVariantChanged));

    public static readonly DependencyProperty ShowLabelProperty =
        DependencyProperty.Register(
            nameof(ShowLabel),
            typeof(bool),
            typeof(PersonnelAssignedBadge),
            new PropertyMetadata(true));

    public PersonnelAssignedBadge()
    {
        InitializeComponent();
        ApplyVariant();
        UpdateVisibility();
    }

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public PersonnelIconVariant IconVariant
    {
        get => (PersonnelIconVariant)GetValue(IconVariantProperty);
        set => SetValue(IconVariantProperty, value);
    }

    public bool ShowLabel
    {
        get => (bool)GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PersonnelAssignedBadge badge)
        {
            badge.UpdateVisibility();
        }
    }

    private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PersonnelAssignedBadge badge)
        {
            badge.ApplyVariant();
        }
    }

    private void UpdateVisibility()
        => Visibility = string.IsNullOrWhiteSpace(BadgeText) ? Visibility.Collapsed : Visibility.Visible;

    private void ApplyVariant()
    {
        if (IconPath is null)
        {
            return;
        }

        var key = IconVariant switch
        {
            PersonnelIconVariant.SinglePerson => "PersonnelAssignedIconStyleA",
            PersonnelIconVariant.CirclePerson => "PersonnelAssignedIconStyleC",
            _ => "PersonnelAssignedIconStyleB"
        };

        if (TryFindResource(key) is Style style)
        {
            IconPath.Style = style;
        }
    }
}
