using StageManagementSystem.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StageManagementSystem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _isDarkTheme = false;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        
        var themeDict = new ResourceDictionary
        {
            Source = new Uri($"Themes/{(_isDarkTheme ? "Dark" : "Light")}.xaml", UriKind.Relative)
        };

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(themeDict);
        
        if (sender is Button btn)
        {
            btn.Content = _isDarkTheme ? "☀️" : "🌙";
            btn.ToolTip = _isDarkTheme ? "Toggle Light Mode" : "Toggle Dark Mode";
        }
    }
}