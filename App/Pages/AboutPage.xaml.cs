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

        Loaded += (_, _) => App.AppErrorSet += OnAppErrorSet;

        Unloaded += (_, _) => App.AppErrorSet -= OnAppErrorSet;
    }

    public static Exception? AppError => App.GetAppError();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnAppErrorSet(Exception _)
    {
        OnPropertyChanged(nameof(AppError));
    }

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

    private void OnGitHubIssuesLinkClick(object sender, RoutedEventArgs e)
    {
        ExternalProcessExtensions.OpenGitHubIssuesLocation();
    }
}