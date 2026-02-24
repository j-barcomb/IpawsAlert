using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IpawsAlert.Wpf.ViewModels;

namespace IpawsAlert.Wpf.Views;

public partial class ComposeView : UserControl
{
    public ComposeView()
    {
        InitializeComponent();
    }

    private void CopyXmlButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ComposeViewModel vm && !string.IsNullOrWhiteSpace(vm.XmlPreview))
            Clipboard.SetText(vm.XmlPreview);
    }
}
