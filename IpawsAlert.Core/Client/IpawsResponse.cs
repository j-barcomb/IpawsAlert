namespace IpawsAlert.Core.Client;

/// <summary>
/// Outcome of a single IPAWS-OPEN message submission attempt.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>Message was accepted by the IPAWS-OPEN gateway.</summary>
    Success,
    /// <summary>Message was rejected by the IPAWS-OPEN gateway (see <see cref="IpawsResponse.Errors"/>).</summary>
    Rejected,
    /// <summary>Network or TLS error before a response was received.</summary>
    NetworkError,
    /// <summary>The server returned an unexpected HTTP status code.</summary>
    UnexpectedHttpStatus,
    /// <summary>The request timed out.</summary>
    Timeout
}

/// <summary>
/// Represents the full result of submitting a CAP alert to IPAWS-OPEN.
/// </summary>
public sealed class IpawsResponse
{
    /// <summary>Whether the message was successfully accepted.</summary>
    public SubmissionStatus Status { get; init; }

    /// <summary>True only when Status == Success.</summary>
    public bool IsSuccess => Status == SubmissionStatus.Success;

    /// <summary>HTTP status code returned by the server (0 if no response received).</summary>
    public int HttpStatusCode { get; init; }

    /// <summary>
    /// IPAWS-OPEN server-assigned message identifier returned on success.
    /// Null if the submission was not accepted.
    /// </summary>
    public string? ServerMessageId { get; init; }

    /// <summary>Raw response body from the IPAWS-OPEN server.</summary>
    public string? RawResponseBody { get; init; }

    /// <summary>
    /// Server-reported error messages on rejection, or client-side error details
    /// on network/timeout failures.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Warnings returned by the server (non-fatal).
    /// The message was accepted but there may be issues to review.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>The CAP identifier of the alert that was submitted.</summary>
    public string? AlertIdentifier { get; init; }

    /// <summary>UTC time when the submission was made.</summary>
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>How long the HTTP round-trip took.</summary>
    public TimeSpan? Elapsed { get; init; }

    // ── Factory helpers ───────────────────────────────────────────────────────

    public static IpawsResponse Ok(string alertId, string? serverId, string? body, TimeSpan elapsed) =>
        new()
        {
            Status          = SubmissionStatus.Success,
            HttpStatusCode  = 200,
            AlertIdentifier = alertId,
            ServerMessageId = serverId,
            RawResponseBody = body,
            Elapsed         = elapsed
        };

    public static IpawsResponse Fail(SubmissionStatus status, int httpCode, IEnumerable<string> errors,
                                      string? body = null, string? alertId = null) =>
        new()
        {
            Status          = status,
            HttpStatusCode  = httpCode,
            Errors          = errors.ToList(),
            RawResponseBody = body,
            AlertIdentifier = alertId
        };

    public static IpawsResponse NetworkFailure(Exception ex, string? alertId = null) =>
        new()
        {
            Status          = ex is TaskCanceledException or OperationCanceledException
                                  ? SubmissionStatus.Timeout
                                  : SubmissionStatus.NetworkError,
            AlertIdentifier = alertId,
            Errors          = new[] { ex.Message }
        };
}
