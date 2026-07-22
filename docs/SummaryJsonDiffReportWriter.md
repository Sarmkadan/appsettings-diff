# SummaryJsonDiffReportWriter

The `SummaryJsonDiffReportWriter` class generates a condensed JSON representation of a diff report produced by the `appsettings-diff` library. It provides methods to output the summary in various formats (console, Markdown, HTML) and to retrieve the summary as a JSON string or a JSON Patch document. This writer is designed for scenarios where only a high-level overview of configuration differences is needed, omitting detailed per‑property changes.

## API

### `public SummaryJsonDiffReportWriter(DiffReport report)`

Initializes a new instance of the `SummaryJsonDiffReportWriter` with the specified diff report.

- **Parameters**  
  `report` – The `DiffReport` object containing the differences to summarize. Must not be `null`.

- **Throws**  
  `ArgumentNullException` if `report` is `null`.

### `public void WriteConsole()`

Writes the summary diff report to the standard output stream in a human‑readable JSON format.

- **Parameters**  
  None.

- **Throws**  
  `InvalidOperationException` if the internal diff report is not available (e.g., if the writer was not properly initialized).

### `public string ToJson()`

Returns the summary diff report as a compact JSON string.

- **Returns**  
  A `string` containing the JSON representation of the summary.

- **Throws**  
  `InvalidOperationException` if the internal diff report is not available.

### `public string ToJson(JsonSerializerOptions options)`

Returns the summary diff report as a JSON string using the specified serialization options.

- **Parameters**  
  `options` – A `JsonSerializerOptions` instance that controls formatting (e.g., indentation, naming policy). Pass `null` to use default options.

- **Returns**  
  A `string` containing the JSON representation of the summary, formatted according to `options`.

- **Throws**  
  `InvalidOperationException` if the internal diff report is not available.

### `public void WriteMarkdown()`

Writes the summary diff report to the standard output stream in Markdown format.

- **Parameters**  
  None.

- **Throws**  
  `InvalidOperationException` if the internal diff report is not available.

### `public void WriteHtml()`

Writes the summary diff report to the standard output stream as an HTML fragment.

- **Parameters**  
  None.

- **Throws**  
  `InvalidOperationException` if the internal diff report is not available.

### `public string ToJsonPatch()`

Returns a JSON Patch (RFC 6902) document that describes the differences in a standard patch format.

- **Returns**  
  A `string` containing the JSON Patch array.

- **Throws**  
  `InvalidOperationException` if the internal diff report is not available.

## Usage

### Example 1: Basic JSON summary and console output

```csharp
using AppsettingsDiff;
using AppsettingsDiff.ReportWriters;

// Assume diffReport is obtained from ConfigDiffer.Diff(...)
DiffReport diffReport = ConfigDiffer.Diff(oldConfig, newConfig);

var writer = new SummaryJsonDiffReportWriter(diffReport);

// Get the summary as a compact JSON string
string jsonSummary = writer.ToJson();
Console.WriteLine(jsonSummary);

// Write the same summary directly to the console
writer.WriteConsole();
```

### Example 2: Formatted JSON and Markdown output

```csharp
using System.Text.Json;
using AppsettingsDiff;
using AppsettingsDiff.ReportWriters;

DiffReport diffReport = ConfigDiffer.Diff(oldConfig, newConfig);

var writer = new SummaryJsonDiffReportWriter(diffReport);

// Pretty-print the JSON summary
var options = new JsonSerializerOptions { WriteIndented = true };
string formattedJson = writer.ToJson(options);
File.WriteAllText("summary.json", formattedJson);

// Generate a Markdown report for documentation
writer.WriteMarkdown(); // outputs to console; redirect if needed
```

## Notes

- **Empty diff reports** – If the `DiffReport` contains no differences, the writer produces an empty summary (e.g., an empty JSON object `{}` or a Markdown table with no rows). No exceptions are thrown in this case.
- **Null or invalid state** – All instance methods throw `InvalidOperationException` if the writer was constructed with a `null` report or if the internal state has been corrupted. Always ensure the `DiffReport` is non‑null and fully populated before calling any output method.
- **Thread safety** – `SummaryJsonDiffReportWriter` is **not thread‑safe**. The instance holds a reference to the diff report and may cache intermediate results. Concurrent calls to any of its methods from multiple threads can lead to undefined behavior. If thread‑safe access is required, synchronize externally or create a new writer per thread.
- **JSON Patch generation** – The `ToJsonPatch()` method produces a valid JSON Patch document only if the original diff report contains operations that can be expressed as add, remove, replace, move, copy, or test. If the diff report includes unsupported operations, the method may throw `NotSupportedException`.
