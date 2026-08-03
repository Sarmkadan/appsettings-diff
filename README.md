## EnvVarOverlay

The `EnvVarOverlay` class overlays environment variables on top of a configuration, filtering and stripping custom prefixes. It provides methods to apply the overlay, normalize environment variables, and check for sensitivity.

### Public Members
- `ReadFromEnvironment(string? prefix = null)` - Reads environment variables with an optional prefix filter.
- `Normalize(IDictionary<string, string> envVars)` - Normalizes environment variables by removing ASP.NET Core prefixes and replacing `__` with `:`.
- `Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, out List<string> overriddenKeys)` - Applies environment variables to a configuration, returning the updated configuration and a list of overridden keys.
- `Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, string? prefix, out List<string> overriddenKeys)` - Applies environment variables to a configuration with an optional prefix filter, returning the updated configuration and a list of overridden keys.

### Example usage:
```csharp
using AppsettingsDiff;

var config = new Dictionary<string, string> { { "Timeout", "30" }, { "Enabled", "true" } };
var envVars = new Dictionary<string, string> { { "Timeout", "60" }, { "Enabled", "false" } };

var result = EnvVarOverlay.Apply(config, envVars);
Console.WriteLine($"Updated config: {result}");
Console.WriteLine($"Overridden keys: {result.overriddenKeys}");
```

## JsonPatchOperation

`JsonPatchOperation` represents a single operation in a JSON Patch document, following RFC 6902. It exposes the operation type (`Op`), the target JSON pointer (`Path`), optional value (`Value`) or source pointer (`From`), and helper methods to serialize the operation or write it to the console in various formats.

### Example usage
```csharp
using AppsettingsDiff;

var patch = new JsonPatchOperation
{
    Op = "replace",
    Path = "/Logging/LogLevel/Default",
    Value = "Warning"
};

string jsonPatch = patch.ToJsonPatch();
Console.WriteLine(jsonPatch);          // Outputs the JSON Patch string

patch.WriteConsole();                  // Writes a human‑readable representation to the console
patch.WriteMarkdown();                 // Writes a Markdown table to the console
patch.WriteHtml();                     // Writes an HTML table to the console
```
