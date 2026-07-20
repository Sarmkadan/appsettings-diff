# appsettings-diff

Diff appsettings across environments and finds keys missing between them.

## DiffReportWriter

The `DiffReportWriter` class generates human-readable reports from configuration diff results. It supports console output with color coding, JSON serialization, and Markdown table generation. Sensitive values are automatically redacted unless explicitly configured otherwise.

### Public Members
- `DiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false)` - Constructor
- `void WriteConsole(DiffResult result)` - Writes a color-coded console table
- `string ToJson(DiffResult result)` - Serializes to indented JSON
- `string ToJson(DiffResult result, bool indented)` - Serializes to JSON with formatting control
- `void WriteMarkdown(DiffResult result, TextWriter writer)` - Writes a Markdown table

### Example usage:

```csharp
using AppsettingsDiff;

// Create a sensitive key detector and writer
var detector = new SensitiveKeyDetector();
var writer = new DiffReportWriter(detector);

// Assume we have a DiffResult from ConfigDiffer
DiffResult result = /* obtain DiffResult */;

// Write to console with color coding
writer.WriteConsole(result);

// Export as JSON
string json = writer.ToJson(result);
Console.WriteLine(json);

// Export as Markdown (suitable for PR comments)
writer.WriteMarkdown(result, Console.Out);

// Export compact JSON
string compactJson = writer.ToJson(result, indented: false);
```

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
{
 Console.WriteLine($"The key '{keyToCheck}' is sensitive.");
}
```

## MergeResultExtensions

The `MergeResultExtensions` class provides a collection of extension methods for `MergeResult` that simplify common operations like retrieving values with type conversion, checking for key existence, and managing merge conflicts. These helpers allow for concise and readable code when working with merge results, supporting string, integer, boolean, and decimal value retrieval with automatic type conversion and fallback to default values.

### Example usage:

```csharp
using AppsettingsDiff;

var baseConfig = new Dictionary<string, string> { { "Timeout", "30" }, { "Enabled", "true" }, { "Rate", "99.5" } };
var ours = new Dictionary<string, string> { { "Timeout", "60" }, { "Enabled", "false" } };
var theirs = new Dictionary<string, string> { { "Timeout", "45" }, { "Rate", "120.75" } };

var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

// Get values with type conversion and defaults
int timeout = result.GetValueOrDefaultAsInt("Timeout", 10);
bool enabled = result.GetValueOrDefaultAsBool("Enabled", true);
decimal rate = result.GetValueOrDefaultAsDecimal("Rate", 0m);
string missingKey = result.GetValueOrDefault("NonExistentKey", "default");

// Check key existence and count
bool hasTimeout = result.ContainsKey("Timeout");
int keyCount = result.Count();
IEnumerable<string> allKeys = result.GetKeys();

// Try to get a value
if (result.TryGetValue("Timeout", out var timeoutValue))
{
 Console.WriteLine($"Timeout value: {timeoutValue}");
}

// Work with conflicts
if (result.HasConflicts())
{
 Console.WriteLine($"Merge conflicts found: {result.GetConflictedKeys().Count}");

 foreach (var conflictedKey in result.GetConflictedKeys())
 {
 var conflict = result.GetConflict(conflictedKey);
 if (conflict != null)
 {
 Console.WriteLine($"Conflict at {conflict.Key}: Base='{conflict.BaseValue}', Ours='{conflict.OurValue}', Theirs='{conflict.TheirValue}'");
 }
 }

 IReadOnlyList<MergeConflict> allConflicts = result.GetConflicts();
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
Console.WriteLine($" Sensitive? {detector.IsSensitiveKey(key)}");
Console.WriteLine($" Potentially Sensitive? {detector.IsPotentiallySensitive(key)}");
Console.WriteLine($" Database Credential? {detector.IsDatabaseCredential(key)}");
Console.WriteLine($" API Credential? {detector.IsApiCredential(key)}");
Console.WriteLine($" Sensitivity Level: {detector.GetSensitivityLevel(key)}");
Console.WriteLine($" Requires Extra Caution? {detector.RequiresExtraCaution(key)}");
}
```

## DiffReportWriterJsonExtensions

`DiffReportWriterJsonExtensions` adds JSON‑serialization helpers for `DiffReportWriter`. It lets you convert a `DiffResult` to a JSON string (optionally indented) and back again, while handling redaction of sensitive values based on a `SensitiveKeyDetector`. The extension also provides a `TryFromJson` method that safely attempts deserialization.

### Example usage:

```csharp
using AppsettingsDiff;

var writer = new DiffReportWriter();
var detector = new SensitiveKeyDetector();

// Assume we have a DiffResult from ConfigDiffer
DiffResult result = /* obtain DiffResult */;

// Serialize the result to indented JSON
string json = writer.ToJson(result, indented: true);

// Deserialize, redacting sensitive values unless we explicitly request them
diffResult? deserialized = DiffReportWriterJsonExtensions.FromJson(json, detector, showSecrets: false);

if (deserialized != null)
{
Console.WriteLine($"BasePath: {deserialized.BasePath}");
Console.WriteLine($"TargetPath: {deserialized.TargetPath}");

foreach (var entry in deserialized.Entries)
{
Console.WriteLine($"{entry.Kind}: {entry.Key}");
Console.WriteLine($" Old: {entry.OldValue}");
Console.WriteLine($" New: {entry.NewValue}");
Console.WriteLine($" Sensitive: {entry.IsSensitive}");
}
}

// Safe try‑parse variant
if (DiffReportWriterJsonExtensions.TryFromJson(json, detector, out var parsed, showSecrets: true))
{
// `parsed` is a fully populated DiffResult
Console.WriteLine($"Parsed {parsed.Entries.Count} entries successfully.");
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
