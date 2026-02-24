using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using IpawsAlert.Core.Builders;
using IpawsAlert.Core.Channels;
using IpawsAlert.Core.Models;

namespace IpawsAlert.Core.Client;

/// <summary>
/// HTTP client for submitting CAP v1.2 messages to the IPAWS-OPEN gateway.
///
/// Uses mutual TLS (mTLS) with the FEMA-issued client certificate.
/// Create one instance per application lifetime and dispose when done.
///
/// <example>
/// <code>
/// var config = new IpawsOpenConfig
/// {
///     Endpoint            = IpawsOpenConfig.TestEndpoint,
///     CertificatePath     = @"C:\certs\my-ipaws-cert.p12",
///     CertificatePassword = "secret",
///     CogId               = "MY-COG-12345"
/// };
///
/// using var client = new IpawsClient(config);
///
/// var alert = new CapAlertBuilder()
///     .WithSender("ipaws-test@myagency.gov")
///     .WithStatus(CapStatus.Test)
///     // ... (see CapAlertBuilder for full usage)
///     .Build();
///
/// var response = await client.SubmitAsync(alert);
/// Console.WriteLine(response.IsSuccess ? "Sent!" : string.Join(", ", response.Errors));
/// </code>
/// </example>
/// </summary>
public sealed class IpawsClient : IDisposable
{
    private readonly IpawsOpenConfig _config;
    private readonly HttpClient      _http;
    private bool                     _disposed;

    // ── Construction ──────────────────────────────────────────────────────────

    public IpawsClient(IpawsOpenConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        var handler = BuildHandler();
        _http = new HttpClient(handler)
        {
            Timeout = config.Timeout
        };
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/xml"));
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Serializes and submits a <see cref="CapAlert"/> to the IPAWS-OPEN gateway.
    /// Retries on transient failures up to <see cref="IpawsOpenConfig.MaxRetries"/> times.
    /// </summary>
    public async Task<IpawsResponse> SubmitAsync(
        CapAlert alert,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert);
        if (string.IsNullOrWhiteSpace(alert.Identifier))
            throw new ArgumentException("Alert must have an Identifier. Call Build() first.");

        string xml;
        try
        {
            xml = CapXmlSerializer.Serialize(alert, indent: false);
        }
        catch (Exception ex)
        {
            return IpawsResponse.Fail(
                SubmissionStatus.Rejected,
                0,
                new[] { $"Serialization error: {ex.Message}" },
                alertId: alert.Identifier);
        }

        return await SubmitXmlAsync(xml, alert.Identifier, cancellationToken);
    }

    /// <summary>
    /// Submits a raw CAP XML string directly (use when you have pre-built XML).
    /// </summary>
    public async Task<IpawsResponse> SubmitXmlAsync(
        string capXml,
        string? alertIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capXml);

        int attempt = 0;
        while (true)
        {
            attempt++;
            var response = await TrySubmitOnceAsync(capXml, alertIdentifier, cancellationToken);

            if (response.IsSuccess)
                return response;

            // Don't retry on logic errors — only transient network/server faults
            bool isRetryable = response.Status is SubmissionStatus.NetworkError or SubmissionStatus.Timeout
                               || response.HttpStatusCode is 429 or >= 500;

            if (!isRetryable || attempt > _config.MaxRetries)
                return response;

            await Task.Delay(_config.RetryDelay * attempt, cancellationToken);
        }
    }

    // ── Internal HTTP logic ───────────────────────────────────────────────────

    private async Task<IpawsResponse> TrySubmitOnceAsync(
        string capXml,
        string? alertId,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var content = new StringContent(capXml, Encoding.UTF8, "application/xml");
            using var request = new HttpRequestMessage(HttpMethod.Post, _config.Endpoint)
            {
                Content = content
            };

            // COG ID header (FEMA may require this)
            if (!string.IsNullOrWhiteSpace(_config.CogId))
                request.Headers.TryAddWithoutValidation("X-IPAWS-CogId", _config.CogId);

            using var httpResponse = await _http.SendAsync(request, ct);
            sw.Stop();

            var body = await httpResponse.Content.ReadAsStringAsync(ct);
            int code = (int)httpResponse.StatusCode;

            if (httpResponse.IsSuccessStatusCode)
            {
                var serverId = ExtractServerMessageId(body);
                return IpawsResponse.Ok(alertId ?? string.Empty, serverId, body, sw.Elapsed);
            }

            // Parse error body
            var errors = ParseErrorBody(body, code);
            return IpawsResponse.Fail(
                code is 400 or 401 or 403 or 422
                    ? SubmissionStatus.Rejected
                    : SubmissionStatus.UnexpectedHttpStatus,
                code,
                errors,
                body,
                alertId);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or TaskCanceledException)
        {
            sw.Stop();
            return IpawsResponse.NetworkFailure(ex, alertId);
        }
    }

    // ── Certificate loading ───────────────────────────────────────────────────

    private HttpClientHandler BuildHandler()
    {
        var handler = new HttpClientHandler
        {
            // Always validate the server cert in production
            ServerCertificateCustomValidationCallback = null
        };

        var cert = LoadCertificate();
        if (cert is not null)
            handler.ClientCertificates.Add(cert);

        return handler;
    }

    private X509Certificate2? LoadCertificate()
    {
        // Option 1: Load by thumbprint from Windows Certificate Store
        if (!string.IsNullOrWhiteSpace(_config.CertThumbprint))
        {
            using var store = new X509Store(StoreName.My, _config.CertStoreLocation);
            store.Open(OpenFlags.ReadOnly);
            var thumb = _config.CertThumbprint.Replace(" ", "").ToUpperInvariant();
            var found = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumb, validOnly: true);
            if (found.Count > 0)
                return found[0];

            throw new InvalidOperationException(
                $"Certificate with thumbprint '{_config.CertThumbprint}' not found in store " +
                $"{_config.CertStoreLocation}/My.");
        }

        // Option 2: Load from .p12/.pfx file
        if (!string.IsNullOrWhiteSpace(_config.CertificatePath))
        {
            if (!File.Exists(_config.CertificatePath))
                throw new FileNotFoundException(
                    $"Certificate file not found: {_config.CertificatePath}");

        return X509CertificateLoader.LoadPkcs12FromFile(
            _config.CertificatePath,
            _config.CertificatePassword,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
        }

        return null; // No cert configured — submissions will likely fail auth
    }

    // ── Response parsing helpers ──────────────────────────────────────────────

    /// <summary>
    /// Attempts to extract a server-assigned message identifier from the
    /// IPAWS-OPEN response body (XML or plain text).
    /// </summary>
    private static string? ExtractServerMessageId(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            var doc = XDocument.Parse(body);
            // IPAWS-OPEN typically returns something like:
            // <response><messageId>…</messageId><status>Success</status></response>
            return doc.Descendants()
                      .FirstOrDefault(e => e.Name.LocalName.Equals(
                          "messageId", StringComparison.OrdinalIgnoreCase))
                      ?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> ParseErrorBody(string body, int statusCode)
{
    if (string.IsNullOrWhiteSpace(body))
        return [$"HTTP {statusCode} with empty body."];

    try
    {
        var doc = XDocument.Parse(body);
        var messages = doc.Descendants()
            .Where(e => e.Name.LocalName.Equals("error", StringComparison.OrdinalIgnoreCase)
                     || e.Name.LocalName.Equals("message", StringComparison.OrdinalIgnoreCase)
                     || e.Name.LocalName.Equals("description", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();

        if (messages.Count > 0)
            return messages;
    }
    catch { /* fall through to plain text */ }

    return [$"HTTP {statusCode}: {body.Trim()[..Math.Min(500, body.Length)]}"];
}

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
    }
}
