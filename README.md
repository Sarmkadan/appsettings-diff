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
