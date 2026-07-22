# HtmlDiffReportWriter

`HtmlDiffReportWriter` is a utility class for generating HTML diff reports that visualize differences between configuration files or similar text-based documents. It provides multiple output formats (HTML, Markdown, JSON, console) and integrates with the `appsettings-diff` tooling ecosystem.

## API

### `HtmlDiffReportWriter`

Initializes a new instance of the `HtmlDiffReportWriter` class with default settings.

```csharp
public HtmlDiffReportWriter()
```

**Parameters:** None
**Return value:** A new `HtmlDiffReportWriter` instance.
**Exceptions:** None

---

### `WriteConsole`

Writes the diff report to the console in the default format (HTML).

```csharp
public void WriteConsole()
```

**Parameters:** None
**Return value:** None
**Exceptions:** Throws `InvalidOperationException` if no diff report has been generated.

---

### `ToJson`

Generates a JSON representation of the diff report.

```csharp
public string ToJson()
```

**Parameters:** None
**Return value:** A JSON string representing the diff report.
**Exceptions:** Throws `InvalidOperationException` if no diff report has been generated.

There is also an overload:

```csharp
public string ToJson(bool indented)
```

**Parameters:**
- `indented` – If `true`, the JSON output is formatted with indentation for readability.

**Return value:** A JSON string representing the diff report.
**Exceptions:** Throws `InvalidOperationException` if no diff report has been generated.

---

### `WriteMarkdown`

Writes the diff report to a file in Markdown format.

```csharp
public void WriteMarkdown(string filePath)
```

**Parameters:**
- `filePath` – The path to the output Markdown file.

**Return value:** None
**Exceptions:**
- Throws `ArgumentNullException` if `filePath` is `null`.
- Throws `InvalidOperationException` if no diff report has been generated.

---

### `WriteHtml`

Writes the diff report to a file in HTML format.

```csharp
public void WriteHtml(string filePath)
```

**Parameters:**
- `filePath` – The path to the output HTML file.

**Return value:** None
**Exceptions:**
- Throws `ArgumentNullException` if `filePath` is `null`.
- Throws `InvalidOperationException` if no diff report has been generated.

---

### `ToJsonPatch`

Generates a JSON Patch (RFC 6902) representation of the diff.

```csharp
public string ToJsonPatch()
```

**Parameters:** None
**Return value:** A JSON Patch string representing the changes.
**Exceptions:** Throws `InvalidOperationException` if no diff report has been generated.

## Usage

### Example 1: Basic HTML Report Generation

```csharp
var writer = new HtmlDiffReportWriter();
writer.WriteHtml("diff-report.html");
```

### Example 2: Multi-Format Output

```csharp
var writer = new HtmlDiffReportWriter();
var json = writer.ToJson(indented: true);
File.WriteAllText("diff-report.json", json);
writer.WriteMarkdown("diff-report.md");
```

## Notes

- The class is not thread-safe; concurrent calls to the same instance may lead to inconsistent output.
- Methods that require a generated diff report will throw `InvalidOperationException` if no report has been produced (e.g., if `ToJson()` is called before any diff is computed).
- File output methods (`WriteMarkdown`, `WriteHtml`) validate `filePath` for `null` but do not check for directory existence or write permissions.
