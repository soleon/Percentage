using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Percentage.App.Extensions;

namespace Percentage.App.Pages;

public sealed partial class AboutPage : INotifyPropertyChanged
{
    public AboutPage()
    {
        InitializeComponent();

        Loaded += (_, _) => App.TrayIconUpdateErrorSet += OnTrayIconUpdateErrorSet;

        Unloaded += (_, _) => App.TrayIconUpdateErrorSet -= OnTrayIconUpdateErrorSet;
    }

    public static Exception? TrayIconUpdateError => App.GetTrayIconUpdateError();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnDonationButtonClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.OpenDonationLocation();
    }

    private void OnFeedbackButtonClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.OpenFeedbackLocation();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnRatingButtonClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.ShowRatingView();
    }

    private void OnSourceCodeButtonClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.OpenSourceCodeLocation();
    }

    private void OnTrayIconUpdateErrorSet(Exception _)
    {
        OnPropertyChanged(nameof(TrayIconUpdateError));
    }
}