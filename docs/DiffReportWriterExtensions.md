# DiffReportWriterExtensions
The `DiffReportWriterExtensions` type provides a set of extension methods for working with diff reports, allowing developers to easily generate summaries, JSON representations, and determine the presence and count of differences between two sets of data. These methods are designed to be used in conjunction with the `DiffReportWriter` class, simplifying the process of analyzing and reporting on differences.

## API
* `public static void WriteConsoleSummary`: Writes a summary of the diff report to the console. This method does not take any parameters and does not return a value. It is intended for use in command-line applications or other scenarios where console output is desired.
* `public static string ToCompactJson`: Returns a compact JSON representation of the diff report. This method does not take any parameters and returns a string containing the JSON data. It may throw exceptions if the diff report cannot be serialized to JSON.
* `public static string ToMarkdownSummary`: Returns a Markdown summary of the diff report. This method does not take any parameters and returns a string containing the Markdown text. It may throw exceptions if the diff report cannot be converted to Markdown.
* `public static bool HasDifferences`: Returns a boolean indicating whether the diff report contains any differences. This method does not take any parameters and returns a value of `true` if differences are present, `false` otherwise.
* `public static int GetTotalDifferenceCount`: Returns the total number of differences in the diff report. This method does not take any parameters and returns an integer value representing the count of differences.

## Usage
The following examples demonstrate how to use the `DiffReportWriterExtensions` methods:
```csharp
// Example 1: Writing a console summary
var diffReport = new DiffReport(); // assume DiffReport is populated
DiffReportWriterExtensions.WriteConsoleSummary(diffReport);

// Example 2: Generating a Markdown summary
var diffReport = new DiffReport(); // assume DiffReport is populated
var markdownSummary = DiffReportWriterExtensions.ToMarkdownSummary(diffReport);
Console.WriteLine(markdownSummary);
```

## Notes
When using the `DiffReportWriterExtensions` methods, note that the `ToCompactJson` and `ToMarkdownSummary` methods may throw exceptions if the diff report contains data that cannot be serialized or converted to the respective formats. Additionally, the `HasDifferences` and `GetTotalDifferenceCount` methods are thread-safe, as they only access the diff report data and do not modify it. However, the `WriteConsoleSummary` method is not thread-safe, as it writes to the console, which is a shared resource. Care should be taken when using this method in multi-threaded environments to avoid concurrent access issues.
