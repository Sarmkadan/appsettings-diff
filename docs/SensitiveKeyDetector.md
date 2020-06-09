# SensitiveKeyDetector

The `SensitiveKeyDetector` class is responsible for identifying and managing sensitive configuration keys within the `appsettings-diff` tool. It facilitates the detection of sensitive entries in configuration files, tracks their values across different versions, and provides structured access to differences for reporting purposes.

## API

### Properties

#### `IsSensitive`
Indicates whether the current key is marked as sensitive.  
**Return Value**: `bool` - `true` if the key is sensitive; otherwise, `false`.

#### `Values`
A dictionary containing key-value pairs of sensitive entries.  
**Return Value**: `Dictionary<string, string>` - The collection of sensitive key-value pairs.

#### `OldValue`
The original value of the key before changes.  
**Return Value**: `string?` - The previous value, or `null` if not applicable.

#### `NewValue`
The updated value of the key after changes.  
**Return Value**: `string?` - The new value, or `null` if not applicable.

#### `Path`
The hierarchical path of the key in the configuration structure.  
**Return Value**: `string?` - The path string, or `null` if not set.

#### `Entries`
A list of individual configuration differences detected.  
**Return Value**: `List<DiffEntry>` - The collection of diff entries.

#### `BasePath`
The base configuration file path used for comparison.  
**Return Value**: `string` - The source path string.

#### `TargetPath`
The target configuration file path for comparison.  
**Return Value**: `string` - The destination path string.

#### `Kind`
Specifies the type of difference detected (e.g., added, removed, modified).  
**Return Value**: `DiffKind` - The difference category.

#### `Key`
The configuration key being analyzed.  
**Return Value**: `string` - The key name.

### Methods

#### `GetValue(string key)`
Retrieves the value associated with the specified key from the `Values` dictionary.  
**Parameters**:  
- `key` (`string`) - The key to look up.  
**Return Value**: `string` - The corresponding value.  
**Exceptions**:  
- `KeyNotFoundException` - Thrown when the key does not exist in `Values`.

#### `ContainsKey(string key)`
Checks if the specified key exists in the `Values` dictionary.  
**Parameters**:  
- `key` (`string`) - The key to check.  
**Return Value**: `bool` - `true` if the key exists; otherwise, `false`.

#### `CountOf(DiffKind kind)`
Counts the number of entries matching the specified difference kind.  
**Parameters**:  
- `kind` (`DiffKind`) - The difference category to count.  
**Return Value**: `int` - The number of matching entries.

#### `Diff()`
Generates a `DiffResult` comparing configurations at `BasePath` and `TargetPath`.  
**Return Value**: `DiffResult` - The comparison result.  
**Exceptions**:  
- `FileNotFoundException` - Thrown if either configuration file does not exist.  
- `InvalidOperationException` - Thrown if paths are invalid or inaccessible.

## Usage

### Example 1: Checking Sensitivity and Retrieving Values
```csharp
var detector = new SensitiveKeyDetector
{
    Key = "ConnectionStrings:Default",
    Kind = DiffKind.Modified,
    OldValue = "Server=old-db;",
    NewValue = "Server=new-db;",
    Path = "ConnectionStrings"
};

if (detector.IsSensitive)
{
    Console.WriteLine($"Sensitive key '{detector.Key}' changed from '{detector.OldValue}' to '{detector.NewValue}'.");
}
```

### Example 2: Generating a Full Configuration Diff
```csharp
var differ = new ConfigDiffer();
var detector = new SensitiveKeyDetector
{
    BasePath = "appsettings.json",
    TargetPath = "appsettings.Development.json"
};

var result = detector.Diff();
foreach (var entry in detector.Entries)
{
    Console.WriteLine($"{entry.Key}: {entry.Kind} ({entry.OldValue} → {entry.NewValue})");
}
```

## Notes

- The `Values` property is not thread-safe. Concurrent modifications may lead to race conditions or inconsistent state.
- The `Diff()` method requires valid, accessible file paths. Ensure `BasePath` and `TargetPath` point to existing configuration files.
- `GetValue` throws `KeyNotFoundException` if the key is not present in `Values`. Use `ContainsKey` to verify existence beforehand.
- `CountOf` returns zero if no entries match the specified `DiffKind`.
- The `Kind` and `Key` properties are marked `required`, meaning they must be initialized during object construction.
