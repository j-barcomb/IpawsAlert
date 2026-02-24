namespace IpawsAlert.Core.Models;

/// <summary>
/// Represents a CAP v1.2 &lt;area&gt; block defining the geographic scope of an alert.
/// At least one of SameCodes, Polygons, Circles, or GeocodePairs must be populated.
/// </summary>
public sealed class AlertArea
{
    /// <summary>Human-readable description of the affected area.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// SAME (Specific Area Message Encoding) location codes.
    /// Format: 6-digit FIPS codes, e.g. "006037" for Los Angeles County, CA.
    /// Used by EAS and WEA dissemination.
    /// </summary>
    public List<string> SameCodes { get; set; } = new();

    /// <summary>
    /// Geographic polygons defining the alert area.
    /// Each polygon is a list of (latitude, longitude) pairs forming a closed ring.
    /// The last point must equal the first point.
    /// CAP format: "lat,lon lat,lon …"
    /// </summary>
    public List<List<(double Lat, double Lon)>> Polygons { get; set; } = new();

    /// <summary>
    /// Circles defining the alert area.
    /// Each circle is a center point + radius in kilometres.
    /// CAP format: "lat,lon radius"
    /// </summary>
    public List<(double Lat, double Lon, double RadiusKm)> Circles { get; set; } = new();

    /// <summary>
    /// Arbitrary geocode key/value pairs (e.g. FIPS6, UGC, SAME).
    /// Key = value-name, Value = code string.
    /// </summary>
    public List<(string ValueName, string Value)> GeocodePairs { get; set; } = new();

    /// <summary>
    /// Optional altitude (in feet above mean sea level) for the affected area.
    /// </summary>
    public double? AltitudeFt { get; set; }

    /// <summary>
    /// Optional ceiling altitude (in feet above mean sea level) for the affected area.
    /// Must be greater than AltitudeFt when specified.
    /// </summary>
    public double? CeilingFt { get; set; }

    /// <summary>Returns true if this area has at least one geographic descriptor.</summary>
    public bool HasGeography =>
        SameCodes.Count > 0 ||
        Polygons.Count > 0 ||
        Circles.Count > 0 ||
        GeocodePairs.Count > 0;
}
