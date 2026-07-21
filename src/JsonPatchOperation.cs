using System.Text.Json.Serialization;

namespace AppsettingsDiff;

/// <summary>
/// Represents a JSON Patch operation according to RFC 6902
/// </summary>
public sealed class JsonPatchOperation
{
    [JsonPropertyName("op")]
    public required string Op { get; set; }

    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }
}