using IpawsAlert.Core.Client;

namespace IpawsAlert.Wpf.ViewModels;

/// <summary>A single entry in the submission history log.</summary>
public sealed class SubmissionRecord
{
    public string          AlertId       { get; init; } = string.Empty;
    public string          EventType     { get; init; } = string.Empty;
    public string          Headline      { get; init; } = string.Empty;
    public string          Status        { get; init; } = string.Empty;
    public bool            IsSuccess     { get; init; }
    public string          CapStatus     { get; init; } = string.Empty;   // Test / Actual
    public string          Channels      { get; init; } = string.Empty;   // "WEA, EAS"
    public DateTimeOffset  SubmittedAt   { get; init; } = DateTimeOffset.Now;
    public string?         ServerMsgId   { get; init; }
    public string?         ErrorSummary  { get; init; }
    public string          RawCapXml     { get; init; } = string.Empty;
    public TimeSpan?       Elapsed       { get; init; }

    public string SubmittedAtLocal => SubmittedAt.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss");
    public string ElapsedLabel     => Elapsed.HasValue ? $"{Elapsed.Value.TotalMilliseconds:F0} ms" : "—";
    public string StatusLabel      => IsSuccess ? "✓ Accepted" : "✗ " + (ErrorSummary ?? "Failed");

    public static SubmissionRecord FromResponse(
        IpawsResponse response,
        string eventType,
        string headline,
        string capStatus,
        string channels,
        string rawXml)
    {
        return new SubmissionRecord
        {
            AlertId      = response.AlertIdentifier ?? "—",
            EventType    = eventType,
            Headline     = headline,
            Status       = response.Status.ToString(),
            IsSuccess    = response.IsSuccess,
            CapStatus    = capStatus,
            Channels     = channels,
            SubmittedAt  = response.SubmittedAt,
            ServerMsgId  = response.ServerMessageId,
            ErrorSummary = response.Errors.Count > 0 ? string.Join("; ", response.Errors) : null,
            RawCapXml    = rawXml,
            Elapsed      = response.Elapsed,
        };
    }
}
