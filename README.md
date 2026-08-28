## ConsoleDiffReportWriter

The ConsoleDiffReportWriter class is responsible for writing console diff reports. It provides methods to write reports in various formats, including JSON, Markdown, and HTML.

Example usage:
```csharp
public ConsoleDiffReportWriter writer = new ConsoleDiffReportWriter();
writer.WriteConsole();
string json = writer.ToJson();
writer.WriteMarkdown();
writer.WriteHtml();
string jsonPatch = writer.ToJsonPatch();
```

## SummaryJsonDiffReportWriter

The SummaryJsonDiffReportWriter writes a compact JSON summary of the diff, showing only counts and lists of changed keys for added, removed, changed, and type-changed entries.
It does not support console, markdown, HTML, or JSON Patch output (those methods throw NotSupportedException).
Example usage:
```csharp
var detector = new SensitiveKeyDetector();
var writer = new SummaryJsonDiffReportWriter(detector);
DiffResult diffResult = ...; // obtain from a diff operation
string json = writer.ToJson(diffResult);
```
