# JsonPatchDiffReportWriter

A utility class for generating and writing JSON Patch diff reports in various formats (console, JSON, Markdown, and HTML). It is designed to serialize differences between configuration snapshots into consumable formats suitable for logging, documentation, or API responses.

## API

### `public JsonPatchDiffReportWriter`

Initializes a new instance of the `JsonPatchDiffReportWriter` class with default settings. The instance is ready to accept diff data via subsequent method calls.

### `public void WriteConsole()`

Writes the current diff report to the console in a human-readable format. The output is formatted for terminal display with colorization where supported.

- **Parameters**: None
- **Return value**: None
- **Throws**: `InvalidOperationException` if no diff data has been set.

### `public string ToJson()`

Serializes the current diff report to a compact JSON string. The JSON structure follows the JSON Patch format (`application/json-patch+json`) and includes only the operations necessary to transform the source configuration into the target.

- **Parameters**: None
- **Return value**: A `string` containing the JSON Patch document.
- **Throws**: `InvalidOperationException` if no diff data has been set.

### `public string ToJson(bool indented)`

Serializes the current diff report to a JSON string with optional pretty-printing.

- **Parameters**:
  - `indented` (`bool`): If `true`, the JSON is formatted with indentation and line breaks for readability; otherwise, it is compact.
- **Return value**: A `string` containing the JSON Patch document.
- **Throws**: `InvalidOperationException` if no diff data has been set.

### `public void WriteMarkdown()`

Writes the current diff report to the console in Markdown format. The output is suitable for embedding in Markdown documents or wikis, with sections for added, removed, and changed values.

- **Parameters**: None
- **Return value**: None
- **Throws**: `InvalidOperationException` if no diff data has been set.

### `public void WriteHtml()`

Writes the current diff report to the console in HTML format. The output is a self-contained HTML fragment with inline styles, suitable for rendering in browsers or email clients.

- **Parameters**: None
- **Return value**: None
- **Throws**: `InvalidOperationException` if no diff data has been set.

### `public string ToJsonPatch()`

Returns the current diff report as a JSON Patch string. This is an alias for `ToJson()` and produces the same output.

- **Parameters**: None
- **Return value**: A `string` containing the JSON Patch document.
- **Throws**: `InvalidOperationException` if no diff data has been set.

## Usage

### Example 1: Writing a JSON Patch to a file
