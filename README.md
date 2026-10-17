## Getting Started

AppsettingsDiff is an executable project (`appsettings-diff.csproj`, assembly `AppsettingsDiff`, target `net10.0`). It is not packaged as a .NET tool (no `PackAsTool`), so `dotnet tool install` does not apply. To use it from your own code, clone this repository and add a project reference.

### Installation

```bash
git clone <repository-url> appsettings-diff
dotnet new console -o DiffDemo
cd DiffDemo
dotnet add reference ../appsettings-diff/appsettings-diff.csproj
```

Adjust the clone URL and the reference path to match your layout.

### Example

Replace the generated `Program.cs` with the following. It reads two JSON files with `JsonConfigReader`, compares them with `ConfigDiffer`, and prints each Added, Removed, and Changed entry. Values under sensitive keys are masked as `***`.

```csharp
using AppsettingsDiff;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- <baseline.json> <target.json>");
    return 1;
}

var reader = new JsonConfigReader();
var baseline = new FlatConfig();
foreach (var (key, value) in reader.ReadJsonConfig(args[0]))
    baseline.Values[key] = value;

var target = new FlatConfig();
foreach (var (key, value) in reader.ReadJsonConfig(args[1]))
    target.Values[key] = value;

var differ = new ConfigDiffer(new SensitiveKeyDetector());
var result = differ.Diff(baseline, target, basePath: args[0], targetPath: args[1]);

if (!result.HasDifferences)
{
    Console.WriteLine("No differences found.");
    return 0;
}

foreach (var entry in result.Entries)
{
    switch (entry.Kind)
    {
        case DiffKind.Added:
            Console.WriteLine($"Added    {entry.Key} = {Show(entry, entry.NewValue)}");
            break;
        case DiffKind.Removed:
            Console.WriteLine($"Removed  {entry.Key} (was {Show(entry, entry.OldValue)})");
            break;
        case DiffKind.Changed:
            Console.WriteLine($"Changed  {entry.Key}: {Show(entry, entry.OldValue)} -> {Show(entry, entry.NewValue)}");
            break;
    }
}

return 0;

static string Show(DiffEntry entry, string? value) => entry.IsSensitive ? "***" : $"\"{value}\"";
```

`JsonConfigReader.ReadJsonConfig` returns a `Dictionary<string, string>`, so each file is copied into a `FlatConfig` (the type `ConfigDiffer.Diff` expects). Keys are flattened with dots (`Logging.LogLevel.Default`), and array items use indexes (`Items[0]`).

### Sample input

`appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "Default": "Server=dev-sql;Database=shop;User Id=app;Password=dev-pass"
  },
  "Features": {
    "NewCheckout": true,
    "LegacySearch": true
  }
}
```

`appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "ConnectionStrings": {
    "Default": "Server=prod-sql;Database=shop;User Id=app;Password=prod-pass"
  },
  "Features": {
    "NewCheckout": false,
    "Beta": true
  }
}
```

### Run

```bash
dotnet run -- appsettings.json appsettings.Production.json
```

Expected output:

```text
Changed  Logging.LogLevel.Default: "Information" -> "Warning"
Changed  ConnectionStrings.Default: *** -> ***
Changed  Features.NewCheckout: "true" -> "false"
Removed  Features.LegacySearch (was "true")
Added    Features.Beta = "true"
```

`ConnectionStrings.Default` is flagged as sensitive, so its values are masked. Entries come out in two passes: keys from the baseline first (Changed and Removed), then keys that only exist in the target (Added).

## ConsoleDiffReportWriter

The ConsoleDiffReportWriter class is responsible for writing console diff reports. It provides methods to write reports in various formats, including JSON, Markdown, and HTML.

Example usage:
```csharp
public ConsoleDiffReportWriter writer = new ConsoleDiffReportWriter();
writer.WriteConsole();
string json = writer.ToJson();
writer.WriteMarkdown();
writer.WriteHtml();
string jsonPatch = writer.ToJsonPatch();
```

## SummaryJsonDiffReportWriter

The SummaryJsonDiffReportWriter writes a compact JSON summary of the diff, showing only counts and lists of changed keys for added, removed, changed, and type-changed entries.
It does not support console, markdown, HTML, or JSON Patch output (those methods throw NotSupportedException).
Example usage:
```csharp
var detector = new SensitiveKeyDetector();
var writer = new SummaryJsonDiffReportWriter(detector);
DiffResult diffResult = ...; // obtain from a diff operation
string json = writer.ToJson(diffResult);
```

## ConfigDifferOptions

`ConfigDifferOptions` allows you to customize how configuration differences are detected and reported. It provides properties to control array comparison behavior (`UnorderedArrays`), limit recursion depth during traversal (`MaxDepth`), and filter results by a specific configuration path (`PathPrefix`).

Example usage:
```csharp
var options = new ConfigDifferOptions
{
    UnorderedArrays = true,
    MaxDepth = 10,
    PathPrefix = "AppSettings"
};

var differ = new ConfigDiffer(options);
var result = differ.Compare(configA, configB);
```

## Generating an HTML report

`HtmlDiffReportWriter` writes a self-contained HTML file (inline CSS, no external assets). The example below is a complete program that:

1. loads a baseline and a target `appsettings` file with `JsonConfigReader`,
2. optionally overlays `APPSETTINGS_`-prefixed environment variables onto the target with `EnvVarOverlay`,
3. runs `ConfigDiffer` with a `SensitiveKeyDetector`, which flags sensitive entries,
4. writes `diff-report.html` with `HtmlDiffReportWriter` in masking mode.

Create the project as shown in [Installation](#installation), replace `Program.cs` with the code below, and put the sample `appsettings.json` and `appsettings.Production.json` from [Sample input](#sample-input) next to it.

```csharp
using System.Collections;
using System.Text;
using AppsettingsDiff;

var baselinePath = "appsettings.json";
var targetPath = "appsettings.Production.json";

var reader = new JsonConfigReader();

var baseline = new FlatConfig();
foreach (var (key, value) in reader.ReadJsonConfig(baselinePath))
    baseline.Values[key] = value;

var targetValues = reader.ReadJsonConfig(targetPath);

// Optional: overlay environment variables prefixed with APPSETTINGS_ onto the target side.
// EnvVarOverlay turns "__" into ":", but JsonConfigReader flattens keys with ".", so map "__" to "." first.
var envVars = Environment.GetEnvironmentVariables()
    .Cast<DictionaryEntry>()
    .ToDictionary(
        e => ((string)e.Key).Replace("__", ".", StringComparison.Ordinal),
        e => (string?)e.Value ?? string.Empty);
targetValues = EnvVarOverlay.Apply(targetValues, envVars, "APPSETTINGS_", out var overriddenKeys);

var target = new FlatConfig();
foreach (var (key, value) in targetValues)
    target.Values[key] = value;

// The same detector drives both the diff (which flags sensitive entries) and the HTML writer.
var detector = new SensitiveKeyDetector();
var differ = new ConfigDiffer(detector);
var result = differ.Diff(baseline, target, basePath: baselinePath, targetPath: targetPath);

var writer = new HtmlDiffReportWriter(detector, showSecrets: false, maskSensitive: true);
using (var file = new StreamWriter("diff-report.html", append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
{
    writer.WriteHtml(result, file);
}

Console.WriteLine($"Wrote diff-report.html: {result.CountOf(DiffKind.Added)} added, " +
                  $"{result.CountOf(DiffKind.Removed)} removed, {result.CountOf(DiffKind.Changed)} changed.");
if (overriddenKeys.Count > 0)
    Console.WriteLine($"Environment overrides applied: {string.Join(", ", overriddenKeys)}");
```

Run it, optionally with an override such as `APPSETTINGS_Features__Beta=false`:

```bash
dotnet run
APPSETTINGS_Features__Beta=false dotnet run
```

### What the report shows

- **Summary:** counts of Added, Removed, Changed, and TypeChanged entries.
- **Details table:** one row per differing key, with the key, the change kind, and the old and new values. Rows are colour-coded: Added is green, Removed is red, Changed is yellow, and TypeChanged is purple.
- **Masked secrets:** values under sensitive keys (for example `ConnectionStrings.Default` or anything matching a password or token pattern) are shown as `***`. `maskSensitive: true` selects `***`; leaving it `false` shows `[REDACTED]` instead. Pass `showSecrets: true` only when the report stays on a trusted machine.

With the sample input, `ConnectionStrings.Default` appears as `*** → ***`, and the other values are shown in plain text.

### Notes

- `HtmlDiffReportWriter` decides masking from `DiffEntry.IsSensitive`, which `ConfigDiffer` sets from its detector. The writer's own detector argument is not consulted when rendering HTML. Pass the same detector to both so the flags and the writer's configuration stay consistent.
- `EnvVarOverlay` keeps `__` as `:`, while `JsonConfigReader` flattens keys with `.`. Map `__` to `.` before overlaying, as the example does. Otherwise an override creates a new `Features:Beta` key instead of replacing `Features.Beta`.

### CLI equivalent

The project also ships a CLI with an `html` format. It writes to standard output, so redirect it to a file:

```bash
dotnet run -- diff appsettings.json appsettings.Production.json --format html --mask-sensitive > diff-report.html
```
