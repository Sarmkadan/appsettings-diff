using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppsettingsDiff
{
    public class ConfigSchema
    {
        public List<string> RequiredKeys { get; set; } = new List<string>();
        public Dictionary<string, string> TypeHints { get; set; } = new Dictionary<string, string>();

        public static ConfigSchema LoadFromJson(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ConfigSchema>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to load schema from JSON");
        }
    }

    public class SchemaValidator
    {
        public IReadOnlyList<SchemaViolation> Validate(Dictionary<string, string> config, ConfigSchema schema)
        {
            var violations = new List<SchemaViolation>();

            foreach (var key in schema.RequiredKeys)
            {
                if (!config.ContainsKey(key))
                {
                    violations.Add(new SchemaViolation
                    {
                        Key = key,
                        Message = $"Required key '{key}' is missing",
                        IsMissing = true
                    });
                    continue;
                }

                if (schema.TypeHints.TryGetValue(key, out var typeHint))
                {
                    var value = config[key];
                    var isValid = true;
                    string errorMessage = "";

                    switch (typeHint.ToLower())
                    {
                        case "int":
                            if (!int.TryParse(value, out _))
                            {
                                isValid = false;
                                errorMessage = "Value must be a valid integer";
                            }
                            break;
                        case "bool":
                            if (!bool.TryParse(value, out _))
                            {
                                isValid = false;
                                errorMessage = "Value must be a valid boolean";
                            }
                            break;
                        case "url":
                            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || 
                                !uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                isValid = false;
                                errorMessage = "Value must be a valid URL";
                            }
                            break;
                        case "guid":
                            if (!Guid.TryParse(value, out _))
                            {
                                isValid = false;
                                errorMessage = "Value must be a valid GUID";
                            }
                            break;
                        default:
                            isValid = false;
                            errorMessage = $"Unknown type hint '{typeHint}'";
                            break;
                    }

                    if (!isValid)
                    {
                        violations.Add(new SchemaViolation
                        {
                            Key = key,
                            Message = errorMessage,
                            IsMissing = false
                        });
                    }
                }
            }

            return violations;
        }
    }

    public class SchemaViolation
    {
        public string Key { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsMissing { get; set; }
    }
}
