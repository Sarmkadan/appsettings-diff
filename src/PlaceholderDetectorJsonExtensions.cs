namespace AppsettingsDiff;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="PlaceholderDetector"/>.
/// </summary>
public static class PlaceholderDetectorJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes a <see cref="PlaceholderDetector"/> instance to a JSON string
	/// containing its full pattern list.
	/// </summary>
	/// <param name="value">The <see cref="PlaceholderDetector"/> instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the <see cref="PlaceholderDetector"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this PlaceholderDetector value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(new PlaceholderDetectorState { Patterns = [.. value.Patterns] }, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="PlaceholderDetector"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>A <see cref="PlaceholderDetector"/> instance populated from the JSON data.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static PlaceholderDetector? FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		var state = JsonSerializer.Deserialize<PlaceholderDetectorState>(json, _jsonOptions);
		return state?.Patterns is null ? null : new PlaceholderDetector(state.Patterns);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="PlaceholderDetector"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized <see cref="PlaceholderDetector"/> instance if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise false.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
	public static bool TryFromJson(string json, out PlaceholderDetector? value)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		try
		{
			value = FromJson(json);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}

	/// <summary>
	/// Serialization contract for <see cref="PlaceholderDetector"/>.
	/// </summary>
	private sealed class PlaceholderDetectorState
	{
		public List<string>? Patterns { get; init; }
	}
}
