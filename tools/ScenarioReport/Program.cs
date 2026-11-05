using System.Globalization;
using System.Net;
using System.Text;
using System.Xml.Linq;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: ScenarioReport <results.trx> <features-directory> <output-directory>");
    return 2;
}

var trxPath = Path.GetFullPath(args[0]);
var featureDirectory = Path.GetFullPath(args[1]);
var outputDirectory = Path.GetFullPath(args[2]);

if (!File.Exists(trxPath))
{
    Console.Error.WriteLine($"TRX results file not found: {trxPath}");
    return 1;
}

var document = XDocument.Load(trxPath);
var root = document.Root ?? throw new InvalidDataException("The TRX file has no root element.");
XNamespace ns = root.Name.Namespace;

var scenarioNames = LoadScenarioNames(featureDirectory);
var results = root.Element(ns + "Results")?
    .Elements(ns + "UnitTestResult")
    .Select(result =>
    {
        var testName = (string?)result.Attribute("testName") ?? "Unknown scenario";
        var normalizedName = Normalize(testName);
        var scenarioName = scenarioNames.TryGetValue(normalizedName, out var featureName)
            ? featureName
            : testName;
        var outcome = (string?)result.Attribute("outcome") ?? "Unknown";
        var durationText = (string?)result.Attribute("duration");
        var duration = TimeSpan.TryParse(durationText, CultureInfo.InvariantCulture, out var parsedDuration)
            ? parsedDuration
            : TimeSpan.Zero;
        var failureMessage = result.Element(ns + "Output")?
            .Element(ns + "ErrorInfo")?
            .Element(ns + "Message")?
            .Value;

        return new ScenarioResult(scenarioName, outcome, duration, failureMessage);
    })
    .OrderBy(result => result.Scenario, StringComparer.OrdinalIgnoreCase)
    .ToList() ?? [];

if (results.Count == 0)
{
    Console.Error.WriteLine("No scenario results were found in the TRX file.");
    return 1;
}

var finishText = (string?)root.Element(ns + "Times")?.Attribute("finish");
var finishedAt = DateTimeOffset.TryParse(finishText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedFinish)
    ? parsedFinish
    : File.GetLastWriteTimeUtc(trxPath);
var runId = ((string?)root.Attribute("id") ?? Guid.NewGuid().ToString("N"))
    .Replace("-", string.Empty, StringComparison.Ordinal);
var reportDirectory = Path.Combine(outputDirectory, "ScenarioReports");
Directory.CreateDirectory(reportDirectory);

var reportPath = Path.Combine(
    reportDirectory,
    $"TestRun-{finishedAt.ToUniversalTime():yyyyMMdd-HHmmss}-{runId[..8]}.html");
var html = BuildReport(results, finishedAt);
await File.WriteAllTextAsync(reportPath, html, new UTF8Encoding(false));
await File.WriteAllTextAsync(Path.Combine(outputDirectory, "latest.html"), html, new UTF8Encoding(false));

Console.WriteLine($"HTML scenario report: {reportPath}");
Console.WriteLine($"Latest report: {Path.Combine(outputDirectory, "latest.html")}");
return 0;

static Dictionary<string, string> LoadScenarioNames(string directory)
{
    var scenarios = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var featureFile in Directory.EnumerateFiles(directory, "*.feature", SearchOption.AllDirectories))
    {
        foreach (var line in File.ReadLines(featureFile))
        {
            var trimmed = line.Trim();
            var prefix = trimmed.StartsWith("Scenario Outline:", StringComparison.Ordinal)
                ? "Scenario Outline:"
                : trimmed.StartsWith("Scenario:", StringComparison.Ordinal)
                    ? "Scenario:"
                    : null;

            if (prefix is null)
            {
                continue;
            }

            var title = trimmed[prefix.Length..].Trim();
            scenarios[Normalize(title)] = title;
        }
    }

    return scenarios;
}

static string Normalize(string value)
{
    return string.Concat(value.Where(char.IsLetterOrDigit));
}

static string BuildReport(IReadOnlyCollection<ScenarioResult> results, DateTimeOffset finishedAt)
{
    var passed = results.Count(result => result.Outcome.Equals("Passed", StringComparison.OrdinalIgnoreCase));
    var skipped = results.Count(result =>
        result.Outcome.Equals("NotExecuted", StringComparison.OrdinalIgnoreCase) ||
        result.Outcome.Equals("Skipped", StringComparison.OrdinalIgnoreCase));
    var failed = results.Count - passed - skipped;
    var rows = string.Join(Environment.NewLine, results.Select(BuildRow));

    return $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>API Scenario Test Report</title>
          <style>
            :root { color-scheme: light; font-family: "Segoe UI", Arial, sans-serif; color: #202a35; background: #f3f6f8; }
            body { margin: 0; padding: 32px 18px; }
            main { max-width: 1000px; margin: 0 auto; }
            h1 { margin: 0; font-size: 1.8rem; }
            .subtitle { margin: 8px 0 24px; color: #5b6773; }
            .summary { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 20px; }
            .metric { min-width: 120px; padding: 14px 18px; background: #fff; border: 1px solid #dce3e8; border-radius: 6px; }
            .metric strong { display: block; font-size: 1.45rem; }
            .metric span { color: #5b6773; font-size: .88rem; }
            .passed { color: #167447; }
            .failed { color: #b42318; }
            .skipped { color: #8a5a00; }
            .table-wrap { overflow-x: auto; background: #fff; border: 1px solid #dce3e8; border-radius: 6px; }
            table { width: 100%; border-collapse: collapse; }
            th, td { padding: 13px 15px; border-bottom: 1px solid #e8edf0; text-align: left; vertical-align: top; }
            th { background: #f8fafb; color: #475461; font-size: .85rem; }
            tbody tr:last-child td { border-bottom: 0; }
            .status { font-weight: 700; }
            .details { color: #8d211b; white-space: pre-wrap; }
            @media (max-width: 600px) { body { padding: 20px 10px; } th, td { padding: 10px; } }
          </style>
        </head>
        <body>
          <main>
            <h1>API Scenario Test Report</h1>
            <p class="subtitle">Completed {{WebUtility.HtmlEncode(finishedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture))}}</p>
            <section class="summary" aria-label="Test summary">
              <div class="metric"><strong>{{results.Count}}</strong><span>Total scenarios</span></div>
              <div class="metric passed"><strong>{{passed}}</strong><span>Passed</span></div>
              <div class="metric failed"><strong>{{failed}}</strong><span>Failed</span></div>
              <div class="metric skipped"><strong>{{skipped}}</strong><span>Skipped</span></div>
            </section>
            <div class="table-wrap">
              <table>
                <thead><tr><th>Scenario</th><th>Result</th><th>Duration</th><th>Failure details</th></tr></thead>
                <tbody>
                  {{rows}}
                </tbody>
              </table>
            </div>
          </main>
        </body>
        </html>
        """;
}

static string BuildRow(ScenarioResult result)
{
    var statusClass = result.Outcome.ToLowerInvariant() switch
    {
        "passed" => "passed",
        "notexecuted" or "skipped" => "skipped",
        _ => "failed"
    };

    return $"<tr><td>{WebUtility.HtmlEncode(result.Scenario)}</td>" +
        $"<td class=\"status {statusClass}\">{WebUtility.HtmlEncode(result.Outcome)}</td>" +
        $"<td>{WebUtility.HtmlEncode(result.Duration.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture))}</td>" +
        $"<td class=\"details\">{WebUtility.HtmlEncode(result.FailureMessage ?? string.Empty)}</td></tr>";
}

sealed record ScenarioResult(string Scenario, string Outcome, TimeSpan Duration, string? FailureMessage);