namespace IpawsAlert.Core.Channels;

/// <summary>
/// Marker interface for channel-specific configuration objects.
/// Each channel has its own settings that affect how the CAP message
/// is parameterized before submission.
/// </summary>
public interface IChannelConfig
{
    /// <summary>Human-readable name of this channel.</summary>
    string ChannelName { get; }

    /// <summary>Whether this channel is enabled for the current alert.</summary>
    bool Enabled { get; }
}

// ═══════════════════════════════════════════════════════════════════════════
// WEA — Wireless Emergency Alerts
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Configuration for the WEA (Wireless Emergency Alerts / CMAS) channel.
///
/// WEA messages are broadcast to all cell phones in the targeted area.
/// The IPAWS-OPEN gateway extracts the CMAMtext / CMAMlongtext parameters
/// from the CAP &lt;info&gt; block to form the over-the-air message.
/// </summary>
public sealed class WeaChannelConfig : IChannelConfig
{
    public string ChannelName => "WEA";
    public bool   Enabled     { get; set; } = true;

    /// <summary>
    /// WEA alert classification:
    ///   Extreme + Immediate → Presidential or Extreme (e.g. Tornado Warning)
    ///   Severe  + Immediate → Severe (e.g. Flash Flood Warning)
    ///   Public Safety Message → AMBER, Imminent Threat, etc.
    /// IPAWS infers this from the CAP Severity/Urgency fields.
    /// </summary>

    /// <summary>
    /// Short (90-char legacy) WEA message text. Used for WEA 2.0.
    /// Leave null to use the CAP headline.
    /// FEMA maps this to the &lt;parameter&gt; CMAMtext.
    /// </summary>
    public string? ShortText { get; set; }

    /// <summary>
    /// Long (360-char) WEA message text for WEA 3.0 capable handsets.
    /// FEMA maps this to the &lt;parameter&gt; CMAMlongtext.
    /// </summary>
    public string? LongText { get; set; }

    /// <summary>
    /// If true, the alert includes embedded phone number / URL in the WEA message.
    /// Requires WEA 3.0 support.
    /// </summary>
    public bool IncludeEmbeddedLink { get; set; } = false;

    /// <summary>
    /// Telephone number to embed in the WEA message (WEA 3.0 only).
    /// </summary>
    public string? EmbeddedPhone { get; set; }

    /// <summary>
    /// URL to embed in the WEA message (WEA 3.0 only, max 23 chars after shortening).
    /// </summary>
    public Uri? EmbeddedUrl { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// EAS — Emergency Alert System
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Configuration for the EAS (Emergency Alert System) channel.
///
/// EAS distributes alerts to broadcast stations (TV, radio) via the
/// FEMA EAS Header and audio/visual interrupt. The CAP SAME geocodes
/// are used to target EAS distribution areas.
/// </summary>
public sealed class EasChannelConfig : IChannelConfig
{
    public string ChannelName => "EAS";
    public bool   Enabled     { get; set; } = true;

    /// <summary>
    /// EAS event code (3-character NWS/SAME code, e.g. "TOR", "SVR", "EVI").
    /// If null, IPAWS-OPEN maps from the CAP event string.
    /// Reference: https://www.nws.noaa.gov/directives/sym/pd01017012curr.pdf
    /// </summary>
    public string? EventCode { get; set; }

    /// <summary>
    /// Organization code for the EAS header (up to 8 chars, e.g. "WXR", "CIV").
    /// </summary>
    public string OrgCode { get; set; } = "WXR";

    /// <summary>
    /// FIPS SAME location codes for EAS targeting (6-digit strings).
    /// These are automatically pulled from CAP &lt;area&gt; SAME geocodes.
    /// Override here only if EAS targeting differs from the general area.
    /// </summary>
    public List<string> SameOverrideCodes { get; set; } = new();

    /// <summary>
    /// Purge time for the EAS header in format "HHNN" (hours + minutes).
    /// e.g. "0100" = 1 hour, "0030" = 30 minutes.
    /// If null, derived from the CAP expires field.
    /// </summary>
    public string? PurgeTime { get; set; }

    /// <summary>
    /// Callsign of the originating station (up to 8 chars, e.g. "KNBC    ").
    /// Padded to 8 characters with spaces in the EAS header.
    /// </summary>
    public string OriginatorCallsign { get; set; } = "IPAWS   ";
}

// ═══════════════════════════════════════════════════════════════════════════
// NWEM — National Weather Emergency Messages
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Configuration for the NWEM (National Warning System – Emergency Messages) channel.
///
/// NWEM distributes alerts through NOAA Weather Radio and the NWS.
/// Appropriate for weather-originated emergencies.
/// </summary>
public sealed class NwemChannelConfig : IChannelConfig
{
    public string ChannelName => "NWEM";
    public bool   Enabled     { get; set; } = true;

    /// <summary>
    /// NWS VTEC (Valid Time Event Code) string.
    /// Example: "/O.NEW.KGRR.TO.W.0001.240601T1800Z-240601T2000Z/"
    /// Required for NWS products that use VTEC.
    /// </summary>
    public string? VtecString { get; set; }

    /// <summary>
    /// NWS Hydrologic VTEC string (H-VTEC). Optional.
    /// Used for hydrological events (floods, etc.).
    /// </summary>
    public string? HvtecString { get; set; }

    /// <summary>
    /// NWS Universal Geographic Code (UGC) strings identifying forecast zones.
    /// Examples: "OHZ001", "OHC001"
    /// </summary>
    public List<string> UgcCodes { get; set; } = new();

    /// <summary>
    /// NWS product identifier / PIL (e.g. "TORGRR" for Tornado Warning from GRR).
    /// </summary>
    public string? ProductId { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════
// IPAWS-OPEN — CAP over HTTPS endpoint config
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Connection settings for the IPAWS-OPEN HTTPS endpoint.
/// Authentication uses mutual TLS (mTLS) with the FEMA-issued certificate.
/// </summary>
public sealed class IpawsOpenConfig : IChannelConfig
{
    public string ChannelName => "IPAWS-OPEN";
    public bool   Enabled     { get; set; } = true;

    // ── Endpoint URLs ─────────────────────────────────────────────────────────

    /// <summary>IPAWS-OPEN testing/integration endpoint (JITC).</summary>
    public static readonly Uri TestEndpoint =
        new("https://tdl.integration.aws.fema.net/cap/SubmitCAPMessage");

    /// <summary>IPAWS-OPEN production endpoint.</summary>
    public static readonly Uri ProductionEndpoint =
        new("https://www.fema.gov/cap/COGProfile.do");

    /// <summary>
    /// The endpoint to POST CAP messages to.
    /// Default: <see cref="TestEndpoint"/>.
    /// </summary>
    public Uri Endpoint { get; set; } = TestEndpoint;

    // ── mTLS Certificate ──────────────────────────────────────────────────────

    /// <summary>
    /// Path to the PKCS#12 (.p12 / .pfx) certificate file issued by FEMA.
    /// Used for mutual TLS authentication.
    /// </summary>
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Password for the PKCS#12 certificate file.
    /// Store securely — do not hardcode in source.
    /// </summary>
    public string? CertificatePassword { get; set; }

    /// <summary>
    /// Certificate thumbprint for looking up a cert from the Windows Certificate Store.
    /// Alternative to <see cref="CertificatePath"/>. Takes precedence if both are set.
    /// Store name: <see cref="CertStoreLocation"/> / Personal.
    /// </summary>
    public string? CertThumbprint { get; set; }

    /// <summary>
    /// Windows certificate store location when using <see cref="CertThumbprint"/>.
    /// Default: CurrentUser.
    /// </summary>
    public System.Security.Cryptography.X509Certificates.StoreLocation CertStoreLocation
        { get; set; } = System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser;

    // ── HTTP settings ─────────────────────────────────────────────────────────

    /// <summary>HTTP request timeout. Default: 30 seconds.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Maximum number of automatic retries on transient failures.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Delay between retries.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// COG (Collaborative Operating Group) identifier issued by FEMA.
    /// Included in the X-IPAWS-CogId header if set.
    /// </summary>
    public string? CogId { get; set; }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>Returns true if this config has enough info to attempt a connection.</summary>
    public bool IsConfigured =>
        Endpoint is not null &&
        (!string.IsNullOrWhiteSpace(CertificatePath) || !string.IsNullOrWhiteSpace(CertThumbprint));
}
