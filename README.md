# appsettings-diff

Diffs appsettings across environments and finds keys missing between them.

## ConfigSchema

The `ConfigSchema` class represents a configuration schema, which is used to validate and compare configuration files. It contains a list of required keys, type hints for each key, and methods to load a schema from JSON and validate a configuration against it.

### Example usage:

## DiffReportWriterExtensions

The `DiffReportWriterExtensions` class provides convenient extension methods for `DiffReportWriter` to facilitate the processing and reporting of configuration differences. These helpers allow for quick console output, JSON serialization, and Markdown summary generation, while also simplifying checks for the existence and volume of differences.

### Example usage:

```csharp
using AppsettingsDiff;

var writer = new DiffReportWriter();
var result = /* Obtain a DiffResult from ConfigDiffer */;

// Check if there are any differences
if (writer.HasDifferences(result))
{
    Console.WriteLine($"Total differences found: {writer.GetTotalDifferenceCount(result)}");

    // Output summary
    writer.WriteConsoleSummary(result);
}

// Export data
string json = writer.ToCompactJson(result);
string markdown = writer.ToMarkdownSummary(result);
```

## SensitiveKeyDetector

The `SensitiveKeyDetector` class is used to identify configuration keys that may contain sensitive information based on a predefined set of patterns. It provides a simple method to check if a given key is considered sensitive.

### Example usage:

```csharp
using AppsettingsDiff;

var detector = new SensitiveKeyDetector();
string keyToCheck = "ConnectionStrings:DefaultConnection";

if (detector.IsSensitive(keyToCheck))
{    Console.WriteLine($"The key '{keyToCheck}' is sensitive.");
}
```

## SensitiveKeyDetectorExtensions

`SensitiveKeyDetectorExtensions` adds a collection of helpful extension methods that build on `SensitiveKeyDetector` to evaluate keys for sensitivity, categorize them, and decide whether they need special handling. These methods let you quickly determine if a key is sensitive, potentially sensitive, related to databases or APIs, its overall sensitivity level, and whether extra caution is warranted.

### Example usage:

```csharp
using AppsettingsDiff;

var detector = new SensitiveKeyDetector();

string[] keys = { "ConnectionStrings:Default", "ApiKey", "DbPassword", "Logging:LogLevel" };

foreach (var key in keys)
{
    Console.WriteLine($"{key}:");
    Console.WriteLine($"  Sensitive? {detector.IsSensitiveKey(key)}");
    Console.WriteLine($"  Potentially Sensitive? {detector.IsPotentiallySensitive(key)}");
    Console.WriteLine($"  Database Credential? {detector.IsDatabaseCredential(key)}");
    Console.WriteLine($"  API Credential? {detector.IsApiCredential(key)}");
    Console.WriteLine($"  Sensitivity Level: {detector.GetSensitivityLevel(key)}");
    Console.WriteLine($"  Requires Extra Caution? {detector.RequiresExtraCaution(key)}");
}
```

## MergeResult

The `MergeResult` class stores the outcome of a three-way merge operation. It contains the resulting configuration dictionary and a list of any merge conflicts encountered, allowing developers to programmatically inspect the outcome.

### Example usage:

```csharp
using AppsettingsDiff;

var baseConfig = new Dictionary<string, string> { { "Key1", "BaseValue" } };
var ours = new Dictionary<string, string> { { "Key1", "OurValue" } };
var theirs = new Dictionary<string, string> { { "Key1", "TheirValue" } };

var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

// Access the merged configuration
foreach (var kvp in result.Merged)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Handle any conflicts
foreach (var conflict in result.Conflicts)
{
    Console.WriteLine($"Conflict at {conflict.Key}: Base='{conflict.BaseValue}', Ours='{conflict.OurValue}', Theirs='{conflict.TheirValue}'");
}
```
