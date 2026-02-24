namespace IpawsAlert.Core.Models;

/// <summary>
/// Represents a CAP v1.2 &lt;info&gt; block.
/// A single &lt;alert&gt; may carry multiple info blocks (e.g. one per language).
/// </summary>
public sealed class AlertInfo
{
    // ── Language ────────────────────────────────────────────────────────────

    /// <summary>RFC 3066 language tag. Default "en-US".</summary>
    public string Language { get; set; } = "en-US";

    // ── Classification ───────────────────────────────────────────────────────

    /// <summary>One or more event categories (flags enum).</summary>
    public CapCategory Category { get; set; } = CapCategory.Other;

    /// <summary>
    /// Free-text event type. Should come from a recognized vocabulary.
    /// Examples: "Tornado Warning", "Amber Alert", "Flash Flood Watch"
    /// </summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>Intended response type(s).</summary>
    public List<CapResponseType> ResponseTypes { get; set; } = new();

    public CapUrgency    Urgency    { get; set; } = CapUrgency.Unknown;
    public CapSeverity   Severity   { get; set; } = CapSeverity.Unknown;
    public CapCertainty  Certainty  { get; set; } = CapCertainty.Unknown;

    // ── Optional classification refinements ──────────────────────────────────

    /// <summary>
    /// Numeric NWS audience value (e.g. "tornado warning" = specific VTEC code).
    /// Optional. Used for audience-based WEA targeting.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Event codes as key/value pairs (e.g. SAME event codes, NWS VTEC).
    /// Key = value-name (e.g. "SAME"), Value = code (e.g. "TOR").
    /// </summary>
    public List<(string ValueName, string Value)> EventCodes { get; set; } = new();

    // ── Timing ───────────────────────────────────────────────────────────────

    /// <summary>Effective time of the information (default: sent time of the alert).</summary>
    public DateTimeOffset? Effective { get; set; }

    /// <summary>Expected time of onset of the subject event.</summary>
    public DateTimeOffset? Onset { get; set; }

    /// <summary>
    /// Expiry time. If not set, IPAWS-OPEN uses a default maximum (24 hours for WEA).
    /// </summary>
    public DateTimeOffset? Expires { get; set; }

    // ── Source / sender ──────────────────────────────────────────────────────

    /// <summary>Text naming the originating authority for this info block.</summary>
    public string? SenderName { get; set; }

    // ── Human-readable content ───────────────────────────────────────────────

    /// <summary>
    /// Short headline summarizing the alert.
    /// WEA: limited to 90 chars (legacy) or 360 chars (WEA 3.0).
    /// </summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Extended description of the hazard.</summary>
    public string? Description { get; set; }

    /// <summary>Recommended action for the targeted audience.</summary>
    public string? Instruction { get; set; }

    /// <summary>Hyperlink to additional or reference information.</summary>
    public Uri? Web { get; set; }

    /// <summary>
    /// Telephone contact for follow-up information.
    /// </summary>
    public string? Contact { get; set; }

    // ── Parameters ───────────────────────────────────────────────────────────

    /// <summary>
    /// System-specific additional parameters as key/value pairs.
    /// IPAWS-OPEN uses these for channel routing codes.
    /// Key = value-name, Value = parameter value.
    /// </summary>
    public List<(string ValueName, string Value)> Parameters { get; set; } = new();

    // ── Geographic areas ─────────────────────────────────────────────────────

    /// <summary>One or more geographic areas covered by this info block.</summary>
    public List<AlertArea> Areas { get; set; } = new();

    // ── Resource attachments ─────────────────────────────────────────────────

    /// <summary>Optional supplemental files (images, audio, documents).</summary>
    public List<AlertResource> Resources { get; set; } = new();
}
