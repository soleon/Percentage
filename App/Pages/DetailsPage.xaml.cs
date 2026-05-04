using System.Windows;
using Percentage.App.Extensions;

namespace Percentage.App.Pages;

public partial class DetailsPage
{
    public DetailsPage()
    {
        InitializeComponent();
    }

    private void OnRefreshButtonClick(object sender, RoutedEventArgs e)
    {
        BatteryInformation.RequestUpdate();
        Application.Current.GetNotifyIconWindow().RequestBatteryStatusUpdate();
    }
}
