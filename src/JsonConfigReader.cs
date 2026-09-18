using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AppsettingsDiff
{
    public class JsonConfigReader
    {
        /// <summary>
        /// Synchronously reads a JSON configuration file and flattens it into a dictionary.
        /// </summary>
        /// <param name="jsonConfigPath">Path to the JSON file.</param>
        /// <returns>A dictionary containing flattened key/value pairs.</returns>
        public Dictionary<string, string> ReadJsonConfig(string jsonConfigPath)
        {
            var json = File.ReadAllText(jsonConfigPath);
            using var doc = JsonDocument.Parse(json);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Flatten(doc.RootElement, string.Empty, values);
            return values;
        }

        /// <summary>
        /// Asynchronously reads a JSON configuration file and flattens it into a dictionary.
        /// </summary>
        /// <param name="jsonConfigPath">Path to the JSON file.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary with flattened key/value pairs.</returns>
        public async Task<Dictionary<string, string>> ReadAsync(string jsonConfigPath, CancellationToken cancellationToken = default)
        {
            // Open the file as a stream to avoid loading the entire file into memory at once.
            await using var stream = File.OpenRead(jsonConfigPath);

            // Parse the JSON document asynchronously, respecting the cancellation token.
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Flatten(doc.RootElement, string.Empty, values);
            return values;
        }

        private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> values)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        Flatten(property.Value, key, values);
                    }
                    break;
                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var key = $"{prefix}[{index}]";
                        Flatten(item, key, values);
                        index++;
                    }
                    break;
                case JsonValueKind.String:
                    values[prefix] = element.GetString() ?? string.Empty;
                    break;
                case JsonValueKind.Number:
                    values[prefix] = element.GetRawText();
                    break;
                case JsonValueKind.True:
                    values[prefix] = "true";
                    break;
                case JsonValueKind.False:
                    values[prefix] = "false";
                    break;
                case JsonValueKind.Null:
                    values[prefix] = string.Empty;
                    break;
                default:
                    values[prefix] = element.GetRawText();
                    break;
            }
        }
    }
}
