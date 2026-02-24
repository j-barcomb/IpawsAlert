using System.Windows.Input;

namespace IpawsAlert.Wpf.ViewModels;

public enum AppPage { Compose, History, Settings }

public sealed class MainViewModel : ViewModelBase
{
    // ── Child ViewModels ──────────────────────────────────────────────────────
    public ComposeViewModel  Compose  { get; } = new();
    public HistoryViewModel  History  { get; } = new();
    public SettingsViewModel Settings { get; } = new();

    // ── Navigation ────────────────────────────────────────────────────────────
    private AppPage _currentPage = AppPage.Compose;
    public AppPage CurrentPage
    {
        get => _currentPage;
        set
        {
            Set(ref _currentPage, value);
            Notify(nameof(IsComposePage));
            Notify(nameof(IsHistoryPage));
            Notify(nameof(IsSettingsPage));
            Notify(nameof(CurrentViewModel));
        }
    }

    public bool IsComposePage  => CurrentPage == AppPage.Compose;
    public bool IsHistoryPage  => CurrentPage == AppPage.History;
    public bool IsSettingsPage => CurrentPage == AppPage.Settings;

    public ViewModelBase CurrentViewModel => CurrentPage switch
    {
        AppPage.Compose  => Compose,
        AppPage.History  => History,
        AppPage.Settings => Settings,
        _                => Compose
    };

    // ── Commands ──────────────────────────────────────────────────────────────
    public ICommand GoToComposeCommand  => new RelayCommand(() => CurrentPage = AppPage.Compose);
    public ICommand GoToHistoryCommand  => new RelayCommand(() => CurrentPage = AppPage.History);
    public ICommand GoToSettingsCommand => new RelayCommand(() => CurrentPage = AppPage.Settings);

    // ── Status bar ────────────────────────────────────────────────────────────
    private string _statusText = "Ready";
    public  string StatusText
    {
        get => _statusText;
        set => Set(ref _statusText, value);
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set { Set(ref _isConnected, value); Notify(nameof(ConnectionLabel)); }
    }
    public string ConnectionLabel => IsConnected ? "CONNECTED" : "NOT CONNECTED";

    // ── Wire up child events ──────────────────────────────────────────────────
    public MainViewModel()
    {
        Compose.AlertSubmitted += (_, result) =>
        {
            History.AddEntry(result);
            CurrentPage = AppPage.History;
        };

        Compose.StatusChanged += (_, msg) => StatusText = msg;
        Settings.SettingsSaved += (_, _) => RefreshConnectionStatus();

        RefreshConnectionStatus();
    }

    private void RefreshConnectionStatus()
    {
        var s = AppSettings.Instance;
        IsConnected = !string.IsNullOrWhiteSpace(s.CogId) &&
                      (!string.IsNullOrWhiteSpace(s.CertificatePath) ||
                       !string.IsNullOrWhiteSpace(s.CertThumbprint));
    }
}
