using System.Windows.Controls;
using IpawsAlert.Wpf.ViewModels;

namespace IpawsAlert.Wpf.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    // PasswordBox doesn't support direct MVVM binding — handled in code-behind
    private void CertPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
            vm.SetCertPassword(pb.Password);
    }
}
