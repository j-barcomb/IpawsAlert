namespace IpawsAlert.Core.Models;

/// <summary>
/// Represents a CAP v1.2 &lt;resource&gt; block — an optional supplemental file
/// attached to an alert (image, audio, document, etc.).
/// </summary>
public sealed class AlertResource
{
    /// <summary>Text describing the resource content.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type of the resource.
    /// Examples: "image/png", "audio/mpeg", "application/pdf"
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>Approximate file size in bytes (-1 = unknown).</summary>
    public long SizeBytes { get; set; } = -1;

    /// <summary>
    /// URI pointing to the resource. Required unless inline Base64 data is provided.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// Base64-encoded resource content for inline embedding.
    /// Mutually exclusive with <see cref="Uri"/> (Uri takes precedence in serialization).
    /// </summary>
    public string? Base64Content { get; set; }

    /// <summary>
    /// SHA-1 digest of the resource for integrity verification.
    /// Format: "sha1+{hex}" per the CAP spec.
    /// </summary>
    public string? DigestSha1 { get; set; }
}
