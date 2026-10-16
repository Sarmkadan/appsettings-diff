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
