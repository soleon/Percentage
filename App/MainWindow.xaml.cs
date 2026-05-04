using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace Percentage.App;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
        App.SnackBarService.SetSnackbarPresenter(SnackbarPresenter);
    }

    internal void NavigateToPage<T>() where T : Page => NavigationView.Navigate(typeof(T));
}
