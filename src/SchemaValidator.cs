using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppsettingsDiff
{
    /// <summary>
    /// Represents a configuration schema.
    /// </summary>
    public class ConfigSchema
    {
        /// <summary>
        /// Gets the list of required keys in the schema.
        /// </summary>
        public List<string> RequiredKeys { get; set; } = new List<string>();

        /// <summary>
        /// Gets the dictionary of type hints for keys in the schema.
        /// </summary>
        public Dictionary<string, string> TypeHints { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Loads a configuration schema from a JSON file.
        /// </summary>
        /// <param name="path">The path to the JSON file.</param>
        /// <returns>The loaded configuration schema.</returns>
        public static ConfigSchema LoadFromJson(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ConfigSchema>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to load schema from JSON");
        }
    }

    /// <summary>
    /// Validates a configuration against a schema.
    /// </summary>
    public class SchemaValidator
    {
        /// <summary>
        /// Validates a configuration against a schema and returns a list of schema violations.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="schema">The schema to validate against.</param>
        /// <returns>A list of schema violations.</returns>
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

    /// <summary>
    /// Represents a schema violation.
    /// </summary>
    public class SchemaViolation
    {
        /// <summary>
        /// Gets the key that is in violation.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets the message describing the violation.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the key is missing.
        /// </summary>
        public bool IsMissing { get; set; }
    }
}
