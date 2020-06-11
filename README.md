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
{
    Console.WriteLine($"The key '{keyToCheck}' is sensitive.");
}
```
