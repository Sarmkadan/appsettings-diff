## HtmlDiffReportWriter

The `HtmlDiffReportWriter` class writes diff results as self-contained HTML reports with inline CSS styling.

### Public Members
- `WriteConsole(DiffResult result, bool noColor = false)` - Writes a colour-coded table to the console.
- `WriteMarkdown(DiffResult result, TextWriter writer)` - Writes a GitHub-flavored markdown report to the supplied writer.
- `WriteHtml(DiffResult result, TextWriter writer)` - Writes a self-contained HTML report to the supplied writer.
- `ToJsonPatch(DiffResult result, TextWriter writer)` - Streams a JSON Patch (RFC 6902) representation of the diff directly to the supplied writer.

### Example usage
```csharp
using AppsettingsDiff;

var result = new DiffResult { BasePath = "path1", TargetPath = "path2" };
var writer = new StringWriter();
new HtmlDiffReportWriter().WriteHtml(result, writer);
Console.WriteLine(writer.ToString());
```

## MarkdownDiffReportWriter

The `MarkdownDiffReportWriter` class generates GitHub-flavored markdown reports from configuration diff results, providing a structured table for added, removed, changed, and type-changed entries. It supports secret redaction based on a `SensitiveKeyDetector` policy, ensuring sensitive values remain protected in the output.

### Public Members
- `MarkdownDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)` - Initializes a new instance of the `MarkdownDiffReportWriter` class.
- `WriteConsole(DiffResult result, bool noColor = false)` - Writes a colour-coded table to the console.
- `WriteMarkdown(DiffResult result, TextWriter writer)` - Writes a GitHub-flavored markdown report to the supplied writer.
- `WriteHtml(DiffResult result, TextWriter writer)` - Writes a self-contained HTML report to the supplied writer.
- `WriteJsonPatch(DiffResult result, TextWriter writer)` - Streams a JSON Patch (RFC 6902) representation of the diff directly to the supplied writer.
- `ToJson(DiffResult result)` / `ToJson(DiffResult result, bool indented)` - Serializes the diff result to a JSON string.
- `ToJsonPatch(DiffResult result)` - Serializes the diff result to a JSON Patch string.

### Example usage
```csharp
using AppsettingsDiff;

var result = new DiffResult { BasePath = "path1", TargetPath = "path2" };
var detector = new SensitiveKeyDetector();
var writer = new StringWriter();
new MarkdownDiffReportWriter(detector).WriteMarkdown(result, writer);
Console.WriteLine(writer.ToString());
```

## JsonPatchDiffReportWriter

The `JsonPatchDiffReportWriter` class generates reports from configuration diff results, with a primary focus on producing JSON Patch (RFC 6902) representations. It offers a variety of output formats including console, markdown, HTML, and JSON, allowing for flexible integration into different workflows.

### Public Members
- `JsonPatchDiffReportWriter()` - Initializes a new instance of the `JsonPatchDiffReportWriter` class.
- `WriteConsole(DiffResult result, bool noColor = false)` - Writes a colour-coded table to the console.
- `ToJson(DiffResult result)` / `ToJson(DiffResult result, bool indented)` - Serializes the diff result to a JSON string.
- `WriteMarkdown(DiffResult result, TextWriter writer)` - Writes a GitHub-flavored markdown report to the supplied writer.
- `WriteHtml(DiffResult result, TextWriter writer)` - Writes a self-contained HTML report to the supplied writer.
- `ToJsonPatch(DiffResult result)` - Serializes the diff result to a JSON Patch string.

### Example usage
```csharp
using AppsettingsDiff;

var result = new DiffResult { BasePath = "path1", TargetPath = "path2" };
var writer = new JsonPatchDiffReportWriter();
string jsonPatch = writer.ToJsonPatch(result);
Console.WriteLine(jsonPatch);
```
