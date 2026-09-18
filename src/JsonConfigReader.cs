using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AppsettingsDiff
{
    public class JsonConfigReader
    {
        public Dictionary<string, string> ReadJsonConfig(string jsonConfigPath)
        {
            var json = File.ReadAllText(jsonConfigPath);
            using var doc = JsonDocument.Parse(json);
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
