# MergeResultExtensions

Provides a set of static extension methods for working with `MergeResult` instances, offering convenient access to merged configuration values with type conversion, conflict inspection, and key enumeration. These methods simplify common post-merge operations such as retrieving typed values, checking for conflicts, and iterating over the merged result set.

## API

### GetValueOrDefault

```csharp
public static string GetValueOrDefault(this MergeResult mergeResult, string key)
```

Retrieves the merged string value for the specified key. If the key is not present in the merged result, returns `null`.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key to look up.

**Returns:** The merged value as a string, or `null` if the key does not exist.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### GetValueOrDefaultAsInt

```csharp
public static int GetValueOrDefaultAsInt(this MergeResult mergeResult, string key, int defaultValue = 0)
```

Retrieves the merged value for the specified key and converts it to an integer. If the key is not present or the value cannot be parsed as an integer, returns the provided default value.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key to look up.
- `defaultValue` — The value to return when the key is missing or parsing fails. Defaults to `0`.

**Returns:** The parsed integer value, or `defaultValue` on failure.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### GetValueOrDefaultAsBool

```csharp
public static bool GetValueOrDefaultAsBool(this MergeResult mergeResult, string key, bool defaultValue = false)
```

Retrieves the merged value for the specified key and converts it to a boolean. If the key is not present or the value cannot be parsed as a boolean, returns the provided default value.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key to look up.
- `defaultValue` — The value to return when the key is missing or parsing fails. Defaults to `false`.

**Returns:** The parsed boolean value, or `defaultValue` on failure.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### GetValueOrDefaultAsDecimal

```csharp
public static decimal GetValueOrDefaultAsDecimal(this MergeResult mergeResult, string key, decimal defaultValue = 0m)
```

Retrieves the merged value for the specified key and converts it to a decimal. If the key is not present or the value cannot be parsed as a decimal, returns the provided default value.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key to look up.
- `defaultValue` — The value to return when the key is missing or parsing fails. Defaults to `0m`.

**Returns:** The parsed decimal value, or `defaultValue` on failure.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### ContainsKey

```csharp
public static bool ContainsKey(this MergeResult mergeResult, string key)
```

Determines whether the merged result contains the specified key.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key to check.

**Returns:** `true` if the key exists in the merged result; otherwise `false`.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### Count

```csharp
public static int Count(this MergeResult mergeResult)
```

Gets the total number of keys present in the merged result.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.

**Returns:** The count of merged keys.

**Throws:** `ArgumentNullException` if `mergeResult` is `null`.

---

### GetKeys

```csharp
public static IEnumerable<string> GetKeys(this MergeResult mergeResult)
```

Enumerates all keys present in the merged result.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.

**Returns:** An `IEnumerable<string>` containing every key in the merged result.

**Throws:** `ArgumentNullException` if `mergeResult` is `null`.

---

### TryGetValue

```csharp
public static bool TryGetValue(this MergeResult mergeResult, string key, out string value)
```

Attempts to retrieve the merged string value for the specified key. Returns a success indicator rather than throwing when the key is absent.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key to look up.
- `value` — When this method returns `true`, contains the merged value; when `false`, contains `null`.

**Returns:** `true` if the key was found; otherwise `false`.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### GetConflicts

```csharp
public static IReadOnlyList<MergeConflict> GetConflicts(this MergeResult mergeResult)
```

Retrieves all merge conflicts that occurred during the merge operation.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.

**Returns:** A read-only list of `MergeConflict` instances. Returns an empty list if no conflicts occurred.

**Throws:** `ArgumentNullException` if `mergeResult` is `null`.

---

### HasConflicts

```csharp
public static bool HasConflicts(this MergeResult mergeResult)
```

Indicates whether any merge conflicts were detected during the merge operation.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.

**Returns:** `true` if one or more conflicts exist; otherwise `false`.

**Throws:** `ArgumentNullException` if `mergeResult` is `null`.

---

### GetConflict

```csharp
public static MergeConflict? GetConflict(this MergeResult mergeResult, string key)
```

Retrieves the merge conflict for a specific key, if one exists.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.
- `key` — The configuration key whose conflict information to retrieve.

**Returns:** A `MergeConflict?` instance if the key had a conflict during merging; `null` if no conflict exists for that key or the key is not present.

**Throws:** `ArgumentNullException` if `mergeResult` or `key` is `null`.

---

### GetConflictedKeys

```csharp
public static IReadOnlyList<string> GetConflictedKeys(this MergeResult mergeResult)
```

Retrieves the keys for which merge conflicts were detected.

**Parameters:**
- `mergeResult` — The `MergeResult` instance to query.

**Returns:** A read-only list of key strings that have conflicts. Returns an empty list if no conflicts occurred.

**Throws:** `ArgumentNullException` if `mergeResult` is `null`.

## Usage

### Example 1: Retrieving typed values and checking for conflicts

```csharp
MergeResult result = differ.Merge(baseConfig, overlayConfig);

// Retrieve typed configuration values with fallback defaults
int port = result.GetValueOrDefaultAsInt("server.port", 8080);
bool enableSsl = result.GetValueOrDefaultAsBool("server.ssl", true);
decimal timeout = result.GetValueOrDefaultAsDecimal("server.timeoutSeconds", 30.0m);

// Check for and handle conflicts
if (result.HasConflicts())
{
    IReadOnlyList<string> conflictedKeys = result.GetConflictedKeys();
    foreach (string key in conflictedKeys)
    {
        MergeConflict? conflict = result.GetConflict(key);
        Console.WriteLine($"Conflict at '{key}': {conflict?.BaseValue} vs {conflict?.OverlayValue}");
    }
}

Console.WriteLine($"Merged configuration contains {result.Count()} keys on port {port}");
```

### Example 2: Safe enumeration and conditional value extraction

```csharp
MergeResult result = differ.Merge(appSettings, environmentOverrides);

// Enumerate all merged keys and safely extract values
foreach (string key in result.GetKeys())
{
    if (result.TryGetValue(key, out string? value))
    {
        Console.WriteLine($"{key} = {value ?? "(null)"}");
    }
}

// Check for specific key existence before retrieving
if (result.ContainsKey("featureFlags.newDashboard"))
{
    bool newDashboard = result.GetValueOrDefaultAsBool("featureFlags.newDashboard");
    if (newDashboard)
    {
        // Activate new dashboard feature
    }
}

// Inspect all conflicts in detail
IReadOnlyList<MergeConflict> conflicts = result.GetConflicts();
foreach (MergeConflict conflict in conflicts)
{
    // Log or resolve each conflict
}
```

## Notes

- All methods throw `ArgumentNullException` when the `mergeResult` parameter is `null`. Methods accepting a `key` parameter also throw if `key` is `null`.
- The typed conversion methods (`GetValueOrDefaultAsInt`, `GetValueOrDefaultAsBool`, `GetValueOrDefaultAsDecimal`) do not throw on parse failures; they silently return the specified default value. This makes them suitable for handling loosely structured or user-provided configuration data where value formats may vary.
- `GetConflict` returns `null` both when the key has no conflict and when the key is entirely absent from the merged result. Use `ContainsKey` or `TryGetValue` to distinguish between these cases if needed.
- `GetConflicts` and `GetConflictedKeys` return empty collections, not `null`, when no conflicts exist. Callers can safely iterate over the results without null checks.
- These methods are extension methods on `MergeResult` and are stateless; they delegate to the underlying data structures of the `MergeResult` instance. Thread safety depends entirely on the thread safety of the `MergeResult` instance being operated on. If the `MergeResult` is immutable (as is typical after a merge operation completes), all methods are safe to call concurrently from multiple threads.
