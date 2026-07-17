# DiffReportWriterJsonExtensions

Provides JSON serialization and deserialization extensions for diff reports, including the data transfer types used in the serialized payload. The static methods enable round-tripping `DiffResult` instances to and from JSON, while the property members define the structure of the serialized output.

## API

### `public static string ToJson(this DiffResult result, JsonSerializerOptions? options = null)`

Serializes a `DiffResult` to a JSON string using the internal DTO structure.

**Parameters**
- `result` – The diff result to serialize. Cannot be null.
- `options` – Optional serializer options. When omitted, a default configuration with camel-case naming and indented output is used.

**Returns**
A JSON string representing the diff report.

**Exceptions**
- `ArgumentNullException` – Thrown if `result` is null.
- `JsonException` – Thrown if serialization fails due to circular references or unsupported types.

---

### `public static DiffResult? FromJson(string json, JsonSerializerOptions? options = null)`

Deserializes a JSON string into a `DiffResult` instance.

**Parameters**
- `json` – The JSON string produced by `ToJson`. Cannot be null or empty.
- `options` – Optional serializer options. Must be compatible with the options used for serialization.

**Returns**
A `DiffResult` instance, or null if the JSON represents an empty report.

**Exceptions**
- `ArgumentNullException` – Thrown if `json` is null.
- `JsonException` – Thrown if the JSON is malformed or does not match the expected schema.

---

### `public static bool TryFromJson(string json, out DiffResult? result, JsonSerializerOptions? options = null)`

Attempts to deserialize a JSON string into a `DiffResult` without throwing on failure.

**Parameters**
- `json` – The JSON string to deserialize.
- `result` – Receives the deserialized `DiffResult` on success; otherwise null.
- `options` – Optional serializer options.

**Returns**
`true` if deserialization succeeded; `false` if the JSON is null, empty, malformed, or schema-incompatible.

**Exceptions**
This method does not throw for invalid input. Other exceptions (e.g., `ArgumentNullException` for `options`) may still propagate.

---

### `public string? BasePath { get; set; }`

Gets or sets the base configuration file path recorded in the report. Null when the diff was produced from in-memory sources.

---

### `public string? TargetPath { get; set; }`

Gets or sets the target configuration file path recorded in the report. Null when the diff was produced from in-memory sources.

---

### `public List<DiffEntryJson>? Entries { get; set; }`

Gets or sets the collection of individual difference entries. Each entry captures a single key-level change. Null when the report contains no differences.

---

### `public string? Kind { get; set; }`

Gets or sets the kind of difference for this entry. Typical values: `"Added"`, `"Removed"`, `"Changed"`, `"TypeChanged"`.

---

### `public string? Key { get; set; }`

Gets or sets the configuration key this entry refers to. Uses the flattened key notation (e.g., `"Logging:LogLevel:Default"`).

---

### `public string? OldValue { get; set; }`

Gets or sets the value from the base configuration. Null when the entry kind is `"Added"`.

---

### `public string? NewValue { get; set; }`

Gets or sets the value from the target configuration. Null when the entry kind is `"Removed"`.

---

### `public bool IsSensitive { get; set; }`

Gets or sets a value indicating whether the entry contains sensitive data (secrets, connection strings, etc.). When true, consumers should avoid logging `OldValue` and `NewValue`.

---

### `public string? Path { get; set; }`

Gets or sets the JSON path within the configuration structure for this entry. Provides structural context beyond the flattened `Key`.

## Usage

### Serialize and deserialize a diff report

```csharp
using AppSettingsDiff;
using AppSettingsDiff.Json;

var baseConfig = ConfigurationBuilder.Load("appsettings.json");
var targetConfig = ConfigurationBuilder.Load("appsettings.Production.json");

DiffResult diff = ConfigDiffer.Diff(baseConfig, targetConfig);

// Serialize to JSON
string json = diff.ToJson();
File.WriteAllText("diff-report.json", json);

// Later: deserialize back to DiffResult
string stored = File.ReadAllText("diff-report.json");
DiffResult? restored = DiffReportWriterJsonExtensions.FromJson(stored);
if (restored is not null)
{
    foreach (var entry in restored.Entries)
    {
        Console.WriteLine($"{entry.Kind}: {entry.Key} = {entry.NewValue}");
    }
}
```

### Safe deserialization with TryFromJson

```csharp
using AppSettingsDiff.Json;

string? cachedJson = cache.GetString("last-diff");
if (!string.IsNullOrEmpty(cachedJson))
{
    if (DiffReportWriterJsonExtensions.TryFromJson(cachedJson, out DiffResult? diff))
    {
        // Process the diff
        RenderDiffReport(diff);
    }
    else
    {
        // Cache corrupted or schema mismatch; evict and recompute
        cache.Remove("last-diff");
        DiffResult fresh = ConfigDiffer.Diff(base, target);
        cache.SetString("last-diff", fresh.ToJson());
        RenderDiffReport(fresh);
    }
}
```

## Notes

- **Thread safety**: The static methods are stateless and thread-safe. The DTO properties (`BasePath`, `TargetPath`, `Entries`, etc.) are simple data holders with no synchronization; treat instances as immutable after deserialization for safe sharing across threads.
- **Schema stability**: The JSON structure is versioned implicitly by the DTO property names. Adding new properties to `DiffEntryJson` or the root report is a non-breaking change for `TryFromJson` and `FromJson` (they ignore unknown properties). Removing or renaming properties breaks deserialization of older payloads.
- **Sensitive data**: `IsSensitive` flags entries detected by `SensitiveKeyDetector`. The JSON output still contains the raw `OldValue` and `NewValue`; it is the consumer's responsibility to redact before logging or persisting to insecure stores.
- **Null collections**: `Entries` may be null (not empty) when the diff contains no differences. Consumers should check for null before iterating.
- **Serializer options**: Passing custom `JsonSerializerOptions` is supported but must preserve the default contract (camel-case property names, public setters). Options that ignore null values will omit `null` properties, which `FromJson` handles correctly.
