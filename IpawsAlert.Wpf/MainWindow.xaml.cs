using System.Windows;
using IpawsAlert.Wpf.ViewModels;

namespace IpawsAlert.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
