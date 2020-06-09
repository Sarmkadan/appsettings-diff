# PlaceholderDetector
The `PlaceholderDetector` class is designed to identify placeholders within a given context, providing a way to detect and report on placeholder occurrences. This class is part of the `appsettings-diff` project, which aims to compare and analyze application settings.

## API
The `PlaceholderDetector` class offers the following public members:
- `public PlaceholderDetector`: The constructor for the `PlaceholderDetector` class, used to create a new instance.
- `public IReadOnlyList<PlaceholderFinding> Scan`: Scans for placeholders and returns a list of `PlaceholderFinding` records, each representing a detected placeholder.
- `public static bool LooksLikePlaceholder`: A static method that determines whether a given input looks like a placeholder. This method can be used independently of an instance of `PlaceholderDetector`.
- `public sealed record PlaceholderFinding`: A sealed record type representing a finding related to a placeholder, which is returned by the `Scan` method.

## Usage
Here are two examples of using the `PlaceholderDetector` class:
```csharp
// Example 1: Basic usage
var detector = new PlaceholderDetector();
var findings = detector.Scan();
foreach (var finding in findings)
{
    Console.WriteLine($"Found placeholder: {finding}");
}

// Example 2: Using the static LooksLikePlaceholder method
var input = "Some input that might be a placeholder";
if (PlaceholderDetector.LooksLikePlaceholder(input))
{
    Console.WriteLine("The input looks like a placeholder.");
}
```

## Notes
When using the `PlaceholderDetector` class, consider the following:
- The `Scan` method returns a list of `PlaceholderFinding` records, which can be empty if no placeholders are detected.
- The `LooksLikePlaceholder` method is static and can be used without creating an instance of `PlaceholderDetector`.
- The `PlaceholderDetector` class does not appear to have any specific thread-safety considerations, as it does not maintain any state that would be affected by concurrent access. However, the `Scan` method may throw exceptions if the input data is invalid or cannot be processed.
- Edge cases, such as null or empty input, should be handled according to the specific requirements of the application using the `PlaceholderDetector` class.
