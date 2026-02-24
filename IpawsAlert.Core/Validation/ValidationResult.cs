namespace IpawsAlert.Core.Validation;

/// <summary>Severity of a validation finding.</summary>
public enum ValidationSeverity
{
    /// <summary>Non-blocking advisory — the alert will likely be accepted but may not behave as intended.</summary>
    Warning,
    /// <summary>The alert is invalid and will be rejected by IPAWS-OPEN.</summary>
    Error
}

/// <summary>A single validation finding.</summary>
public sealed record ValidationFinding(
    ValidationSeverity Severity,
    string             Code,
    string             Message,
    string?            Path = null   // e.g. "InfoBlocks[0].Areas[0]"
);

/// <summary>
/// Aggregated result of validating a <see cref="Models.CapAlert"/>.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationFinding> _findings = new();

    public IReadOnlyList<ValidationFinding> Findings => _findings;

    /// <summary>True when there are no Error-severity findings.</summary>
    public bool IsValid => _findings.All(f => f.Severity != ValidationSeverity.Error);

    public IEnumerable<ValidationFinding> Errors   => _findings.Where(f => f.Severity == ValidationSeverity.Error);
    public IEnumerable<ValidationFinding> Warnings => _findings.Where(f => f.Severity == ValidationSeverity.Warning);

    internal void AddError  (string code, string message, string? path = null) =>
        _findings.Add(new ValidationFinding(ValidationSeverity.Error,   code, message, path));

    internal void AddWarning(string code, string message, string? path = null) =>
        _findings.Add(new ValidationFinding(ValidationSeverity.Warning, code, message, path));

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if there are any errors.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            var msgs = string.Join("\n  • ", Errors.Select(e => $"[{e.Code}] {e.Message}"));
            throw new InvalidOperationException($"CAP alert validation failed:\n  • {msgs}");
        }
    }
}
