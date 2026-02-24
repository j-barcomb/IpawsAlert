using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using IpawsAlert.Core.Channels;

namespace IpawsAlert.Wpf.ViewModels;

// ═══════════════════════════════════════════════════════════════════════════════
// HistoryViewModel
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class HistoryViewModel : ViewModelBase
{
    public ObservableCollection<SubmissionRecord> Entries { get; } = new();

    private SubmissionRecord? _selected;
    public SubmissionRecord? Selected
    {
        get => _selected;
        set
        {
            Set(ref _selected, value);
            Notify(nameof(HasSelection));
            Notify(nameof(SelectedXml));
            Notify(nameof(SelectedErrors));
            Notify(nameof(SelectedHasError));
        }
    }

    public bool   HasSelection   => _selected is not null;
    public bool   HasNoEntries   => Entries.Count == 0;
    public bool   SelectedHasError => _selected is not null && !_selected.IsSuccess;
    public string SelectedXml    => _selected?.RawCapXml ?? string.Empty;
    public string SelectedErrors => _selected?.ErrorSummary ?? "No errors.";

    public ICommand ClearHistoryCommand => new RelayCommand(() =>
    {
        Entries.Clear();
        Selected = null;
    }, () => Entries.Count > 0);

    public ICommand CopyXmlCommand => new RelayCommand(() =>
    {
        if (_selected is not null)
            Clipboard.SetText(_selected.RawCapXml);
    }, () => _selected is not null);

    internal void AddEntry(SubmissionRecord record)
    {
        Entries.Insert(0, record);  // newest first
        Notify(nameof(HasNoEntries));
        Selected = record;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// SettingsViewModel
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class SettingsViewModel : ViewModelBase
{
    public event EventHandler? SettingsSaved;

    // ── Connection ────────────────────────────────────────────────────────────

    private string _cogId;
    public string CogId
    {
        get => _cogId;
        set => Set(ref _cogId, value);
    }

    private string _senderAddress;
    public string SenderAddress
    {
        get => _senderAddress;
        set => Set(ref _senderAddress, value);
    }

    private string _senderName;
    public string SenderName
    {
        get => _senderName;
        set => Set(ref _senderName, value);
    }

    private bool _useTestEndpoint;
    public bool UseTestEndpoint
    {
        get => _useTestEndpoint;
        set
        {
            Set(ref _useTestEndpoint, value);
            Notify(nameof(ActiveEndpointLabel));
        }
    }

    public string ActiveEndpointLabel => _useTestEndpoint
        ? IpawsOpenConfig.TestEndpoint.ToString()
        : IpawsOpenConfig.ProductionEndpoint.ToString();

    // ── Certificate ───────────────────────────────────────────────────────────

    private bool _useCertStore;
    public bool UseCertStore
    {
        get => _useCertStore;
        set { Set(ref _useCertStore, value); Notify(nameof(UseFileMode)); }
    }
    public bool UseFileMode
    {
        get => !_useCertStore;
        set { _useCertStore = !value; Notify(nameof(UseCertStore)); Notify(nameof(UseFileMode)); }
    }

    private string _certPath;
    public string CertPath
    {
        get => _certPath;
        set => Set(ref _certPath, value);
    }

    // CertPassword is handled by PasswordBox in code-behind (not bindable for security)
    public string CertPasswordPlaceholder => "••••••••";

    private string _certThumbprint;
    public string CertThumbprint
    {
        get => _certThumbprint;
        set => Set(ref _certThumbprint, value);
    }

    // ── UI Preferences ────────────────────────────────────────────────────────

    private bool _confirmBeforeSend;
    public bool ConfirmBeforeSend
    {
        get => _confirmBeforeSend;
        set => Set(ref _confirmBeforeSend, value);
    }

    private bool _showXmlPreview;
    public bool ShowXmlPreview
    {
        get => _showXmlPreview;
        set => Set(ref _showXmlPreview, value);
    }

    // ── Status ────────────────────────────────────────────────────────────────

    private string _saveStatus = string.Empty;
    public string SaveStatus
    {
        get => _saveStatus;
        set => Set(ref _saveStatus, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand SaveCommand => new RelayCommand(Save);

    public ICommand BrowseCertCommand => new RelayCommand(() =>
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select FEMA IPAWS Certificate",
            Filter = "PKCS#12 Certificate|*.p12;*.pfx|All Files|*.*"
        };
        if (dlg.ShowDialog() == true)
            CertPath = dlg.FileName;
    });

    // ── Init ──────────────────────────────────────────────────────────────────

    public SettingsViewModel()
    {
        var s = AppSettings.Instance;
        _cogId            = s.CogId;
        _senderAddress    = s.SenderAddress;
        _senderName       = s.SenderName;
        _useTestEndpoint  = s.UseTestEndpoint;
        _useCertStore     = s.UseCertStore;
        _certPath         = s.CertificatePath;
        _certThumbprint   = s.CertThumbprint;
        _confirmBeforeSend = s.ConfirmBeforeSend;
        _showXmlPreview   = s.ShowXmlPreview;
    }

    private void Save()
    {
        var s = AppSettings.Instance;
        s.CogId             = _cogId.Trim();
        s.SenderAddress     = _senderAddress.Trim();
        s.SenderName        = _senderName.Trim();
        s.UseTestEndpoint   = _useTestEndpoint;
        s.UseCertStore      = _useCertStore;
        s.CertificatePath   = _certPath.Trim();
        s.CertThumbprint    = _certThumbprint.Trim();
        s.ConfirmBeforeSend = _confirmBeforeSend;
        s.ShowXmlPreview    = _showXmlPreview;
        s.Save();

        SaveStatus = "✓  Settings saved.";
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    // Password is stored via code-behind, not ViewModel binding
    public void SetCertPassword(string password)
    {
        AppSettings.Instance.CertificatePassword = password;
    }
}
