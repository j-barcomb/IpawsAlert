using System.IO;
using System.Text.Json;

namespace IpawsAlert.Wpf.ViewModels;

/// <summary>
/// Persisted application settings stored in %AppData%\IpawsAlert\settings.json.
/// Covers connection config (cert path, COG ID, endpoint) and UI preferences.
/// </summary>
public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IpawsAlert",
        "settings.json");

    // Singleton
    public static AppSettings Instance { get; } = new();
    private AppSettings() { }

    // ── Connection ────────────────────────────────────────────────────────────
    public string CertificatePath     { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;  // encrypted at rest in future
    public string CertThumbprint      { get; set; } = string.Empty;
    public bool   UseCertStore        { get; set; } = false;
    public bool   UseTestEndpoint     { get; set; } = true;
    public string CogId               { get; set; } = string.Empty;
    public string SenderAddress       { get; set; } = string.Empty;
    public string SenderName          { get; set; } = string.Empty;

    // ── UI preferences ────────────────────────────────────────────────────────
    public string LastEventType       { get; set; } = string.Empty;
    public bool   ConfirmBeforeSend   { get; set; } = true;
    public bool   ShowXmlPreview      { get; set; } = true;
    public int    DefaultDurationHours { get; set; } = 1;

    // ── Persistence ───────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
            if (loaded is null) return;
            CertificatePath      = loaded.CertificatePath;
            CertificatePassword  = loaded.CertificatePassword;
            CertThumbprint       = loaded.CertThumbprint;
            UseCertStore         = loaded.UseCertStore;
            UseTestEndpoint      = loaded.UseTestEndpoint;
            CogId                = loaded.CogId;
            SenderAddress        = loaded.SenderAddress;
            SenderName           = loaded.SenderName;
            LastEventType        = loaded.LastEventType;
            ConfirmBeforeSend    = loaded.ConfirmBeforeSend;
            ShowXmlPreview       = loaded.ShowXmlPreview;
            DefaultDurationHours = loaded.DefaultDurationHours;
        }
        catch { /* first run or corrupt — use defaults */ }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch { /* best-effort */ }
    }
}
