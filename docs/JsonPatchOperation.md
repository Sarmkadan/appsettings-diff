# JsonPatchOperation

Represents a single operation within a JSON Patch document, encapsulating the necessary details to describe a modification to a JSON structure. Used to construct and serialize individual patch operations for application to configuration or data models.

## API

### `Op`
```csharp
public required string Op { get; set; }
```
Specifies the type of operation to perform (e.g., "add", "remove", "replace"). Must be a valid JSON Patch operation type as defined by RFC 6902. Throws `ArgumentNullException` if not initialized.

### `Path`
```csharp
public required string Path { get; set; }
```
Defines the JSON Pointer location where the operation applies. Must be a valid JSON Pointer string. Throws `ArgumentNullException` if not initialized.

### `Value`
```csharp
public string? Value { get; set; }
```
Contains the value to apply for operations that require it (e.g., "add", "replace"). May be `null` for operations like "remove" or "move".

### `From`
```csharp
public string? From { get; set; }
```
Specifies the source path for "move" or "copy" operations. Ignored for other operation types.

### `DiffReportWriter`
```csharp
public DiffReportWriter DiffReportWriter { get; set; }
```
Provides access to a writer instance for generating diff reports. Used internally by output methods to format operation details.

### `WriteConsole()`
```csharp
public void WriteConsole()
```
Outputs the operation details to the console in a human-readable format. Uses `DiffReportWriter` for formatting. Does not throw exceptions under normal operation.

### `ToJson()`
```csharp
public string ToJson()
```
Serializes the operation to a JSON string representation. Returns a valid JSON object conforming to the JSON Patch operation schema. Throws `InvalidOperationException` if required properties (`Op`, `Path`) are not set.

### `ToJson(bool indented = false)`
```csharp
public string ToJson(bool indented = false)
```
Serializes the operation to a JSON string with optional indentation. The `indented` parameter controls formatting. Throws `InvalidOperationException` if required properties are not set.

### `WriteMarkdown()`
```csharp
public void WriteMarkdown()
```
Writes the operation details in Markdown format to a file or stream via `DiffReportWriter`. Useful for documentation generation. Does not throw exceptions under normal operation.

### `WriteHtml()`
```csharp
public void WriteHtml()
```
Outputs the operation details in HTML format using `DiffReportWriter`. Intended for web-based reporting. Does not throw exceptions under normal operation.

### `ToJsonPatch()`
```csharp
public string ToJsonPatch()
```
Generates a complete JSON Patch document string containing this operation. Returns a JSON array with the operation as its sole element. Throws `InvalidOperationException` if required properties are not set.

## Usage

### Example 1: Creating and Serializing a Replace Operation
```csharp
var operation = new JsonPatchOperation
{
    Op = "replace",
    Path = "/settings/logging/level",
    Value = "Warning"
};

string json = operation.ToJson();
Console.WriteLine(json); 
// Output: {"op":"replace","path":"/settings/logging/level","value":"Warning"}
```

### Example 2: Generating a Markdown Report for Multiple Operations
```csharp
var operations = new List<JsonPatchOperation>
{
    new JsonPatchOperation { Op = "add", Path = "/features/newFeature", Value = true },
    new JsonPatchOperation { Op = "remove", Path = "/deprecated/setting" }
};

foreach (var op in operations)
{
    op.WriteMarkdown();
}
```

## Notes

- All required properties (`Op`, `Path`) must be initialized before calling serialization or output methods; failure to do so will result in runtime exceptions.
- The `Value` and `From` properties are optional and should only be set when relevant to the operation type.
- Thread-safety is not guaranteed for instances of this type. Concurrent access to the same instance may lead to inconsistent state or output.
- The dual `ToJson()` overloads allow for compact or pretty-printed output, with the default being compact.
- `DiffReportWriter` is a dependency that must be properly configured for output methods to function correctly.
