namespace IpawsAlert.Core.Models;

/// <summary>CAP v1.2 §3.2.1 – Status of the alert message.</summary>
public enum CapStatus
{
    /// <summary>Actionable by all targeted recipients.</summary>
    Actual,
    /// <summary>Actionable only by designated exercise participants.</summary>
    Exercise,
    /// <summary>Technical handling system only, not for public dissemination.</summary>
    System,
    /// <summary>For preliminary or incomplete message testing.</summary>
    Test,
    /// <summary>A preliminary template or draft.</summary>
    Draft
}

/// <summary>CAP v1.2 §3.2.2 – Nature of the alert message.</summary>
public enum CapMsgType
{
    /// <summary>Initial information requiring immediate action.</summary>
    Alert,
    /// <summary>Updates and supersedes an earlier message.</summary>
    Update,
    /// <summary>Cancels an earlier message.</summary>
    Cancel,
    /// <summary>Acknowledges receipt and acceptance of the message.</summary>
    Ack,
    /// <summary>Indicates rejection of the message.</summary>
    Error
}

/// <summary>CAP v1.2 §3.2.3 – Scope / intended distribution of the alert.</summary>
public enum CapScope
{
    /// <summary>For general dissemination to unrestricted audiences.</summary>
    Public,
    /// <summary>For dissemination only to users with a known operational need.</summary>
    Restricted,
    /// <summary>For dissemination only to specified addresses.</summary>
    Private
}

/// <summary>CAP v1.2 §3.2.6 – Category of the subject event.</summary>
[Flags]
public enum CapCategory
{
    None        = 0,
    Geo         = 1 << 0,   // Geophysical
    Met         = 1 << 1,   // Meteorological
    Safety      = 1 << 2,   // General emergency / public safety
    Security    = 1 << 3,   // Law enforcement / military
    Rescue      = 1 << 4,   // Rescue / recovery
    Fire        = 1 << 5,   // Fire suppression / rescue
    Health      = 1 << 6,   // Medical / public health
    Env         = 1 << 7,   // Pollution / environmental
    Transport   = 1 << 8,   // Public / private transportation
    Infra       = 1 << 9,   // Utility / infrastructure
    CBRNE       = 1 << 10,  // Chemical / biological / radiological / nuclear / explosive
    Other       = 1 << 11
}

/// <summary>CAP v1.2 §3.2.7 – Urgency of the subject event.</summary>
public enum CapUrgency
{
    /// <summary>Responsive action should be taken immediately.</summary>
    Immediate,
    /// <summary>Responsive action should be taken soon (within the next hour).</summary>
    Expected,
    /// <summary>Responsive action should be taken in the near future.</summary>
    Future,
    /// <summary>Responsive action is no longer required.</summary>
    Past,
    /// <summary>Urgency not known.</summary>
    Unknown
}

/// <summary>CAP v1.2 §3.2.8 – Severity of the subject event.</summary>
public enum CapSeverity
{
    /// <summary>Extraordinary threat to life or property.</summary>
    Extreme,
    /// <summary>Significant threat to life or property.</summary>
    Severe,
    /// <summary>Possible threat to life or property.</summary>
    Moderate,
    /// <summary>Minimal to no known threat to life or property.</summary>
    Minor,
    /// <summary>Severity unknown.</summary>
    Unknown
}

/// <summary>CAP v1.2 §3.2.9 – Certainty of the subject event.</summary>
public enum CapCertainty
{
    /// <summary>Determined to have occurred or to be ongoing.</summary>
    Observed,
    /// <summary>Highly likely (p > ~85%).</summary>
    Likely,
    /// <summary>Possible but not likely (p ≤ ~85%).</summary>
    Possible,
    /// <summary>Not expected to occur (p ≤ ~2%).</summary>
    Unlikely,
    /// <summary>Certainty unknown.</summary>
    Unknown
}

/// <summary>CAP v1.2 §3.2.5 – Message type for the info block.</summary>
public enum CapResponseType
{
    Shelter,
    Evacuate,
    Prepare,
    Execute,
    Avoid,
    Monitor,
    Assess,
    AllClear,
    None
}
