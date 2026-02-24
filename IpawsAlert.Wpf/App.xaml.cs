using System.Windows;
using IpawsAlert.Wpf.ViewModels;

namespace IpawsAlert.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load persisted settings before the window opens
        AppSettings.Instance.Load();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppSettings.Instance.Save();
        base.OnExit(e);
    }
}