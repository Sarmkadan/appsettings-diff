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
