# MergeResult
The `MergeResult` type represents the outcome of merging two sets of application settings, providing information about the merged result, any conflicts that arose during the merge, and the values from the base, "our", and "their" settings. It is used to determine the final state of application settings after a merge operation.

## API
* `public Dictionary<string, string> Merged`: A dictionary containing the merged application settings, where each key is a setting name and the corresponding value is the merged value for that setting.
* `public IReadOnlyList<MergeConflict> Conflicts`: A list of conflicts that occurred during the merge operation, where each conflict represents a setting that could not be automatically merged.
* `public string Key`: The key of the setting that was merged.
* `public string? BaseValue`: The value of the setting from the base settings, or `null` if no base value was provided.
* `public string? OurValue`: The value of the setting from the "our" settings, or `null` if no "our" value was provided.
* `public string? TheirValue`: The value of the setting from the "their" settings, or `null` if no "their" value was provided.
* `public static MergeResult Merge`: A static method that performs the merge operation and returns a `MergeResult` instance representing the outcome of the merge.

## Usage
The following examples demonstrate how to use the `MergeResult` type:
```csharp
// Example 1: Merging two sets of application settings
var baseSettings = new Dictionary<string, string> { { "Setting1", "BaseValue1" } };
var ourSettings = new Dictionary<string, string> { { "Setting1", "OurValue1" } };
var theirSettings = new Dictionary<string, string> { { "Setting1", "TheirValue1" } };

var mergeResult = MergeResult.Merge("Setting1", baseSettings, ourSettings, theirSettings);
Console.WriteLine($"Merged value: {mergeResult.Merged["Setting1"]}");
Console.WriteLine($"Conflicts: {mergeResult.Conflicts.Count}");

// Example 2: Handling merge conflicts
var conflictingSettings = new Dictionary<string, string> { { "Setting2", "BaseValue2" } };
var ourConflictingSettings = new Dictionary<string, string> { { "Setting2", "OurValue2" } };
var theirConflictingSettings = new Dictionary<string, string> { { "Setting2", "TheirValue2" } };

var conflictingMergeResult = MergeResult.Merge("Setting2", conflictingSettings, ourConflictingSettings, theirConflictingSettings);
if (conflictingMergeResult.Conflicts.Count > 0)
{
    Console.WriteLine("Merge conflict detected:");
    foreach (var conflict in conflictingMergeResult.Conflicts)
    {
        Console.WriteLine($"  {conflict}");
    }
}
```

## Notes
When using the `MergeResult` type, note that the `Merged` dictionary will only contain settings that were successfully merged. If a conflict occurs, the corresponding setting will not be included in the `Merged` dictionary, but will instead be represented as a `MergeConflict` in the `Conflicts` list. Additionally, the `BaseValue`, `OurValue`, and `TheirValue` properties may be `null` if no corresponding value was provided. The `Merge` method is thread-safe, but the `MergeResult` instance itself is not designed to be accessed concurrently. Edge cases, such as merging settings with null or empty values, are handled according to the specific implementation of the `Merge` method.
