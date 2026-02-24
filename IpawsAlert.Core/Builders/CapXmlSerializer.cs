using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using IpawsAlert.Core.Models;

namespace IpawsAlert.Core.Builders;

/// <summary>
/// Serializes <see cref="CapAlert"/> objects to CAP v1.2 XML and deserializes back.
/// Output is schema-valid against the OASIS CAP v1.2 schema.
/// </summary>
public static class CapXmlSerializer
{
    // CAP v1.2 namespace
    private const string CapNs = "urn:oasis:names:tc:emergency:cap:1.2";

    // CAP date-time format: ISO 8601 with offset
    private const string DateFmt = "yyyy-MM-ddTHH:mm:sszzz";

    // ── Serialization ────────────────────────────────────────────────────────

    /// <summary>
    /// Serializes a <see cref="CapAlert"/> to a CAP v1.2 XML string.
    /// </summary>
    /// <param name="alert">The alert to serialize.</param>
    /// <param name="indent">Whether to indent the output (debug-friendly).</param>
    public static string Serialize(CapAlert alert, bool indent = false)
    {
        XNamespace ns = CapNs;

        var alertElem = new XElement(ns + "alert",
            new XAttribute(XNamespace.Xmlns + "cap", CapNs),
            Elem(ns, "identifier",  alert.Identifier ?? throw new InvalidOperationException("Identifier must be set before serialization. Call Build() first.")),
            Elem(ns, "sender",      alert.Sender),
            Elem(ns, "sent",        alert.Sent.ToString(DateFmt)),
            Elem(ns, "status",      alert.Status.ToString()),
            Elem(ns, "msgType",     alert.MsgType.ToString()),
            Elem(ns, "scope",       alert.Scope.ToString())
        );

        if (!string.IsNullOrWhiteSpace(alert.Restriction))
            alertElem.Add(Elem(ns, "restriction", alert.Restriction));

        if (!string.IsNullOrWhiteSpace(alert.Addresses))
            alertElem.Add(Elem(ns, "addresses", WrapInQuotes(alert.Addresses)));

        foreach (var code in alert.Codes)
            alertElem.Add(Elem(ns, "code", code));

        if (!string.IsNullOrWhiteSpace(alert.Note))
            alertElem.Add(Elem(ns, "note", alert.Note));

        if (alert.References.Count > 0)
            alertElem.Add(Elem(ns, "references", string.Join(" ", alert.References)));

        if (alert.Incidents.Count > 0)
            alertElem.Add(Elem(ns, "incidents", string.Join(" ", alert.Incidents)));

        foreach (var info in alert.InfoBlocks)
            alertElem.Add(SerializeInfo(ns, info));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            alertElem
        );

        var settings = new XmlWriterSettings
        {
            Indent          = indent,
            IndentChars     = "  ",
            Encoding        = new UTF8Encoding(false),
            OmitXmlDeclaration = false
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
            doc.WriteTo(writer);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static XElement SerializeInfo(XNamespace ns, AlertInfo info)
    {
        var elem = new XElement(ns + "info",
            Elem(ns, "language",    info.Language),
            SerializeCategories(ns, info.Category),
            Elem(ns, "event",       info.Event),
            SerializeResponseTypes(ns, info.ResponseTypes),
            Elem(ns, "urgency",     info.Urgency.ToString()),
            Elem(ns, "severity",    info.Severity.ToString()),
            Elem(ns, "certainty",   info.Certainty.ToString())
        );

        if (!string.IsNullOrWhiteSpace(info.Audience))
            elem.Add(Elem(ns, "audience", info.Audience));

        foreach (var (vn, v) in info.EventCodes)
            elem.Add(SerializeValuePair(ns, "eventCode", vn, v));

        if (info.Effective.HasValue)
            elem.Add(Elem(ns, "effective", info.Effective.Value.ToString(DateFmt)));
        if (info.Onset.HasValue)
            elem.Add(Elem(ns, "onset", info.Onset.Value.ToString(DateFmt)));
        if (info.Expires.HasValue)
            elem.Add(Elem(ns, "expires", info.Expires.Value.ToString(DateFmt)));

        if (!string.IsNullOrWhiteSpace(info.SenderName))
            elem.Add(Elem(ns, "senderName", info.SenderName));

        if (!string.IsNullOrWhiteSpace(info.Headline))
            elem.Add(Elem(ns, "headline", info.Headline));
        if (!string.IsNullOrWhiteSpace(info.Description))
            elem.Add(Elem(ns, "description", info.Description));
        if (!string.IsNullOrWhiteSpace(info.Instruction))
            elem.Add(Elem(ns, "instruction", info.Instruction));

        if (info.Web is not null)
            elem.Add(Elem(ns, "web", info.Web.ToString()));
        if (!string.IsNullOrWhiteSpace(info.Contact))
            elem.Add(Elem(ns, "contact", info.Contact));

        foreach (var (vn, v) in info.Parameters)
            elem.Add(SerializeValuePair(ns, "parameter", vn, v));

        foreach (var resource in info.Resources)
            elem.Add(SerializeResource(ns, resource));

        foreach (var area in info.Areas)
            elem.Add(SerializeArea(ns, area));

        return elem;
    }

    private static IEnumerable<XElement> SerializeCategories(XNamespace ns, CapCategory cats)
    {
        if (cats == CapCategory.None)
        {
            yield return Elem(ns, "category", "Other");
            yield break;
        }
        foreach (CapCategory flag in Enum.GetValues<CapCategory>())
            if (flag != CapCategory.None && cats.HasFlag(flag))
                yield return Elem(ns, "category", flag.ToString());
    }

    private static IEnumerable<XElement> SerializeResponseTypes(XNamespace ns, List<CapResponseType> types)
    {
        foreach (var t in types)
            yield return Elem(ns, "responseType", t.ToString());
    }

    private static XElement SerializeValuePair(XNamespace ns, string elementName, string valueName, string value)
    {
        return new XElement(ns + elementName,
            Elem(ns, "valueName", valueName),
            Elem(ns, "value", value)
        );
    }

    private static XElement SerializeResource(XNamespace ns, AlertResource resource)
    {
        var elem = new XElement(ns + "resource",
            Elem(ns, "resourceDesc", resource.Description),
            Elem(ns, "mimeType", resource.MimeType)
        );
        if (resource.SizeBytes >= 0)
            elem.Add(Elem(ns, "size", resource.SizeBytes.ToString()));
        if (resource.Uri is not null)
            elem.Add(Elem(ns, "uri", resource.Uri.ToString()));
        else if (!string.IsNullOrWhiteSpace(resource.Base64Content))
            elem.Add(Elem(ns, "derefUri", resource.Base64Content));
        if (!string.IsNullOrWhiteSpace(resource.DigestSha1))
            elem.Add(Elem(ns, "digest", resource.DigestSha1));
        return elem;
    }

    private static XElement SerializeArea(XNamespace ns, AlertArea area)
    {
        var elem = new XElement(ns + "area",
            Elem(ns, "areaDesc", area.Description)
        );

        foreach (var polygon in area.Polygons)
        {
            var pts = string.Join(" ", polygon.Select(p =>
                $"{p.Lat.ToString(CultureInfo.InvariantCulture)},{p.Lon.ToString(CultureInfo.InvariantCulture)}"));
            elem.Add(Elem(ns, "polygon", pts));
        }

        foreach (var (lat, lon, r) in area.Circles)
        {
            var val = $"{lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)} {r.ToString(CultureInfo.InvariantCulture)}";
            elem.Add(Elem(ns, "circle", val));
        }

        foreach (var (vn, v) in area.GeocodePairs)
            elem.Add(SerializeValuePair(ns, "geocode", vn, v));

        if (area.AltitudeFt.HasValue)
            elem.Add(Elem(ns, "altitude", area.AltitudeFt.Value.ToString(CultureInfo.InvariantCulture)));
        if (area.CeilingFt.HasValue)
            elem.Add(Elem(ns, "ceiling", area.CeilingFt.Value.ToString(CultureInfo.InvariantCulture)));

        // SAME codes are serialized as geocode value pairs per IPAWS conventions
        foreach (var code in area.SameCodes)
            elem.Add(SerializeValuePair(ns, "geocode", "SAME", code));

        return elem;
    }

    // ── Deserialization ──────────────────────────────────────────────────────

    /// <summary>
    /// Deserializes a CAP v1.2 XML string into a <see cref="CapAlert"/>.
    /// </summary>
    /// <exception cref="FormatException">Thrown when required fields are missing.</exception>
    public static CapAlert Deserialize(string xml)
    {
        var doc  = XDocument.Parse(xml);
        XNamespace ns = CapNs;
        var root = doc.Root ?? throw new FormatException("Empty XML document.");

        var alert = new CapAlert
        {
            Identifier = RequiredText(root, ns + "identifier"),
            Sender     = RequiredText(root, ns + "sender"),
            Sent       = ParseDate(RequiredText(root, ns + "sent")),
            Status     = ParseEnum<CapStatus> (RequiredText(root, ns + "status")),
            MsgType    = ParseEnum<CapMsgType>(RequiredText(root, ns + "msgType")),
            Scope      = ParseEnum<CapScope>  (RequiredText(root, ns + "scope")),
            Restriction = root.Element(ns + "restriction")?.Value,
            Addresses   = root.Element(ns + "addresses")?.Value,
            Note        = root.Element(ns + "note")?.Value,
        };

        var refs = root.Element(ns + "references")?.Value;
        if (!string.IsNullOrWhiteSpace(refs))
            alert.References.AddRange(refs.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var incidents = root.Element(ns + "incidents")?.Value;
        if (!string.IsNullOrWhiteSpace(incidents))
            alert.Incidents.AddRange(incidents.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        foreach (var code in root.Elements(ns + "code"))
            alert.Codes.Add(code.Value);

        foreach (var infoElem in root.Elements(ns + "info"))
            alert.InfoBlocks.Add(DeserializeInfo(ns, infoElem));

        return alert;
    }

    private static AlertInfo DeserializeInfo(XNamespace ns, XElement e)
    {
        var info = new AlertInfo
        {
            Language    = e.Element(ns + "language")?.Value ?? "en-US",
            Event       = RequiredText(e, ns + "event"),
            Urgency     = ParseEnum<CapUrgency>  (RequiredText(e, ns + "urgency")),
            Severity    = ParseEnum<CapSeverity> (RequiredText(e, ns + "severity")),
            Certainty   = ParseEnum<CapCertainty>(RequiredText(e, ns + "certainty")),
            Audience    = e.Element(ns + "audience")?.Value,
            SenderName  = e.Element(ns + "senderName")?.Value,
            Headline    = e.Element(ns + "headline")?.Value ?? string.Empty,
            Description = e.Element(ns + "description")?.Value,
            Instruction = e.Element(ns + "instruction")?.Value,
            Contact     = e.Element(ns + "contact")?.Value,
        };

        // Category (flags)
        foreach (var c in e.Elements(ns + "category"))
        {
            if (Enum.TryParse<CapCategory>(c.Value, out var cat))
                info.Category |= cat;
        }

        // Response types
        foreach (var rt in e.Elements(ns + "responseType"))
        {
            if (Enum.TryParse<CapResponseType>(rt.Value, out var r))
                info.ResponseTypes.Add(r);
        }

        // Timing
        var eff = e.Element(ns + "effective")?.Value;
        if (eff is not null) info.Effective = ParseDate(eff);
        var ons = e.Element(ns + "onset")?.Value;
        if (ons is not null) info.Onset = ParseDate(ons);
        var exp = e.Element(ns + "expires")?.Value;
        if (exp is not null) info.Expires = ParseDate(exp);

        var web = e.Element(ns + "web")?.Value;
        if (web is not null && Uri.TryCreate(web, UriKind.Absolute, out var uri))
            info.Web = uri;

        // Value pairs
        foreach (var ec in e.Elements(ns + "eventCode"))
            info.EventCodes.Add(ReadValuePair(ec, ns));
        foreach (var p in e.Elements(ns + "parameter"))
            info.Parameters.Add(ReadValuePair(p, ns));

        foreach (var r in e.Elements(ns + "resource"))
            info.Resources.Add(DeserializeResource(ns, r));
        foreach (var a in e.Elements(ns + "area"))
            info.Areas.Add(DeserializeArea(ns, a));

        return info;
    }

    private static AlertResource DeserializeResource(XNamespace ns, XElement e) =>
        new()
        {
            Description    = e.Element(ns + "resourceDesc")?.Value ?? string.Empty,
            MimeType       = e.Element(ns + "mimeType")?.Value ?? string.Empty,
            SizeBytes      = long.TryParse(e.Element(ns + "size")?.Value, out var sz) ? sz : -1,
            Uri            = Uri.TryCreate(e.Element(ns + "uri")?.Value, UriKind.Absolute, out var u) ? u : null,
            Base64Content  = e.Element(ns + "derefUri")?.Value,
            DigestSha1     = e.Element(ns + "digest")?.Value,
        };

    private static AlertArea DeserializeArea(XNamespace ns, XElement e)
    {
        var area = new AlertArea
        {
            Description = e.Element(ns + "areaDesc")?.Value ?? string.Empty
        };

        foreach (var p in e.Elements(ns + "polygon"))
        {
            var pts = p.Value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(pt => {
                    var parts = pt.Split(',');
                    return (double.Parse(parts[0], CultureInfo.InvariantCulture),
                            double.Parse(parts[1], CultureInfo.InvariantCulture));
                }).ToList();
            area.Polygons.Add(pts);
        }

        foreach (var c in e.Elements(ns + "circle"))
        {
            var parts = c.Value.Trim().Split(' ');
            var coords = parts[0].Split(',');
            area.Circles.Add((
                double.Parse(coords[0], CultureInfo.InvariantCulture),
                double.Parse(coords[1], CultureInfo.InvariantCulture),
                double.Parse(parts[1],  CultureInfo.InvariantCulture)
            ));
        }

        foreach (var g in e.Elements(ns + "geocode"))
        {
            var (vn, v) = ReadValuePair(g, ns);
            if (vn == "SAME")
                area.SameCodes.Add(v);
            else
                area.GeocodePairs.Add((vn, v));
        }

        if (double.TryParse(e.Element(ns + "altitude")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var alt))
            area.AltitudeFt = alt;
        if (double.TryParse(e.Element(ns + "ceiling")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var ceil))
            area.CeilingFt = ceil;

        return area;
    }

    // ── XML helpers ──────────────────────────────────────────────────────────

    private static XElement Elem(XNamespace ns, string name, string? value = null) =>
        new(ns + name, value ?? string.Empty);

    private static string RequiredText(XElement parent, XName name)
    {
        var val = parent.Element(name)?.Value;
        if (val is null) throw new FormatException($"Missing required CAP element: <{name.LocalName}>");
        return val;
    }

    private static (string ValueName, string Value) ReadValuePair(XElement e, XNamespace ns) =>
        (e.Element(ns + "valueName")?.Value ?? string.Empty,
         e.Element(ns + "value")?.Value ?? string.Empty);

    private static DateTimeOffset ParseDate(string s) =>
        DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result)
            ? result
            : throw new FormatException($"Unknown {typeof(T).Name} value: '{value}'");

    private static string WrapInQuotes(string addresses)
    {
        // CAP spec: multi-word tokens must be wrapped in double-quotes
        return string.Join(" ", addresses.Split(' ').Select(a =>
            a.Contains(' ') ? $"\"{a}\"" : a));
    }
}
