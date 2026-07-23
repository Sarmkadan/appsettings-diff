using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the file deserializes to a null schema.</exception>
        /// <exception cref="JsonException">Thrown when the file is not valid JSON.</exception>
        public static ConfigSchema LoadFromJson(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ConfigSchema>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Failed to load schema from JSON");
        }

        /// <summary>
        /// Infers a configuration schema from a JSON document.
        /// </summary>
        /// <param name="document">The JSON document to infer from.</param>
        /// <param name="detector">Optional sensitive key detector for marking sensitive keys.</param>
        /// <returns>A new configuration schema inferred from the document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is <see langword="null"/>.</exception>
        public static ConfigSchema InferFrom(JsonDocument document, SensitiveKeyDetector? detector = null)
        {
            ArgumentNullException.ThrowIfNull(document);

            detector ??= new SensitiveKeyDetector();

            var schema = new ConfigSchema();
            var visitedKeys = new HashSet<string>(StringComparer.Ordinal);

            InferFromNode(document.RootElement, "", schema, visitedKeys, detector);

            // Mark all visited keys as required by default
            schema.RequiredKeys.AddRange(visitedKeys);

            return schema;
        }

        /// <summary>
        /// Infers a configuration schema from a flattened key-value dictionary.
        /// </summary>
        /// <param name="config">The configuration key-value pairs.</param>
        /// <param name="detector">Optional sensitive key detector for marking sensitive keys.</param>
        /// <returns>A new configuration schema inferred from the configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <see langword="null"/>.</exception>
        public static ConfigSchema InferFrom(Dictionary<string, string> config, SensitiveKeyDetector? detector = null)
        {
            ArgumentNullException.ThrowIfNull(config);

            detector ??= new SensitiveKeyDetector();

            var schema = new ConfigSchema();
            var visitedKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (key, value) in config)
            {
                if (visitedKeys.Contains(key))
                    continue;

                visitedKeys.Add(key);

                // Infer type from the string value
                var typeHint = InferTypeFromValue(value);
                if (typeHint != null)
                {
                    schema.TypeHints[key] = typeHint;
                }

                // Mark as sensitive if detected
                if (detector.IsSensitive(key))
                {
                    schema.TypeHints[key] = "string"; // Ensure it's treated as string for sensitive values
                }
            }

            // Mark all keys as required by default
            schema.RequiredKeys.AddRange(visitedKeys);

            return schema;
        }

        /// <summary>
        /// Infers a type from a configuration value string.
        /// </summary>
        /// <param name="value">The configuration value.</param>
        /// <returns>The inferred type hint ("string", "number", "bool", "array", "object"), or null if indeterminate.</returns>
        private static string? InferTypeFromValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Try to parse as boolean
            if (bool.TryParse(value, out _))
                return "bool";

            // Try to parse as integer
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                return "number";

            // Try to parse as double
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                return "number";

            // Try to parse as URL
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return "url";

            // Try to parse as GUID
            if (Guid.TryParse(value, out _))
                return "guid";

            // Try to parse as DateTime
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                return "datetime";

            // Check if it looks like a connection string (contains = or ;)
            if (value.Contains('=') || value.Contains(';'))
                return "string";

            // Default to string
            return "string";
        }

        /// <summary>
        /// Recursively infers schema from a JSON node.
        /// </summary>
        /// <param name="element">The JSON element to process.</param>
        /// <param name="path">The current path in the JSON structure.</param>
        /// <param name="schema">The schema being built.</param>
        /// <param name="visitedKeys">Set of already visited keys to avoid duplicates.</param>
        /// <param name="detector">Sensitive key detector for marking sensitive keys.</param>
        private static void InferFromNode(JsonElement element, string path, ConfigSchema schema, HashSet<string> visitedKeys, SensitiveKeyDetector detector)
        {
            var currentPath = string.IsNullOrEmpty(path) ? "" : path + ":";

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(path) ? property.Name : $"{path}:{property.Name}";
                        InferFromNode(property.Value, key, schema, visitedKeys, detector);
                    }
                    break;

                case JsonValueKind.Array:
                    // For arrays, we just note the presence but don't recurse into individual elements
                    // as they typically have the same structure
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!visitedKeys.Contains(path))
                        {
                            visitedKeys.Add(path);
                            schema.TypeHints[path] = "array";
                        }
                    }
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    if (!string.IsNullOrEmpty(path) && !visitedKeys.Contains(path))
                    {
                        visitedKeys.Add(path);

                        // Infer type from the actual value
                        string? typeHint = null;

                        if (element.ValueKind == JsonValueKind.String)
                        {
                            typeHint = InferTypeFromValue(element.GetString()!);
                        }
                        else if (element.ValueKind == JsonValueKind.Number)
                        {
                            // Numbers can be integers or doubles
                            var number = element.GetRawText();
                            typeHint = "number";
                        }
                        else if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                        {
                            typeHint = "bool";
                        }

                        if (typeHint != null)
                        {
                            schema.TypeHints[path] = typeHint;
                        }

                        // Mark as sensitive if detected
                        if (detector.IsSensitive(path))
                        {
                            schema.TypeHints[path] = "string"; // Ensure it's treated as string for sensitive values
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Saves the configuration schema to a JSON file.
        /// </summary>
        /// <param name="path">The path to save the schema to.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or whitespace.</exception>
        /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
        public void SaveToJson(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(path, json);
        }
    }

    /// <summary>
    /// Validates a configuration against a schema.
    /// </summary>
    public class SchemaValidator
    {
        /// <summary>
        /// Gets or sets a value indicating whether to report unknown keys (keys present in config but not in schema).
        /// </summary>
        public bool ReportUnknownKeys { get; set; } = true;

        /// <summary>
        /// Validates a configuration against a schema and returns a list of schema violations.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <param name="schema">The schema to validate against.</param>
        /// <returns>A list of schema violations.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="config"/> or <paramref name="schema"/> is <see langword="null"/>.
        /// </exception>
        public IReadOnlyList<SchemaViolation> Validate(Dictionary<string, string> config, ConfigSchema schema)
        {
            ArgumentNullException.ThrowIfNull(config);
            ArgumentNullException.ThrowIfNull(schema);

            var violations = new List<SchemaViolation>();

            // Check for missing required keys
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

                    switch (typeHint.Trim().ToLowerInvariant())
                    {
                        case "string":
                            break;
                        case "int":
                            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
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
                        case "double":
                            if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                            {
                                isValid = false;
                                errorMessage = "Value must be a valid double";
                            }
                            break;
                        case "datetime":
                            if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                            {
                                isValid = false;
                                errorMessage = "Value must be a valid DateTime";
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

            // Lint pass: Check for connection strings with credentials in config values
            foreach (var key in config.Keys)
            {
                if (schema.TypeHints.ContainsKey(key) || schema.RequiredKeys.Contains(key))
                {
                    var value = config[key];
                    if (ContainsConnectionStringCredentials(value))
                    {
                        violations.Add(new SchemaViolation
                        {
                            Key = key,
                            Message = $"Configuration value contains a connection string with credentials (detected Password= or pwd= pattern)",
                            IsSensitive = true
                        });
                    }
                }
            }

            // Check for keys differing only by casing
            var normalizedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in config.Keys)
            {
                var normalized = key.ToUpperInvariant();
                if (normalizedKeys.Contains(normalized))
                {
                    violations.Add(new SchemaViolation
                    {
                        Key = key,
                        Message = $"Key '{key}' differs only by casing from another key in config. Keys are case-insensitive and should have unique casing.",
                        IsCasingConflict = true,
                        IsMissing = false,
                        IsUnknown = false
                    });
                }
                else
                {
                    normalizedKeys.Add(normalized);
                }
            }

            // Check for unknown keys (present in config but not in schema)
            if (ReportUnknownKeys)
            {
                var knownKeys = new HashSet<string>(schema.RequiredKeys, StringComparer.OrdinalIgnoreCase);
                knownKeys.UnionWith(schema.TypeHints.Keys);

                foreach (var key in config.Keys)
                {
                    if (!knownKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    {
                        violations.Add(new SchemaViolation
                        {
                            Key = key,
                            Message = $"Unknown key '{key}' is present in config but not defined in schema",
                            IsUnknown = true,
                            IsMissing = false
                        });
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// Determines whether the given configuration value contains a connection string with credentials.
        /// Checks for patterns like 'Password=' or 'pwd=' in the value.
        /// </summary>
        /// <param name="value">The configuration value to check.</param>
        /// <returns><see langword="true"/> if the value contains a connection string with credentials; otherwise <see langword="false"/>.</returns>
        private static bool ContainsConnectionStringCredentials(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var lowerValue = value.ToLowerInvariant();
            return lowerValue.Contains("password=") || lowerValue.Contains("pwd=");
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

        /// <summary>
        /// Gets a value indicating whether the key is unknown (present in config but not in schema).
        /// </summary>
        public bool IsUnknown { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is a casing conflict (keys differing only by casing).
        /// </summary>
        public bool IsCasingConflict { get; set; }

        /// <summary>
        /// Gets a value indicating whether the violation is related to sensitive data (e.g., connection strings with passwords).
        /// </summary>
        public bool IsSensitive { get; set; }
    }
}