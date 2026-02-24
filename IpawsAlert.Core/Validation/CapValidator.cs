using IpawsAlert.Core.Models;

namespace IpawsAlert.Core.Validation;

/// <summary>
/// Validates a <see cref="CapAlert"/> against:
///   1. CAP v1.2 schema requirements
///   2. IPAWS-OPEN business rules (FEMA COG requirements)
///   3. Channel-specific constraints (WEA character limits, EAS codes, etc.)
///
/// References:
///   - CAP v1.2: http://docs.oasis-open.org/emergency/cap/v1.2/CAP-v1.2-os.html
///   - IPAWS Developer Resources: https://www.fema.gov/emergency-managers/practitioners/integrated-public-alert-warning-system/developers
/// </summary>
public static class CapValidator
{
    // ── WEA text limits ───────────────────────────────────────────────────────
    private const int WeaShortTextMaxLength = 90;
    private const int WeaLongTextMaxLength  = 360;

    // ── CAP v1.2 constants ────────────────────────────────────────────────────
    private static readonly HashSet<string> KnownEasEventCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "EAN","EAT","NPT","RMT","RWT","ADR","AVA","AVW","BZW","CAE",
        "CDW","CEM","CFA","CFW","DSW","DUW","EQW","EVI","EWW","FFW",
        "FFA","FFS","FFW","FLW","FLA","FLS","FRW","FSW","HLS","HMW",
        "HUA","HUW","HWA","HWW","IBW","IFW","LAE","LEW","LSW","NAT",
        "NIC","NMN","NPT","NST","NUW","POS","RHW","SCY","SMW","SPS",
        "SPW","SQW","SSA","SSW","SVA","SVR","SVS","TOR","TOE","TRA",
        "TRW","TSA","TSW","TXB","TXF","TXO","TXR","VOW","WSA","WSW"
    };

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the alert and returns a <see cref="ValidationResult"/> containing
    /// all errors and warnings found.
    /// </summary>
    public static ValidationResult Validate(CapAlert alert)
    {
        var result = new ValidationResult();
        ValidateAlertLevel(alert, result);

        for (int i = 0; i < alert.InfoBlocks.Count; i++)
            ValidateInfoBlock(alert.InfoBlocks[i], $"InfoBlocks[{i}]", alert.Status, result);

        return result;
    }

    // ── Alert-level rules ─────────────────────────────────────────────────────

    private static void ValidateAlertLevel(CapAlert alert, ValidationResult r)
    {
        // ── Required fields ───────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(alert.Identifier))
            r.AddError("CAP001", "Alert Identifier is required.", "Identifier");

        if (string.IsNullOrWhiteSpace(alert.Sender))
            r.AddError("CAP002", "Alert Sender is required.", "Sender");
        else if (!IsValidSender(alert.Sender))
            r.AddWarning("CAP003", $"Sender '{alert.Sender}' does not look like an email address. FEMA recommends email-format sender IDs.", "Sender");

        if (alert.Sent == default)
            r.AddError("CAP004", "Alert Sent timestamp is required.", "Sent");
        else if (alert.Sent > DateTimeOffset.UtcNow.AddMinutes(5))
            r.AddWarning("CAP005", $"Alert Sent time is more than 5 minutes in the future ({alert.Sent:u}). Verify the system clock.", "Sent");

        // ── MsgType linkage ───────────────────────────────────────────────────
        if (alert.MsgType is CapMsgType.Update or CapMsgType.Cancel)
        {
            if (alert.References.Count == 0)
                r.AddError("CAP006", $"MsgType='{alert.MsgType}' requires at least one reference to the prior message.", "References");
        }

        // ── Scope ─────────────────────────────────────────────────────────────
        if (alert.Scope == CapScope.Restricted && string.IsNullOrWhiteSpace(alert.Restriction))
            r.AddError("CAP007", "Scope='Restricted' requires the Restriction field.", "Restriction");

        if (alert.Scope == CapScope.Private && string.IsNullOrWhiteSpace(alert.Addresses))
            r.AddError("CAP008", "Scope='Private' requires the Addresses field.", "Addresses");

        // ── IPAWS-OPEN requirements ────────────────────────────────────────────
        if (!alert.Codes.Contains("IPAWSv1.0"))
            r.AddError("IPAWS001", "IPAWS-OPEN requires the code 'IPAWSv1.0' in the Codes list.", "Codes");

        if (alert.InfoBlocks.Count == 0)
            r.AddError("CAP009", "Alert must contain at least one info block.", "InfoBlocks");
    }

    // ── Info-block rules ──────────────────────────────────────────────────────

    private static void ValidateInfoBlock(AlertInfo info, string path, CapStatus status, ValidationResult r)
    {
        // ── Required fields ───────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(info.Event))
            r.AddError("CAP010", "Event is required.", $"{path}.Event");

        if (string.IsNullOrWhiteSpace(info.Headline))
            r.AddWarning("CAP011", "Headline is strongly recommended.", $"{path}.Headline");

        // ── Unknown classifications ────────────────────────────────────────────
        if (info.Urgency == CapUrgency.Unknown && status == CapStatus.Actual)
            r.AddWarning("CAP012", "Urgency='Unknown' is not appropriate for Actual alerts.", $"{path}.Urgency");

        if (info.Severity == CapSeverity.Unknown && status == CapStatus.Actual)
            r.AddWarning("CAP013", "Severity='Unknown' is not appropriate for Actual alerts.", $"{path}.Severity");

        if (info.Certainty == CapCertainty.Unknown && status == CapStatus.Actual)
            r.AddWarning("CAP014", "Certainty='Unknown' is not appropriate for Actual alerts.", $"{path}.Certainty");

        // ── Timing ────────────────────────────────────────────────────────────
        if (info.Expires.HasValue)
        {
            if (info.Expires.Value <= DateTimeOffset.UtcNow)
                r.AddError("CAP015", $"Expires ({info.Expires.Value:u}) is in the past.", $"{path}.Expires");

            if (info.Effective.HasValue && info.Expires.Value <= info.Effective.Value)
                r.AddError("CAP016", "Expires must be after Effective.", $"{path}.Expires");

            if (info.Expires.Value > DateTimeOffset.UtcNow.AddHours(24))
                r.AddWarning("CAP017", "WEA channel caps alert duration at 24 hours. Longer expiry is fine for EAS/NWEM.", $"{path}.Expires");
        }
        else
        {
            r.AddWarning("CAP018", "Expires not set. IPAWS-OPEN will apply a default expiry.", $"{path}.Expires");
        }

        // ── Area ──────────────────────────────────────────────────────────────
        if (info.Areas.Count == 0)
            r.AddError("CAP019", "Info block must have at least one area.", $"{path}.Areas");

        for (int i = 0; i < info.Areas.Count; i++)
            ValidateArea(info.Areas[i], $"{path}.Areas[{i}]", r);

        // ── Channel-specific rules ────────────────────────────────────────────
        ValidateWeaParameters(info, path, r);
        ValidateEasParameters(info, path, r);
        ValidateNwemParameters(info, path, r);
    }

    // ── Area rules ────────────────────────────────────────────────────────────

    private static void ValidateArea(AlertArea area, string path, ValidationResult r)
    {
        if (string.IsNullOrWhiteSpace(area.Description))
            r.AddWarning("CAP020", "Area description is missing.", $"{path}.Description");

        if (!area.HasGeography)
            r.AddError("CAP021", "Area must have at least one geographic descriptor (SAME code, polygon, circle, or geocode).", path);

        // Validate SAME codes: 6-digit numeric
        foreach (var code in area.SameCodes)
        {
            if (code.Length != 6 || !code.All(char.IsDigit))
                r.AddError("CAP022", $"SAME code '{code}' is invalid. Must be exactly 6 digits.", path);
        }

        // Validate polygons
        for (int i = 0; i < area.Polygons.Count; i++)
        {
            var poly = area.Polygons[i];
            if (poly.Count < 4)
                r.AddError("CAP023", $"Polygon[{i}] must have at least 4 points (3 + closing point).", path);
            else if (poly[0] != poly[^1])
                r.AddWarning("CAP024", $"Polygon[{i}] is not closed (first and last points differ).", path);

            foreach (var (lat, lon) in poly)
            {
                if (lat < -90 || lat > 90)
                    r.AddError("CAP025", $"Polygon[{i}] contains an invalid latitude: {lat}", path);
                if (lon < -180 || lon > 180)
                    r.AddError("CAP026", $"Polygon[{i}] contains an invalid longitude: {lon}", path);
            }
        }

        // Altitude / ceiling
        if (area.CeilingFt.HasValue && area.AltitudeFt.HasValue && area.CeilingFt <= area.AltitudeFt)
            r.AddError("CAP027", "Ceiling must be greater than Altitude.", path);
    }

    // ── WEA-specific rules ────────────────────────────────────────────────────

    private static void ValidateWeaParameters(AlertInfo info, string path, ValidationResult r)
    {
        bool weaEnabled = info.Parameters.Any(p =>
            p.ValueName.Equals("WEAHandling", StringComparison.OrdinalIgnoreCase) &&
            p.Value.Equals("Broadcast", StringComparison.OrdinalIgnoreCase));

        if (!weaEnabled) return;

        // SAME codes required for WEA geographic targeting
        bool hasSame = info.Areas.Any(a => a.SameCodes.Count > 0 || a.GeocodePairs.Any(g => g.ValueName == "SAME"));
        if (!hasSame)
            r.AddWarning("WEA001", "WEA channel targeting requires SAME codes in at least one area.", $"{path}.Areas");

        // Short text length
        var shortText = info.Parameters.FirstOrDefault(p =>
            p.ValueName.Equals("CMAMtext", StringComparison.OrdinalIgnoreCase)).Value;
        if (shortText is not null && shortText.Length > WeaShortTextMaxLength)
            r.AddError("WEA002",
                $"CMAMtext (WEA 2.0 short text) is {shortText.Length} characters; maximum is {WeaShortTextMaxLength}.",
                $"{path}.Parameters[CMAMtext]");

        // Long text length
        var longText = info.Parameters.FirstOrDefault(p =>
            p.ValueName.Equals("CMAMlongtext", StringComparison.OrdinalIgnoreCase)).Value;
        if (longText is not null && longText.Length > WeaLongTextMaxLength)
            r.AddError("WEA003",
                $"CMAMlongtext (WEA 3.0 long text) is {longText.Length} characters; maximum is {WeaLongTextMaxLength}.",
                $"{path}.Parameters[CMAMlongtext]");

        // Headline fallback warning
        if (shortText is null && longText is null)
            r.AddWarning("WEA004",
                "WEA is enabled but neither CMAMtext nor CMAMlongtext is set. IPAWS-OPEN will derive the WEA text from the Headline.",
                path);

        if (!string.IsNullOrWhiteSpace(info.Headline) && info.Headline.Length > WeaShortTextMaxLength)
            r.AddWarning("WEA005",
                $"Headline is {info.Headline.Length} characters. WEA legacy devices only display {WeaShortTextMaxLength} characters.",
                $"{path}.Headline");
    }

    // ── EAS-specific rules ────────────────────────────────────────────────────

    private static void ValidateEasParameters(AlertInfo info, string path, ValidationResult r)
    {
        bool easEnabled = info.Parameters.Any(p =>
            p.ValueName.Equals("EASHandling", StringComparison.OrdinalIgnoreCase) &&
            p.Value.Equals("Broadcast", StringComparison.OrdinalIgnoreCase));

        if (!easEnabled) return;

        // EAS requires SAME codes
        bool hasSame = info.Areas.Any(a => a.SameCodes.Count > 0 || a.GeocodePairs.Any(g => g.ValueName == "SAME"));
        if (!hasSame)
            r.AddError("EAS001", "EAS channel requires SAME location codes in at least one area.", $"{path}.Areas");

        // Check SAME count — EAS header supports max 31 SAME codes
        var sameCount = info.Areas.SelectMany(a => a.SameCodes).Distinct().Count()
                      + info.Areas.SelectMany(a => a.GeocodePairs.Where(g => g.ValueName == "SAME")).Count();
        if (sameCount > 31)
            r.AddWarning("EAS002",
                $"EAS header supports a maximum of 31 SAME location codes; found {sameCount}.",
                $"{path}.Areas");
    }

    // ── NWEM-specific rules ───────────────────────────────────────────────────

    private static void ValidateNwemParameters(AlertInfo info, string path, ValidationResult r)
    {
        bool nwemEnabled = info.Parameters.Any(p =>
            p.ValueName.Equals("NWEMHandling", StringComparison.OrdinalIgnoreCase) &&
            p.Value.Equals("Broadcast", StringComparison.OrdinalIgnoreCase));

        if (!nwemEnabled) return;

        // NWEM benefits strongly from VTEC
        bool hasVtec = info.Parameters.Any(p =>
            p.ValueName.Equals("VTEC", StringComparison.OrdinalIgnoreCase));
        if (!hasVtec)
            r.AddWarning("NWEM001",
                "NWEM channel is enabled but no VTEC parameter is present. NWS products typically require VTEC.",
                path);

        // UGC codes recommended
        bool hasUgc = info.Areas.Any(a =>
            a.GeocodePairs.Any(g => g.ValueName.Equals("UGC", StringComparison.OrdinalIgnoreCase)));
        if (!hasUgc)
            r.AddWarning("NWEM002",
                "NWEM channel is enabled but no UGC geocodes are present. NOAA Weather Radio uses UGC zone codes.",
                path);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsValidSender(string sender) =>
        sender.Contains('@') && sender.Contains('.');
}
