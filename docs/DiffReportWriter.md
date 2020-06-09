# DiffReportWriter

A utility class for generating human-readable and machine-readable reports of configuration differences between two states. It serializes the results of a diff operation into console output, JSON, or Markdown formats, enabling integration with CI/CD pipelines, documentation generation, or manual review.

## API

### `DiffReportWriter`

The default constructor initializes a new instance of the `DiffReportWriter` class with no internal state. This instance is ready to serialize a diff result once provided via one of the output methods.

### `WriteConsole()`

Writes the current diff report to the console standard output stream using a human-readable format. This method does not modify the internal state of the `DiffReportWriter` and can be called multiple times with the same result.

- **Parameters:** None
- **Return value:** `void`
- **Throws:** Does not throw under normal operation.

### `ToJson()`

Serializes the current diff report into a JSON string. The output is compact and suitable for machine processing, logging, or transmission over APIs.

- **Parameters:** None
- **Return value:** `string` – A JSON representation of the diff report.
- **Throws:** Does not throw under normal operation.

### `ToMarkdown()`

Serializes the current diff report into a Markdown string. The output is formatted for readability in Markdown viewers, including Git platforms, documentation sites, or IDE previews.

- **Parameters:** None
- **Return value:** `string` – A Markdown-formatted representation of the diff report.
- **Throws:** Does not throw under normal operation.

## Usage
