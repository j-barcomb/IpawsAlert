using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using IpawsAlert.Core.Builders;
using IpawsAlert.Core.Channels;
using IpawsAlert.Core.Client;
using IpawsAlert.Core.Models;
using IpawsAlert.Core.Validation;

namespace IpawsAlert.Wpf.ViewModels;

public sealed class ComposeViewModel : ViewModelBase
{
    // ── Events ────────────────────────────────────────────────────────────────
    public event EventHandler<SubmissionRecord>? AlertSubmitted;
    public event EventHandler<string>?           StatusChanged;

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 1 — Alert Classification
    // ═══════════════════════════════════════════════════════════════════════════

    // Status
    public IReadOnlyList<string> StatusOptions { get; } =
        Enum.GetNames<CapStatus>().ToList();
    private string _selectedStatus = CapStatus.Test.ToString();
    public string SelectedStatus
    {
        get => _selectedStatus;
        set { Set(ref _selectedStatus, value); RefreshXmlPreview(); Notify(nameof(IsActualMode)); }
    }
    public bool IsActualMode      => _selectedStatus == CapStatus.Actual.ToString();
    public bool IsUpdateOrCancel  => _selectedMsgType == CapMsgType.Update.ToString()
                                 || _selectedMsgType == CapMsgType.Cancel.ToString();

    // MsgType
    public IReadOnlyList<string> MsgTypeOptions { get; } =
        Enum.GetNames<CapMsgType>().ToList();
    private string _selectedMsgType = CapMsgType.Alert.ToString();
    public string SelectedMsgType
    {
        get => _selectedMsgType;
        set { Set(ref _selectedMsgType, value); RefreshXmlPreview(); Notify(nameof(IsUpdateOrCancel)); }
    }

    // Scope
    public IReadOnlyList<string> ScopeOptions { get; } =
        Enum.GetNames<CapScope>().ToList();
    private string _selectedScope = CapScope.Public.ToString();
    public string SelectedScope
    {
        get => _selectedScope;
        set { Set(ref _selectedScope, value); RefreshXmlPreview(); }
    }

    // References (for Update/Cancel)
    private string _referenceAlertId = string.Empty;
    public string ReferenceAlertId
    {
        get => _referenceAlertId;
        set { Set(ref _referenceAlertId, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 2 — Event Information
    // ═══════════════════════════════════════════════════════════════════════════

    public IReadOnlyList<string> CommonEventTypes { get; } = new[]
    {
        "Tornado Warning", "Tornado Watch",
        "Severe Thunderstorm Warning", "Severe Thunderstorm Watch",
        "Flash Flood Warning", "Flash Flood Watch",
        "Winter Storm Warning", "Winter Storm Watch",
        "Blizzard Warning",
        "Hurricane Warning", "Hurricane Watch",
        "Tsunami Warning", "Tsunami Watch",
        "Earthquake Warning",
        "Flood Warning", "Flood Watch",
        "Excessive Heat Warning", "Heat Advisory",
        "Wind Advisory", "High Wind Warning",
        "Frost/Freeze Warning",
        "Amber Alert",
        "Child Abduction Emergency",
        "Civil Emergency Message",
        "Evacuation Immediate",
        "Shelter in Place Warning",
        "Hazardous Materials Warning",
        "Nuclear Power Plant Warning",
        "Radiological Hazard Warning",
        "Law Enforcement Warning",
        "Blue Alert",
        "Presidential Alert",
        "Custom Event…"
    };

    private string _selectedEventType = "Tornado Warning";
    public string SelectedEventType
    {
        get => _selectedEventType;
        set
        {
            Set(ref _selectedEventType, value);
            if (value != "Custom Event…")
                CustomEventType = value;
            Notify(nameof(IsCustomEvent));
            RefreshXmlPreview();
        }
    }
    public bool IsCustomEvent => _selectedEventType == "Custom Event…";

    private string _customEventType = string.Empty;
    public string CustomEventType
    {
        get => _customEventType;
        set { Set(ref _customEventType, value); RefreshXmlPreview(); }
    }

    public string EffectiveEventType =>
        IsCustomEvent ? _customEventType : _selectedEventType;

    // Urgency / Severity / Certainty
    public IReadOnlyList<string> UrgencyOptions   { get; } = Enum.GetNames<CapUrgency>().ToList();
    public IReadOnlyList<string> SeverityOptions  { get; } = Enum.GetNames<CapSeverity>().ToList();
    public IReadOnlyList<string> CertaintyOptions { get; } = Enum.GetNames<CapCertainty>().ToList();

    private string _urgency   = CapUrgency.Immediate.ToString();
    private string _severity  = CapSeverity.Extreme.ToString();
    private string _certainty = CapCertainty.Observed.ToString();

    public string Urgency
    {
        get => _urgency;
        set { Set(ref _urgency, value); Notify(nameof(SeverityColor)); RefreshXmlPreview(); }
    }
    public string Severity
    {
        get => _severity;
        set { Set(ref _severity, value); Notify(nameof(SeverityColor)); RefreshXmlPreview(); }
    }
    public string Certainty
    {
        get => _certainty;
        set { Set(ref _certainty, value); RefreshXmlPreview(); }
    }

    /// <summary>Returns the hex color key name for the severity badge.</summary>
    public string SeverityColor => _severity switch
    {
        "Extreme"  => "#FF3B30",
        "Severe"   => "#FF9500",
        "Moderate" => "#FFD60A",
        "Minor"    => "#34C759",
        _          => "#8B949E"
    };

    // EAS event code
    private string _easEventCode = string.Empty;
    public string EasEventCode
    {
        get => _easEventCode;
        set { Set(ref _easEventCode, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 3 — Message Content
    // ═══════════════════════════════════════════════════════════════════════════

    private string _headline = string.Empty;
    public string Headline
    {
        get => _headline;
        set { Set(ref _headline, value); Notify(nameof(HeadlineCharCount)); RefreshXmlPreview(); }
    }
    public int HeadlineCharCount => _headline.Length;

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { Set(ref _description, value); RefreshXmlPreview(); }
    }

    private string _instruction = string.Empty;
    public string Instruction
    {
        get => _instruction;
        set { Set(ref _instruction, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 4 — WEA Channel
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _weaEnabled = true;
    public bool WeaEnabled
    {
        get => _weaEnabled;
        set { Set(ref _weaEnabled, value); RefreshXmlPreview(); }
    }

    private string _weaShortText = string.Empty;
    public string WeaShortText
    {
        get => _weaShortText;
        set { Set(ref _weaShortText, value); Notify(nameof(WeaShortCharCount)); Notify(nameof(WeaShortOverLimit)); RefreshXmlPreview(); }
    }
    public int  WeaShortCharCount => _weaShortText.Length;
    public bool WeaShortOverLimit => _weaShortText.Length > 90;

    private string _weaLongText = string.Empty;
    public string WeaLongText
    {
        get => _weaLongText;
        set { Set(ref _weaLongText, value); Notify(nameof(WeaLongCharCount)); Notify(nameof(WeaLongOverLimit)); RefreshXmlPreview(); }
    }
    public int  WeaLongCharCount => _weaLongText.Length;
    public bool WeaLongOverLimit => _weaLongText.Length > 360;

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 5 — EAS Channel
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _easEnabled = true;
    public bool EasEnabled
    {
        get => _easEnabled;
        set { Set(ref _easEnabled, value); RefreshXmlPreview(); }
    }

    private string _easOrgCode = "WXR";
    public string EasOrgCode
    {
        get => _easOrgCode;
        set { Set(ref _easOrgCode, value); RefreshXmlPreview(); }
    }

    private string _easCallsign = "IPAWS   ";
    public string EasCallsign
    {
        get => _easCallsign;
        set { Set(ref _easCallsign, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 6 — NWEM Channel
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _nwemEnabled = false;
    public bool NwemEnabled
    {
        get => _nwemEnabled;
        set { Set(ref _nwemEnabled, value); RefreshXmlPreview(); }
    }

    private string _vtecString = string.Empty;
    public string VtecString
    {
        get => _vtecString;
        set { Set(ref _vtecString, value); RefreshXmlPreview(); }
    }

    private string _ugcCodes = string.Empty;   // comma-separated
    public string UgcCodes
    {
        get => _ugcCodes;
        set { Set(ref _ugcCodes, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 7 — Geographic Area
    // ═══════════════════════════════════════════════════════════════════════════

    private string _areaDescription = string.Empty;
    public string AreaDescription
    {
        get => _areaDescription;
        set { Set(ref _areaDescription, value); RefreshXmlPreview(); }
    }

    // SAME codes — managed as an ObservableCollection
    public ObservableCollection<string> SameCodes { get; } = new();

    private string _sameCodeInput = string.Empty;
    public string SameCodeInput
    {
        get => _sameCodeInput;
        set => Set(ref _sameCodeInput, value);
    }

    public ICommand AddSameCodeCommand => new RelayCommand(() =>
    {
        var code = _sameCodeInput.Trim();
        if (code.Length == 6 && code.All(char.IsDigit) && !SameCodes.Contains(code))
        {
            SameCodes.Add(code);
            SameCodeInput = string.Empty;
            RefreshXmlPreview();
        }
    });

    public ICommand RemoveSameCodeCommand => new RelayCommand<string>(code =>
    {
        if (code is not null) { SameCodes.Remove(code); RefreshXmlPreview(); }
    });

    // Polygon input (raw "lat,lon lat,lon …")
    private string _polygonText = string.Empty;
    public string PolygonText
    {
        get => _polygonText;
        set { Set(ref _polygonText, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Section 8 — Timing
    // ═══════════════════════════════════════════════════════════════════════════

    private DateTime _effectiveDate = DateTime.Now;
    public DateTime EffectiveDate
    {
        get => _effectiveDate;
        set { Set(ref _effectiveDate, value); RefreshXmlPreview(); }
    }

    private DateTime _expiryDate = DateTime.Now.AddHours(1);
    public DateTime ExpiryDate
    {
        get => _expiryDate;
        set { Set(ref _expiryDate, value); RefreshXmlPreview(); }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════════════════

    private string _validationSummary = string.Empty;
    public string ValidationSummary
    {
        get => _validationSummary;
        private set { Set(ref _validationSummary, value); Notify(nameof(HasValidationResult)); }
    }

    private bool _hasValidationErrors;
    public bool HasValidationResult => !string.IsNullOrEmpty(_validationSummary);
    public bool HasValidationErrors
    {
        get => _hasValidationErrors;
        private set => Set(ref _hasValidationErrors, value);
    }

    private bool _hasValidationWarnings;
    public bool HasValidationWarnings
    {
        get => _hasValidationWarnings;
        private set => Set(ref _hasValidationWarnings, value);
    }

    public ICommand ValidateCommand => new RelayCommand(() =>
    {
        var alert = BuildAlert();
        var result = CapValidator.Validate(alert);
        UpdateValidationDisplay(result);
    });

    private void UpdateValidationDisplay(ValidationResult result)
    {
        HasValidationErrors   = !result.IsValid;
        HasValidationWarnings = result.Warnings.Any();

        if (result.Findings.Count == 0)
        {
            ValidationSummary = "✓  No issues found.";
            return;
        }

        var lines = result.Findings
            .Select(f => $"{(f.Severity == ValidationSeverity.Error ? "✗" : "⚠")} [{f.Code}] {f.Message}")
            .ToList();
        ValidationSummary = string.Join("\n", lines);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XML Preview
    // ═══════════════════════════════════════════════════════════════════════════

    private string _xmlPreview = string.Empty;
    public string XmlPreview
    {
        get => _xmlPreview;
        private set => Set(ref _xmlPreview, value);
    }

    private bool _showXmlPreview = true;
    public bool ShowXmlPreview
    {
        get => _showXmlPreview;
        set => Set(ref _showXmlPreview, value);
    }

    private void RefreshXmlPreview()
    {
        if (!_showXmlPreview) return;
        try
        {
            var alert = BuildAlert();
            XmlPreview = CapXmlSerializer.Serialize(alert, indent: true);
        }
        catch (Exception ex)
        {
            XmlPreview = $"<!-- Build error: {ex.Message} -->";
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Send / Submit
    // ═══════════════════════════════════════════════════════════════════════════

    private bool _isSending;
    public bool IsSending
    {
        get => _isSending;
        private set { Set(ref _isSending, value); Notify(nameof(CanSend)); }
    }

    public bool CanSend => !_isSending && !string.IsNullOrWhiteSpace(Headline);

    public ICommand SendCommand      => new AsyncRelayCommand(SendAsync, () => CanSend);
    public ICommand ClearFormCommand => new RelayCommand(ClearForm);

    private async Task SendAsync()
    {
        // Validate first
        var alert = BuildAlert();
        var validation = CapValidator.Validate(alert);
        UpdateValidationDisplay(validation);

        if (!validation.IsValid)
        {
            StatusChanged?.Invoke(this, "⚠ Validation errors — review before sending.");
            return;
        }

        // Confirmation dialog for Actual alerts
        var settings = AppSettings.Instance;
        bool isActual = _selectedStatus == CapStatus.Actual.ToString();

        if (isActual && settings.ConfirmBeforeSend)
        {
            var confirm = MessageBox.Show(
                $"You are about to submit a LIVE ACTUAL alert:\n\n" +
                $"  Event:    {EffectiveEventType}\n" +
                $"  Severity: {_severity}\n" +
                $"  Headline: {_headline}\n\n" +
                $"This will trigger real broadcasts to the public.\n" +
                $"Are you sure?",
                "Confirm LIVE Alert Submission",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;
        }

        IsSending = true;
        StatusChanged?.Invoke(this, "Submitting alert to IPAWS-OPEN…");

        string rawXml = CapXmlSerializer.Serialize(alert, indent: true);

        try
        {
            var config = BuildClientConfig(settings);
            using var client = new IpawsClient(config);
            var response = await client.SubmitAsync(alert);

            var channels = BuildChannelLabel();
            var record = SubmissionRecord.FromResponse(
                response, EffectiveEventType, _headline,
                _selectedStatus, channels, rawXml);

            AlertSubmitted?.Invoke(this, record);

            if (response.IsSuccess)
            {
                StatusChanged?.Invoke(this, $"✓ Alert accepted — Server ID: {response.ServerMessageId ?? "N/A"}");
                ClearForm();
            }
            else
            {
                StatusChanged?.Invoke(this, $"✗ Submission failed: {string.Join(", ", response.Errors)}");
            }
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"✗ Error: {ex.Message}");
        }
        finally
        {
            IsSending = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Form helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private CapAlert BuildAlert()
    {
        var s = AppSettings.Instance;

        var builder = new CapAlertBuilder()
            .WithSender(s.SenderAddress.Length > 0 ? s.SenderAddress : "test@ipaws.example.gov")
            .WithStatus(Enum.Parse<CapStatus>(_selectedStatus))
            .WithMsgType(Enum.Parse<CapMsgType>(_selectedMsgType))
            .WithScope(Enum.Parse<CapScope>(_selectedScope));

        if (!string.IsNullOrWhiteSpace(_referenceAlertId) &&
            _selectedMsgType is "Update" or "Cancel")
        {
            builder.AddReference(s.SenderAddress, _referenceAlertId, DateTimeOffset.UtcNow.AddDays(-1));
        }

        builder.AddInfo(info =>
        {
            info.WithCategory(CapCategory.Met)
                .WithEvent(EffectiveEventType)
                .WithUrgency(Enum.Parse<CapUrgency>(_urgency))
                .WithSeverity(Enum.Parse<CapSeverity>(_severity))
                .WithCertainty(Enum.Parse<CapCertainty>(_certainty))
                .WithEffective(new DateTimeOffset(_effectiveDate, TimeZoneInfo.Local.GetUtcOffset(_effectiveDate)))
                .WithExpiry(new DateTimeOffset(_expiryDate, TimeZoneInfo.Local.GetUtcOffset(_expiryDate)))
                .WithHeadline(_headline)
                .WithDescription(_description.Length > 0 ? _description : null!)
                .WithInstruction(_instruction.Length > 0 ? _instruction : null!);

            if (!string.IsNullOrWhiteSpace(s.SenderName))
                info.WithSenderName(s.SenderName);

            if (!string.IsNullOrWhiteSpace(_easEventCode))
                info.AddEventCode("SAME", _easEventCode);

            // Channel routing
            if (_weaEnabled)
                info.AddWeaRouting(
                    _weaShortText.Length > 0 ? _weaShortText : null,
                    _weaLongText.Length  > 0 ? _weaLongText  : null);
            if (_easEnabled)
                info.AddEasRouting();
            if (_nwemEnabled)
            {
                info.AddNwemRouting();
                if (_vtecString.Length > 0)
                    info.AddParameter("VTEC", _vtecString);
                foreach (var ugc in _ugcCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    info.AddArea(a => a.AddGeocode("UGC", ugc).WithDescription(ugc));
            }

            // Area
            if (SameCodes.Count > 0)
                info.AddArea(area =>
                {
                    area.WithDescription(_areaDescription.Length > 0 ? _areaDescription : "Alert Area");
                    foreach (var code in SameCodes)
                        area.AddSameCode(code);
                });
        });

        return builder.Build();
    }

    private static IpawsOpenConfig BuildClientConfig(AppSettings s)
    {
        var config = new IpawsOpenConfig
        {
            Endpoint = s.UseTestEndpoint
                ? IpawsOpenConfig.TestEndpoint
                : IpawsOpenConfig.ProductionEndpoint,
            CogId   = s.CogId,
        };

        if (s.UseCertStore)
            config.CertThumbprint = s.CertThumbprint;
        else
        {
            config.CertificatePath     = s.CertificatePath;
            config.CertificatePassword = s.CertificatePassword;
        }

        return config;
    }

    private string BuildChannelLabel()
    {
        var parts = new List<string>();
        if (_weaEnabled)  parts.Add("WEA");
        if (_easEnabled)  parts.Add("EAS");
        if (_nwemEnabled) parts.Add("NWEM");
        return string.Join(", ", parts);
    }

    public void ClearForm()
    {
        Headline        = string.Empty;
        Description     = string.Empty;
        Instruction     = string.Empty;
        WeaShortText    = string.Empty;
        WeaLongText     = string.Empty;
        SameCodes.Clear();
        PolygonText     = string.Empty;
        ValidationSummary = string.Empty;
        SelectedStatus  = CapStatus.Test.ToString();
        SelectedMsgType = CapMsgType.Alert.ToString();
        EffectiveDate   = DateTime.Now;
        ExpiryDate      = DateTime.Now.AddHours(1);
        RefreshXmlPreview();
    }
}
