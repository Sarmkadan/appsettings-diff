# ConsoleDiffReportWriter

The `ConsoleDiffReportWriter` class provides a set of helpers for rendering a settings difference report in various textual formats. It is typically instantiated with a `DiffReport` produced by `ConfigDiffer` and then used to output the report to the console, as JSON, JSON‑Patch, Markdown, or HTML.

## API

### ConsoleDiffReportWriter()
```csharp
public ConsoleDiffReportWriter()
```
Initializes a new instance of `ConsoleDiffReportWriter`. The instance is ready to accept a `DiffReport` and render it using the member methods.

### WriteConsole(DiffReport report)
```csharp
public void WriteConsole(DiffReport report)
```
Writes a human‑readable diff report to the standard console output.

- **Parameters**  
  - `report`: The diff report to render. Must not be `null`.
- **Return value**  
  - None.
- **Exceptions**  
  - `ArgumentNullException` if `report` is `null`.  
  - `IOException` if writing to the console fails.

### ToJson()
```csharp
public string ToJson()
```
Serializes the internal diff report to a JSON string using default serializer settings.

- **Return value**  
  - A JSON‑encoded string representing the diff report.
- **Exceptions**  
  - `InvalidOperationException` if the writer has not been supplied with a report.  
  - `JsonSerializationException` if serialization fails.

### ToJson(JsonSerializerOptions options)
```csharp
public string ToJson(JsonSerializerOptions options)
```
Serializes the internal diff report to a JSON string, allowing customization of the serialization process.

- **Parameters**  
  - `options`: Settings that control how the JSON is produced. Must not be `null`.
- **Return value**  
  - A JSON‑encoded string representing the diff report.
- **Exceptions**  
  - `ArgumentNullException` if `options` is `null`.  
  - `JsonSerializationException` if serialization fails.

### WriteMarkdown(TextWriter writer)
```csharp
public void WriteMarkdown(TextWriter writer)
```
Writes the diff report in Markdown format to the supplied `TextWriter`.

- **Parameters**  
  - `writer`: Destination for the Markdown output. Must not be `null`.
- **Return value**  
  - None.
- **Exceptions**  
  - `ArgumentNullException` if `writer` is `null`.  
  - `IOException` if an I/O error occurs while writing.

### WriteHtml(TextWriter writer)
```csharp
public void WriteHtml(TextWriter writer)
```
Writes the diff report in HTML format to the supplied `TextWriter`.

- **Parameters**  
  - `writer`: Destination for the HTML output. Must not be `null`.
- **Return value**  
  - None.
- **Exceptions**  
  - `ArgumentNullException` if `writer` is `null`.  
  - `IOException` if an I/O error occurs while writing.

### ToJsonPatch()
```csharp
public string ToJsonPatch()
```
Produces a JSON Patch document that captures the differences between the two configuration sets.

- **Return value**  
  - A string containing a JSON Patch array.
- **Exceptions**  
  - `InvalidOperationException` if the diff cannot be expressed as a JSON Patch (e.g., complex structural changes).  
  - `JsonSerializationException` if serialization fails.

## Usage

### Example 1: Console output
```csharp
using Varigence.AppSettingsDiff;

// Assume `options` and `baseline` are two IConfiguration roots
var differ = new ConfigDiffer();
DiffReport report = differ.GetDiff(baseline, options);

var writer = new ConsoleDiffReportWriter();
writer.WriteConsole(report);   // prints a readable diff to stdout
```

### Example 2: Generating JSON and saving to a file
```csharp
using System.IO;
using System.Text.Json;
using Varigence.AppSettingsDiff;

var differ = new ConfigDiffer();
DiffReport report = differ.GetDiff(baseline, options);

var jsonWriter = new ConsoleDiffReportWriter();
string json = jsonWriter.ToJson(new JsonSerializerOptions { WriteIndented = true });

File.WriteAllText("diff-report.json", json);
```

## Notes

- The writer does **not** store the `DiffReport` internally; each method expects the report to be passed explicitly (except the `ToJson` overloads, which operate on the report supplied during the last call to a write method or via a constructor overload not shown here). Supplying a `null` report will result in an `ArgumentNullException`.
- Instance members are **not thread‑safe**. Concurrent calls to any of the methods from multiple threads should be synchronized externally.
- Large reports may consume considerable memory when serialized to JSON or JSON‑Patch; consider streaming alternatives if size is a concern.
- The `ToJsonPatch` method will throw if the diff includes operations that cannot be represented as a JSON Patch (e.g., moving a section to a different parent). In such cases, fall back to `ToJson` or one of the text‑based formats.
