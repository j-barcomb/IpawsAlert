// ═══════════════════════════════════════════════════════════════════════════════
// IpawsAlert.Core — Usage Examples
// ═══════════════════════════════════════════════════════════════════════════════
// These examples illustrate how to build, validate, serialize, and submit
// CAP v1.2 alerts using the IpawsAlert.Core library.
// ═══════════════════════════════════════════════════════════════════════════════

using IpawsAlert.Core.Builders;
using IpawsAlert.Core.Channels;
using IpawsAlert.Core.Client;
using IpawsAlert.Core.Models;
using IpawsAlert.Core.Validation;

// ── Example 1: Tornado Warning — WEA + EAS + IPAWS-OPEN ────────────────────────
static async Task Example_TornadoWarning()
{
    // Build the alert using the fluent builder
    var alert = new CapAlertBuilder()
        .WithSender("w-nws.webmaster@noaa.gov")
        .WithStatus(CapStatus.Test)          // Use CapStatus.Actual for real alerts
        .WithMsgType(CapMsgType.Alert)
        .WithScope(CapScope.Public)
        .AddInfo(info => info
            .WithLanguage("en-US")
            .WithCategory(CapCategory.Met)
            .WithEvent("Tornado Warning")
            .AddResponseType(CapResponseType.Shelter)
            .WithUrgency(CapUrgency.Immediate)
            .WithSeverity(CapSeverity.Extreme)
            .WithCertainty(CapCertainty.Observed)
            .WithSenderName("National Weather Service Grand Rapids MI")
            .WithHeadline("Tornado Warning issued for Kent County until 6:00 PM EDT")
            .WithDescription(
                "At 5:05 PM EDT, a confirmed large and extremely dangerous tornado was located " +
                "near Caledonia, moving northeast at 45 mph. This is a PARTICULARLY DANGEROUS " +
                "SITUATION. TAKE COVER NOW!")
            .WithInstruction(
                "TAKE COVER NOW! Move to a basement or an interior room on the lowest floor of " +
                "a sturdy building. Avoid windows.")
            .WithEffective(DateTimeOffset.UtcNow)
            .WithExpiry(DateTimeOffset.UtcNow.AddHours(1))
            .AddEventCode("SAME", "TOR")     // EAS SAME event code
            // NWS VTEC string (required for NWS products)
            .AddParameter("VTEC", "/O.NEW.KGRR.TO.W.0001.240601T2105Z-240601T2200Z/")
            // Geographic targeting: Kent County, Michigan (FIPS 26081)
            .AddSameCode("026081")
            // WEA channel: 90-char short text + 360-char long text
            .AddWeaRouting(
                shortText: "Tornado Warning this area until 6PM EDT. Take shelter now! NWS",
                longText:  "Tornado Warning for Kent County MI until 6:00 PM EDT. " +
                           "A confirmed tornado is on the ground near Caledonia moving NE at 45 mph. " +
                           "Take shelter in a sturdy building immediately. Avoid windows."
            )
            // EAS channel
            .AddEasRouting()
        )
        .Build();

    // ── Validate before sending ────────────────────────────────────────────────
    var validation = CapValidator.Validate(alert);

    Console.WriteLine($"Validation: {(validation.IsValid ? "PASS" : "FAIL")}");
    foreach (var finding in validation.Findings)
        Console.WriteLine($"  [{finding.Severity}] {finding.Code}: {finding.Message}");

    if (!validation.IsValid)
    {
        Console.WriteLine("Alert has errors — aborting submission.");
        return;
    }

    // ── Preview the CAP XML ────────────────────────────────────────────────────
    var xml = CapXmlSerializer.Serialize(alert, indent: true);
    Console.WriteLine("\n── CAP XML Preview ──");
    Console.WriteLine(xml[..Math.Min(500, xml.Length)] + "...");

    // ── Submit to IPAWS-OPEN ───────────────────────────────────────────────────
    var config = new IpawsOpenConfig
    {
        Endpoint            = IpawsOpenConfig.TestEndpoint,
        CertificatePath     = @"C:\certs\my-fema-cert.p12",
        CertificatePassword = "your-cert-password",   // Load from secure config in real app
        CogId               = "YOUR-COG-ID",
        Timeout             = TimeSpan.FromSeconds(30),
        MaxRetries          = 3
    };

    using var client = new IpawsClient(config);
    var response = await client.SubmitAsync(alert);

    if (response.IsSuccess)
    {
        Console.WriteLine($"\n✅ Alert submitted successfully!");
        Console.WriteLine($"   Alert ID:  {response.AlertIdentifier}");
        Console.WriteLine($"   Server ID: {response.ServerMessageId}");
        Console.WriteLine($"   Elapsed:   {response.Elapsed?.TotalMilliseconds:F0} ms");
    }
    else
    {
        Console.WriteLine($"\n❌ Submission failed: {response.Status}  (HTTP {response.HttpStatusCode})");
        foreach (var error in response.Errors)
            Console.WriteLine($"   Error: {error}");
    }
}

// ── Example 2: Amber Alert — WEA only ─────────────────────────────────────────
static CapAlert Example_AmberAlert()
{
    return new CapAlertBuilder()
        .WithSender("amber@state.gov")
        .WithStatus(CapStatus.Test)
        .WithMsgType(CapMsgType.Alert)
        .AddInfo(info => info
            .WithCategory(CapCategory.Safety)
            .WithEvent("Child Abduction Emergency")
            .AddResponseType(CapResponseType.Monitor)
            .WithUrgency(CapUrgency.Immediate)
            .WithSeverity(CapSeverity.Severe)
            .WithCertainty(CapCertainty.Likely)
            .WithHeadline("AMBER Alert: Abducted child in blue Honda Civic, plate ABC1234")
            .WithDescription(
                "AMBER Alert: White male child, 8 years old, 4 feet tall, brown hair, " +
                "blue eyes, wearing a red t-shirt. Last seen entering a blue 2019 Honda Civic " +
                "license plate ABC1234 in the downtown area.")
            .WithInstruction("Call 911 immediately if you see the vehicle or child.")
            .WithExpiryDuration(TimeSpan.FromHours(8))
            // Multiple counties in a single area
            .AddArea(area => area
                .WithDescription("Example Metro Area")
                .AddSameCode("039049")  // Franklin County, OH
                .AddSameCode("039041")  // Delaware County, OH
                .AddSameCode("039045")  // Fairfield County, OH
            )
            .AddWeaRouting(
                shortText: "AMBER Alert: Blue Honda Civic OH/ABC1234. 8yr old male. Call 911.",
                longText:  "AMBER Alert: White 8yr old male, 4ft, brown hair, blue eyes, red shirt. " +
                           "Blue 2019 Honda Civic plate ABC1234. Last seen downtown. Call 911 if seen."
            )
        )
        .Build();
}

// ── Example 3: Update / Cancel a previous alert ───────────────────────────────
static CapAlert Example_CancelPreviousAlert(CapAlert original)
{
    return new CapAlertBuilder()
        .WithSender(original.Sender)
        .WithStatus(original.Status)
        .WithMsgType(CapMsgType.Cancel)
        .WithScope(CapScope.Public)
        .AddReference(original.Sender, original.Identifier!, original.Sent)
        .WithNote($"Cancelling alert {original.Identifier}")
        .AddInfo(info => info
            .WithCategory(CapCategory.Met)
            .WithEvent("Tornado Warning")
            .WithUrgency(CapUrgency.Past)
            .WithSeverity(CapSeverity.Unknown)
            .WithCertainty(CapCertainty.Unknown)
            .WithHeadline("Tornado Warning has been cancelled.")
            .WithInstruction("The tornado warning has been cancelled. Remain aware of your surroundings.")
            .WithExpiry(DateTimeOffset.UtcNow.AddMinutes(30))
            .AddSameCode("026081")
        )
        .Build();
}

// ── Example 4: Deserialize incoming CAP XML ───────────────────────────────────
static void Example_Deserialize()
{
    const string incomingXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <alert xmlns="urn:oasis:names:tc:emergency:cap:1.2">
          <identifier>example.gov-20240601T180000Z-001</identifier>
          <sender>test@example.gov</sender>
          <sent>2024-06-01T18:00:00-00:00</sent>
          <status>Test</status>
          <msgType>Alert</msgType>
          <scope>Public</scope>
          <code>IPAWSv1.0</code>
          <info>
            <language>en-US</language>
            <category>Met</category>
            <event>Flash Flood Warning</event>
            <urgency>Immediate</urgency>
            <severity>Severe</severity>
            <certainty>Likely</certainty>
            <expires>2024-06-01T20:00:00-00:00</expires>
            <headline>Flash Flood Warning for Example County</headline>
            <area>
              <areaDesc>Example County</areaDesc>
              <geocode>
                <valueName>SAME</valueName>
                <value>039049</value>
              </geocode>
            </area>
          </info>
        </alert>
        """;

    var deserialized = CapXmlSerializer.Deserialize(incomingXml);
    Console.WriteLine($"Deserialized alert: {deserialized.Identifier}");
    Console.WriteLine($"  Event:    {deserialized.InfoBlocks[0].Event}");
    Console.WriteLine($"  Severity: {deserialized.InfoBlocks[0].Severity}");
}

// ── Run examples ──────────────────────────────────────────────────────────────
Console.WriteLine("=== IpawsAlert.Core Usage Examples ===\n");

Console.WriteLine("--- Example 2: AMBER Alert (build + validate) ---");
var amber = Example_AmberAlert();
var amberValidation = CapValidator.Validate(amber);
Console.WriteLine($"Valid: {amberValidation.IsValid}");
Console.WriteLine(CapXmlSerializer.Serialize(amber, indent: true)[..300] + "...\n");

Console.WriteLine("--- Example 4: Deserialize incoming XML ---");
Example_Deserialize();

Console.WriteLine("\n--- Example 1: Tornado Warning (requires cert for actual send) ---");
Console.WriteLine("Skipping HTTP submission in example. Configure IpawsOpenConfig and call Example_TornadoWarning().");
