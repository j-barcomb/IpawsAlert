namespace IpawsAlert.Core.Models;

/// <summary>
/// Root CAP v1.2 &lt;alert&gt; message.
/// Spec reference: http://docs.oasis-open.org/emergency/cap/v1.2/CAP-v1.2-os.html
/// </summary>
public sealed class CapAlert
{
    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Globally unique message identifier.
    /// Convention: {sender-domain},{YYYY}-{sequence} — must never be reused.
    /// If null, the builder will generate one automatically.
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Identifier of the originating system or agency.
    /// For IPAWS, this is your COG Identifier (e.g. "w-nws.webmaster@noaa.gov").
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>Time the message was issued. Defaults to UtcNow when built.</summary>
    public DateTimeOffset Sent { get; set; } = DateTimeOffset.UtcNow;

    // ── Classification ────────────────────────────────────────────────────────

    public CapStatus  Status  { get; set; } = CapStatus.Test;
    public CapMsgType MsgType { get; set; } = CapMsgType.Alert;
    public CapScope   Scope   { get; set; } = CapScope.Public;

    // ── Reference linkage ─────────────────────────────────────────────────────

    /// <summary>
    /// For Update or Cancel messages: references to the prior message(s) being
    /// superseded. Format per CAP spec: "{sender},{identifier},{sent}" per item.
    /// </summary>
    public List<string> References { get; set; } = new();

    /// <summary>
    /// For Alert and Update: identifiers of related but independent messages
    /// (same event, different area). Format: "{sender},{identifier},{sent}" per item.
    /// </summary>
    public List<string> Incidents { get; set; } = new();

    // ── Restriction / addresses ───────────────────────────────────────────────

    /// <summary>
    /// Required when Scope = Restricted. A rule for limiting redistribution.
    /// </summary>
    public string? Restriction { get; set; }

    /// <summary>
    /// Required when Scope = Private. Space-delimited list of intended recipients.
    /// </summary>
    public string? Addresses { get; set; }

    /// <summary>
    /// Codes denoting special handling of the message.
    /// IPAWS routing codes go here (e.g. "IPAWSv1.0").
    /// </summary>
    public List<string> Codes { get; set; } = new();

    /// <summary>Text describing the purpose of the alert message.</summary>
    public string? Note { get; set; }

    // ── Info blocks ───────────────────────────────────────────────────────────

    /// <summary>
    /// One or more info blocks. Most alerts carry exactly one (English).
    /// Multi-language alerts include one block per language.
    /// </summary>
    public List<AlertInfo> InfoBlocks { get; set; } = new();
}
