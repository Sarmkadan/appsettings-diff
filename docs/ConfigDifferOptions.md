# ConfigDifferOptions

Configuration options that control the behavior of the configuration differ when comparing two configuration sources. These options determine how arrays are compared, the maximum recursion depth for nested structures, and an optional path prefix applied to all reported differences.

## API

### `public bool UnorderedArrays`

Gets or sets a value indicating whether array comparisons should ignore element order. When `true`, arrays are treated as unordered collections; two arrays are considered equal if they contain the same elements regardless of sequence. When `false` (default), arrays are compared element-by-element in order.

**Default:** `false`

**Throws:** None.

---

### `public int? MaxDepth`

Gets or sets the maximum recursion depth when traversing nested configuration structures. A value of `null` (default) indicates no limit. When set, traversal stops at the specified depth and deeper values are treated as opaque leaf nodes for comparison purposes.

**Default:** `null`

**Throws:** `ArgumentOutOfRangeException` if set to a value less than 1.

---

### `public string? PathPrefix`

Gets or sets an optional prefix prepended to every difference path in the resulting diff report. Useful when comparing configuration subsections or when embedding diff results in a larger context. When `null` or empty, no prefix is applied.

**Default:** `null`

**Throws:** None.

## Usage

### Basic comparison with default options

```csharp
using AppSettingsDiff;

var options = new ConfigDifferOptions();
var differ = new ConfigDiffer(options);

var left = ConfigurationBuilder.Load("appsettings.json");
var right = ConfigurationBuilder.Load("appsettings.Development.json");

var report = differ.Diff(left, right);
DiffReportWriter.WriteConsole(report);
```

### Customizing array semantics and depth

```csharp
using AppSettingsDiff;

var options = new ConfigDifferOptions
{
    UnorderedArrays = true,
    MaxDepth = 5,
    PathPrefix = "features."
};

var differ = new ConfigDiffer(options);

var left = ConfigurationBuilder.Load("config/base.json");
var right = ConfigurationBuilder.Load("config/override.json");

var report = differ.Diff(left, right);

foreach (var entry in report.Entries)
{
    Console.WriteLine($"{entry.Path}: {entry.OldValue} -> {entry.NewValue}");
}
```

## Notes

- **Thread safety:** `ConfigDifferOptions` is a plain data object with no internal synchronization. It is safe to share a single instance across threads only if it is fully initialized before any concurrent access and never mutated afterward. Mutating properties after passing the instance to a `ConfigDiffer` while a diff operation is in progress results in undefined behavior.
- **Depth limiting:** Setting `MaxDepth` to a low value (e.g., 1 or 2) can significantly improve performance for deeply nested configurations but may cause distinct deep values to be reported as equal because their substructure is not examined.
- **Path prefix formatting:** `PathPrefix` is concatenated directly with the computed difference path without any separator normalization. Ensure the prefix ends with a dot (`.`) or other desired delimiter if the diff paths do not already include one.
- **Array comparison:** `UnorderedArrays = true` uses a multiset comparison algorithm with O(n log n) complexity due to sorting. For very large arrays, consider the performance impact versus the semantic requirement.
