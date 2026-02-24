using IpawsAlert.Core.Models;

namespace IpawsAlert.Core.Builders;

/// <summary>
/// Fluent builder for constructing well-formed <see cref="CapAlert"/> messages.
/// 
/// Usage:
/// <code>
/// var alert = new CapAlertBuilder()
///     .WithSender("w-nws.webmaster@noaa.gov")
///     .WithStatus(CapStatus.Test)
///     .WithMsgType(CapMsgType.Alert)
///     .AddInfo(info => info
///         .WithEvent("Tornado Warning")
///         .WithSeverity(CapSeverity.Extreme)
///         .WithUrgency(CapUrgency.Immediate)
///         .WithCertainty(CapCertainty.Observed)
///         .WithHeadline("Tornado Warning for Example County")
///         .WithDescription("A confirmed tornado is on the ground...")
///         .WithInstruction("Take shelter immediately...")
///         .WithExpiry(DateTimeOffset.UtcNow.AddHours(1))
///         .AddSameCode("042001")         // Example FIPS
///         .AddWeaRouting()
///         .AddEasRouting()
///     )
///     .Build();
/// </code>
/// </summary>
public sealed class CapAlertBuilder
{
    private readonly CapAlert _alert = new();
    private readonly List<AlertInfoBuilder> _infoBuilders = new();

    // ── Alert-level setters ──────────────────────────────────────────────────

    /// <summary>Sets the COG sender identifier.</summary>
    public CapAlertBuilder WithSender(string sender)
    {
        _alert.Sender = sender;
        return this;
    }

    /// <summary>Overrides the auto-generated identifier.</summary>
    public CapAlertBuilder WithIdentifier(string identifier)
    {
        _alert.Identifier = identifier;
        return this;
    }

    /// <summary>Overrides the sent timestamp (default: UtcNow at build time).</summary>
    public CapAlertBuilder WithSent(DateTimeOffset sent)
    {
        _alert.Sent = sent;
        return this;
    }

    public CapAlertBuilder WithStatus(CapStatus status)
    {
        _alert.Status = status;
        return this;
    }

    public CapAlertBuilder WithMsgType(CapMsgType msgType)
    {
        _alert.MsgType = msgType;
        return this;
    }

    public CapAlertBuilder WithScope(CapScope scope)
    {
        _alert.Scope = scope;
        return this;
    }

    public CapAlertBuilder WithNote(string note)
    {
        _alert.Note = note;
        return this;
    }

    /// <summary>
    /// Adds a reference to a prior alert (required for Update and Cancel).
    /// </summary>
    public CapAlertBuilder AddReference(string sender, string identifier, DateTimeOffset sent)
    {
        _alert.References.Add($"{sender},{identifier},{sent:yyyy-MM-ddTHH:mm:sszzz}");
        return this;
    }

    /// <summary>Adds an IPAWS routing code (e.g. "IPAWSv1.0").</summary>
    public CapAlertBuilder AddCode(string code)
    {
        _alert.Codes.Add(code);
        return this;
    }

    // ── Info block ────────────────────────────────────────────────────────────

    /// <summary>Adds an info block via a nested builder action.</summary>
    public CapAlertBuilder AddInfo(Action<AlertInfoBuilder> configure)
    {
        var builder = new AlertInfoBuilder();
        configure(builder);
        _infoBuilders.Add(builder);
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces the final <see cref="CapAlert"/>. Does NOT validate —
    /// call <see cref="Validation.CapValidator.Validate"/> separately if desired.
    /// </summary>
    public CapAlert Build()
    {
        // Auto-generate identifier if not set
        if (string.IsNullOrWhiteSpace(_alert.Identifier))
            _alert.Identifier = GenerateIdentifier(_alert.Sender, _alert.Sent);

        // Ensure the standard IPAWS code is present
        if (!_alert.Codes.Contains("IPAWSv1.0"))
            _alert.Codes.Insert(0, "IPAWSv1.0");

        foreach (var ib in _infoBuilders)
            _alert.InfoBlocks.Add(ib.Build());

        return _alert;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GenerateIdentifier(string sender, DateTimeOffset sent)
    {
        // Convention used by many IPAWS senders:
        // {sender}-{yyyyMMddHHmmss}-{random4}
        var domain = sender.Contains('@') ? sender.Split('@')[1] : sender;
        return $"{domain}-{sent:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}

/// <summary>
/// Fluent builder for a single CAP &lt;info&gt; block.
/// </summary>
public sealed class AlertInfoBuilder
{
    private readonly AlertInfo _info = new();

    // ── Language ──────────────────────────────────────────────────────────────

    public AlertInfoBuilder WithLanguage(string lang)
    {
        _info.Language = lang;
        return this;
    }

    // ── Classification ────────────────────────────────────────────────────────

    public AlertInfoBuilder WithCategory(CapCategory category)
    {
        _info.Category = category;
        return this;
    }

    public AlertInfoBuilder WithEvent(string eventType)
    {
        _info.Event = eventType;
        return this;
    }

    public AlertInfoBuilder AddResponseType(CapResponseType responseType)
    {
        _info.ResponseTypes.Add(responseType);
        return this;
    }

    public AlertInfoBuilder WithUrgency(CapUrgency urgency)
    {
        _info.Urgency = urgency;
        return this;
    }

    public AlertInfoBuilder WithSeverity(CapSeverity severity)
    {
        _info.Severity = severity;
        return this;
    }

    public AlertInfoBuilder WithCertainty(CapCertainty certainty)
    {
        _info.Certainty = certainty;
        return this;
    }

    // ── Timing ────────────────────────────────────────────────────────────────

    public AlertInfoBuilder WithEffective(DateTimeOffset effective)
    {
        _info.Effective = effective;
        return this;
    }

    public AlertInfoBuilder WithOnset(DateTimeOffset onset)
    {
        _info.Onset = onset;
        return this;
    }

    public AlertInfoBuilder WithExpiry(DateTimeOffset expires)
    {
        _info.Expires = expires;
        return this;
    }

    /// <summary>Convenience: set expiry as a duration from now.</summary>
    public AlertInfoBuilder WithExpiryDuration(TimeSpan duration)
    {
        _info.Expires = DateTimeOffset.UtcNow.Add(duration);
        return this;
    }

    // ── Source ────────────────────────────────────────────────────────────────

    public AlertInfoBuilder WithSenderName(string name)
    {
        _info.SenderName = name;
        return this;
    }

    // ── Content ───────────────────────────────────────────────────────────────

    public AlertInfoBuilder WithHeadline(string headline)
    {
        _info.Headline = headline;
        return this;
    }

    public AlertInfoBuilder WithDescription(string description)
    {
        _info.Description = description;
        return this;
    }

    public AlertInfoBuilder WithInstruction(string instruction)
    {
        _info.Instruction = instruction;
        return this;
    }

    public AlertInfoBuilder WithWeb(Uri uri)
    {
        _info.Web = uri;
        return this;
    }

    public AlertInfoBuilder WithContact(string contact)
    {
        _info.Contact = contact;
        return this;
    }

    // ── Parameters / routing ─────────────────────────────────────────────────

    public AlertInfoBuilder AddParameter(string name, string value)
    {
        _info.Parameters.Add((name, value));
        return this;
    }

    /// <summary>
    /// Adds the IPAWS-OPEN routing parameter that targets the WEA channel.
    /// FEMA requires: CMAMtext for the alert text, CMAMlongtext for WEA 3.0.
    /// </summary>
    public AlertInfoBuilder AddWeaRouting(string? shortText = null, string? longText = null)
    {
        _info.Parameters.Add(("WEAHandling", "Broadcast"));
        if (shortText is not null)
            _info.Parameters.Add(("CMAMtext", shortText));
        if (longText is not null)
            _info.Parameters.Add(("CMAMlongtext", longText));
        return this;
    }

    /// <summary>
    /// Adds the IPAWS-OPEN routing parameter that targets the EAS channel.
    /// </summary>
    public AlertInfoBuilder AddEasRouting()
    {
        _info.Parameters.Add(("EASHandling", "Broadcast"));
        return this;
    }

    /// <summary>
    /// Adds the IPAWS-OPEN routing parameter that targets the NWEM channel.
    /// </summary>
    public AlertInfoBuilder AddNwemRouting()
    {
        _info.Parameters.Add(("NWEMHandling", "Broadcast"));
        return this;
    }

    public AlertInfoBuilder AddEventCode(string valueName, string value)
    {
        _info.EventCodes.Add((valueName, value));
        return this;
    }

    // ── Area ──────────────────────────────────────────────────────────────────

    /// <summary>Adds an area via a nested builder action.</summary>
    public AlertInfoBuilder AddArea(Action<AlertAreaBuilder> configure)
    {
        var ab = new AlertAreaBuilder();
        configure(ab);
        _info.Areas.Add(ab.Build());
        return this;
    }

    /// <summary>Shorthand: add an area with one or more SAME codes.</summary>
    public AlertInfoBuilder AddSameCode(params string[] sameCodes)
    {
        return AddArea(a => {
            foreach (var code in sameCodes)
                a.AddSameCode(code);
            a.WithDescription($"SAME:{string.Join(",", sameCodes)}");
        });
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    internal AlertInfo Build() => _info;
}

/// <summary>
/// Fluent builder for a CAP &lt;area&gt; block.
/// </summary>
public sealed class AlertAreaBuilder
{
    private readonly AlertArea _area = new();

    public AlertAreaBuilder WithDescription(string description)
    {
        _area.Description = description;
        return this;
    }

    public AlertAreaBuilder AddSameCode(string fips6)
    {
        _area.SameCodes.Add(fips6);
        return this;
    }

    /// <summary>
    /// Adds a closed polygon. Points must form a ring (last == first).
    /// </summary>
    public AlertAreaBuilder AddPolygon(IEnumerable<(double Lat, double Lon)> points)
    {
        var list = points.ToList();
        if (list.Count > 0 && list[0] != list[^1])
            list.Add(list[0]); // auto-close
        _area.Polygons.Add(list);
        return this;
    }

    public AlertAreaBuilder AddCircle(double lat, double lon, double radiusKm)
    {
        _area.Circles.Add((lat, lon, radiusKm));
        return this;
    }

    public AlertAreaBuilder AddGeocode(string valueName, string value)
    {
        _area.GeocodePairs.Add((valueName, value));
        return this;
    }

    public AlertAreaBuilder WithAltitude(double altFt, double? ceilingFt = null)
    {
        _area.AltitudeFt = altFt;
        _area.CeilingFt = ceilingFt;
        return this;
    }

    internal AlertArea Build() => _area;
}
