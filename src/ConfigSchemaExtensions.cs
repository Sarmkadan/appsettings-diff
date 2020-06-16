using System;
using System.Collections.Generic;
using System.Globalization;

namespace AppsettingsDiff
{
    /// <summary>
    /// Provides extension methods for <see cref="ConfigSchema"/> to enhance schema validation and configuration handling.
    /// </summary>
    public static class ConfigSchemaExtensions
    {
        /// <summary>
        /// Determines whether the specified key is required according to the schema.
        /// </summary>
        /// <param name="schema">The schema to check against.</param>
        /// <param name="key">The key to check.</param>
        /// <returns><see langword="true"/> if the key is required; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public static bool IsRequiredKey(this ConfigSchema schema, string key)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentException.ThrowIfNullOrEmpty(key);

            return schema.RequiredKeys.Contains(key, StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets the type hint for the specified key if it exists.
        /// </summary>
        /// <param name="schema">The schema to get the type hint from.</param>
        /// <param name="key">The key to get the type hint for.</param>
        /// <param name="typeHint">When this method returns, contains the type hint if the key exists; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the key has a type hint; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public static bool TryGetTypeHint(this ConfigSchema schema, string key, out string? typeHint)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (schema.TypeHints.TryGetValue(key, out var hint))
            {
                typeHint = hint;
                return true;
            }

            typeHint = null;
            return false;
        }

        /// <summary>
        /// Validates that a configuration value matches its declared type hint.
        /// </summary>
        /// <param name="schema">The schema containing type hints.</param>
        /// <param name="key">The configuration key to validate.</param>
        /// <param name="value">The configuration value to validate.</param>
        /// <returns>A <see cref="SchemaViolation"/> if validation fails; otherwise, <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public static SchemaViolation? ValidateType(this ConfigSchema schema, string key, string value)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentException.ThrowIfNullOrEmpty(value);

            if (!schema.TypeHints.TryGetValue(key, out var typeHint))
            {
                return null; // No type hint for this key
            }

            var normalizedType = typeHint.Trim().ToLowerInvariant();

            return normalizedType switch
            {
                "int" => ValidateIntType(key, value),
                "bool" => ValidateBoolType(key, value),
                "url" => ValidateUrlType(key, value),
                "guid" => ValidateGuidType(key, value),
                "double" => ValidateDoubleType(key, value),
                "datetime" => ValidateDateTimeType(key, value),
                _ => new SchemaViolation
                {
                    Key = key,
                    Message = $"Unknown type hint '{typeHint}' for key '{key}'",
                    IsMissing = false
                }
            };
        }

        /// <summary>
        /// Gets all keys that have type hints defined in the schema.
        /// </summary>
        /// <param name="schema">The schema to get typed keys from.</param>
        /// <returns>An enumerable of keys with type hints.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="schema"/> is <see langword="null"/>.</exception>
        public static IEnumerable<string> GetTypedKeys(this ConfigSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            foreach (var key in schema.TypeHints.Keys)
            {
                yield return key;
            }
        }

        private static SchemaViolation? ValidateIntType(string key, string value)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                return null;
            }

            return new SchemaViolation
            {
                Key = key,
                Message = $"Value '{value}' is not a valid integer for key '{key}'",
                IsMissing = false
            };
        }

        private static SchemaViolation? ValidateBoolType(string key, string value)
        {
            if (bool.TryParse(value, out _))
            {
                return null;
            }

            return new SchemaViolation
            {
                Key = key,
                Message = $"Value '{value}' is not a valid boolean for key '{key}'",
                IsMissing = false
            };
        }

        private static SchemaViolation? ValidateUrlType(string key, string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new SchemaViolation
            {
                Key = key,
                Message = $"Value '{value}' is not a valid URL for key '{key}'",
                IsMissing = false
            };
        }

        private static SchemaViolation? ValidateGuidType(string key, string value)
        {
            if (Guid.TryParse(value, out _))
            {
                return null;
            }

            return new SchemaViolation
            {
                Key = key,
                Message = $"Value '{value}' is not a valid GUID for key '{key}'",
                IsMissing = false
            };
        }

        private static SchemaViolation? ValidateDoubleType(string key, string value)
        {
            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
            {
                return null;
            }

            return new SchemaViolation
            {
                Key = key,
                Message = $"Value '{value}' is not a valid double for key '{key}'",
                IsMissing = false
            };
        }

        private static SchemaViolation? ValidateDateTimeType(string key, string value)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
            {
                return null;
            }

            return new SchemaViolation
            {
                Key = key,
                Message = $"Value '{value}' is not a valid DateTime for key '{key}'",
                IsMissing = false
            };
        }
    }
}