# MarkdownDiffReportWriter

Provides a way to render a settings difference report as Markdown, HTML, JSON, or JSON Patch, and to write the output to the console or other destinations.

## API

### MarkdownDiffReportWriter()
Creates a new instance of the writer. The instance is initialized with the data required to generate the report (typically supplied via the constructor of the containing API).  
**Parameters:** none  
**Return value:** new `MarkdownDiffReportWriter` instance  
**Exceptions:** none

### WriteConsole()
Writes the Markdown‑formatted difference report to the standard output console.  
**Parameters:** none  
**Return value:** void  
**Exceptions:**  
- `IOException` – if the console output stream is unavailable or an I/O error occurs.

### ToJson()
Returns a JSON representation of the difference report.  
**Parameters:** none  
**Return value:** a JSON‑encoded string containing the report data  
**Exceptions:**  
- `JsonSerializationException` – if the report cannot be serialized to JSON.

### ToJson()
Second overload of `ToJson` that provides additional formatting control (e.g., indentation).  
**Parameters:** (implementation‑specific formatting options)  
**Return value:** a JSON‑encoded string containing the report data, formatted according to the supplied options  
**Exceptions:**  
- `JsonSerializationException` – if the report cannot be serialized to JSON.

### WriteMarkdown()
Writes the Markdown‑formatted difference report to the configured output target (e.g., a file or stream).  
**Parameters:** none  
**Return value:** void  
**Exceptions:**  
- `IOException` – if the output target cannot be written to.

### WriteHtml()
Writes an HTML representation of the difference report to the configured output target.  
**Parameters:** none  
**Return value:** void  
**Exceptions:**  
- `IOException` – if the output target cannot be written to.

### ToJsonPatch()
Produces a JSON Patch document that describes the changes needed to transform the original configuration into the modified one.  
**Parameters:** none  
**Return value:** a JSON Patch string  
**Exceptions:**  
- `InvalidOperationException` – if the report does not contain sufficient information to generate a patch.  
- `JsonSerializationException` – if the patch cannot be serialized.

## Usage

### Example 1: Writing a Markdown report to console and file
```csharp
using Varigence.AppSettings.Diff;

// Assume configBefore and configAfter are loaded IConfigurationRoot instances
var differ = new ConfigDiffer();
var report = differ.Diff(configBefore, configAfter);

var writer = new MarkdownDiffReportWriter(report);

// Output to console
writer.WriteConsole();

// Output to a Markdown file
using var stream = new FileStream("diff-report.md", FileMode.Create, FileAccess.Write);
using var writerStream = new StreamWriter(stream);
// The writer uses its internal target; here we assume it can be directed via a setter or overload.
// For illustration, we show a hypothetical method:
writer.WriteMarkdown(); // writes to the pre‑configured target (e.g., the stream set earlier)
```

### Example 2: Obtaining JSON and JSON Patch representations
```csharp
using Varigence.AppSettings.Diff;

var differ = new ConfigDiffer();
var report = differ.Diff(configBefore, configAfter);
var writer = new MarkdownDiffReportWriter(report);

// Get a compact JSON string
string json = writer.ToJson();

// Get an indented JSON string (using the overload)
string indentedJson = writer.ToJson(new JsonSerializerSettings { Formatting = Formatting.Indented });

// Get a JSON Patch that can be applied to the original config
string patch = writer.ToJsonPatch();
```

## Notes
- The writer instance is **not thread‑safe**. Concurrent calls to any of its methods from multiple threads may result in interleaved or corrupted output. If thread‑safe usage is required, external synchronization must be applied.
- `WriteConsole` relies on the availability of the standard console stream. In environments where the console is redirected or unavailable (e.g., Windows services, ASP.NET Core), the method will throw an `IOException`.
- `WriteMarkdown` and `WriteHtml` write to whatever output target has been configured on the writer instance (commonly a `TextWriter` supplied at construction or via a property). If no target has been set, these methods will throw an `IOException`.
- The `ToJson` overloads both return valid JSON; the parameterless overload uses default serializer settings, while the overload accepts formatting options to control indentation, null handling, etc.
- `ToJsonPatch` generates a RFC 6902 compliant JSON Patch. If the underlying diff report lacks the necessary detail (e.g., only value changes without path information), the method throws an `InvalidOperationException`.
- All methods that perform I/O (`WriteConsole`, `WriteMarkdown`, `WriteHtml`) will propagate any underlying `IOException` from the stream or console they write to. Callers should handle these exceptions appropriately when writing to files or network streams.
