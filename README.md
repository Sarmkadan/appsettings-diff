# appsettings-diff

Diffs appsettings across environments and finds keys missing between them.

## ConfigSchema

The `ConfigSchema` class represents a configuration schema, which is used to validate and compare configuration files. It contains a list of required keys, type hints for each key, and methods to load a schema from JSON and validate a configuration against it.

### Example usage:

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
