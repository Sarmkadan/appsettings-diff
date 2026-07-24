using System;
using System.Collections.Generic;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for SchemaValidator to improve branch and edge-case coverage.
    /// </summary>
    public class SchemaValidatorTests
    {
        private readonly SchemaValidator _validator = new();

        [Fact]
        public void Validate_MissingRequiredKey_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "key1", "missingKey", "key2" },
                TypeHints = new Dictionary<string, string>()
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var missingViolation = Assert.Single(violations, v => v.IsMissing);
            Assert.Equal("missingKey", missingViolation.Key);
            Assert.Equal("Required key 'missingKey' is missing", missingViolation.Message);
            Assert.True(missingViolation.IsMissing);
        }

        [Fact]
        public void Validate_TypeMismatch_IntegerExpected_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["port"] = "not-a-number"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "port" },
                TypeHints = new Dictionary<string, string> { ["port"] = "int" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var typeViolation = Assert.Single(violations);
            Assert.Equal("port", typeViolation.Key);
            Assert.Equal("Value must be a valid integer", typeViolation.Message);
            Assert.False(typeViolation.IsMissing);
        }

        [Fact]
        public void Validate_TypeMismatch_BooleanExpected_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["enabled"] = "not-a-boolean"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "enabled" },
                TypeHints = new Dictionary<string, string> { ["enabled"] = "bool" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var typeViolation = Assert.Single(violations);
            Assert.Equal("enabled", typeViolation.Key);
            Assert.Equal("Value must be a valid boolean", typeViolation.Message);
            Assert.False(typeViolation.IsMissing);
        }

        [Fact]
        public void Validate_TypeMismatch_DoubleExpected_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["timeout"] = "not-a-number"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "timeout" },
                TypeHints = new Dictionary<string, string> { ["timeout"] = "double" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var typeViolation = Assert.Single(violations);
            Assert.Equal("timeout", typeViolation.Key);
            Assert.Equal("Value must be a valid double", typeViolation.Message);
            Assert.False(typeViolation.IsMissing);
        }

        [Fact]
        public void Validate_TypeMismatch_DateTimeExpected_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["timestamp"] = "not-a-date"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "timestamp" },
                TypeHints = new Dictionary<string, string> { ["timestamp"] = "datetime" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var typeViolation = Assert.Single(violations);
            Assert.Equal("timestamp", typeViolation.Key);
            Assert.Equal("Value must be a valid DateTime", typeViolation.Message);
            Assert.False(typeViolation.IsMissing);
        }

        [Fact]
        public void Validate_TypeMismatch_UrlExpected_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["endpoint"] = "not-a-url"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "endpoint" },
                TypeHints = new Dictionary<string, string> { ["endpoint"] = "url" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var typeViolation = Assert.Single(violations);
            Assert.Equal("endpoint", typeViolation.Key);
            Assert.Equal("Value must be a valid URL", typeViolation.Message);
            Assert.False(typeViolation.IsMissing);
        }

        [Fact]
        public void Validate_TypeMismatch_GuidExpected_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["id"] = "not-a-guid"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "id" },
                TypeHints = new Dictionary<string, string> { ["id"] = "guid" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var typeViolation = Assert.Single(violations);
            Assert.Equal("id", typeViolation.Key);
            Assert.Equal("Value must be a valid GUID", typeViolation.Message);
            Assert.False(typeViolation.IsMissing);
        }

        [Fact]
        public void Validate_UnknownKeys_WhenReportUnknownKeysIsTrue_ProducesSchemaViolation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["knownKey"] = "value1",
                ["unknownKey"] = "value2"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "knownKey" },
                TypeHints = new Dictionary<string, string> { ["knownKey"] = "string" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var unknownViolation = Assert.Single(violations, v => v.IsUnknown);
            Assert.Equal("unknownKey", unknownViolation.Key);
            Assert.Equal("Unknown key 'unknownKey' is present in config but not defined in schema", unknownViolation.Message);
            Assert.True(unknownViolation.IsUnknown);
            Assert.False(unknownViolation.IsMissing);
        }

        [Fact]
        public void Validate_UnknownKeys_WhenReportUnknownKeysIsFalse_NoViolationsForUnknownKeys()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["knownKey"] = "value1",
                ["unknownKey"] = "value2"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "knownKey" },
                TypeHints = new Dictionary<string, string> { ["knownKey"] = "string" }
            };

            // Set ReportUnknownKeys to false
            _validator.ReportUnknownKeys = false;

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert - Only violations should be for missing required keys, not unknown keys
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_NestedHierarchicalKeys_ReportsFullDottedPath()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Database:ConnectionString"] = "Server=localhost",
                ["Database:Timeout"] = "30"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "Database:ConnectionString", "Database:Timeout" },
                TypeHints = new Dictionary<string, string>
                {
                    ["Database:ConnectionString"] = "string",
                    ["Database:Timeout"] = "int"
                }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations); // Should be valid
            Assert.Equal("Database:ConnectionString", schema.RequiredKeys[0]);
            Assert.Equal("Database:Timeout", schema.RequiredKeys[1]);
        }

        [Fact]
        public void Validate_EmptySchema_AcceptsAnyValidConfig()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["anyKey"] = "anyValue",
                ["anotherKey"] = "123"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string>()
            };

            // Set ReportUnknownKeys to false to avoid unknown key violations
            _validator.ReportUnknownKeys = false;

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert - Empty schema should accept any config when unknown keys are not reported
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string>()
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!, schema));
        }

        [Fact]
        public void Validate_NullSchema_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["key"] = "value"
            };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _validator.Validate(config, null!));
        }

        [Fact]
        public void Validate_ValidIntType_PassesValidation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["port"] = "8080"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["port"] = "int" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_ValidBoolType_PassesValidation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["enabled"] = "true"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["enabled"] = "bool" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_ValidDoubleType_PassesValidation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["timeout"] = "30.5"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["timeout"] = "double" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_ValidDateTimeType_PassesValidation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["timestamp"] = "2024-01-01T12:00:00Z"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["timestamp"] = "datetime" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_ValidUrlType_PassesValidation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["endpoint"] = "https://example.com/api"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["endpoint"] = "url" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_ValidGuidType_PassesValidation()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["id"] = "550e8400-e29b-41d4-a716-446655440000"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["id"] = "guid" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_StringType_AcceptsAnyString()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["text"] = "any string value"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["text"] = "string" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.Empty(violations);
        }

        [Fact]
        public void Validate_MultipleViolations_ReportsAllIssues()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["validKey"] = "value",
                ["invalidInt"] = "not-a-number",
                ["unknownKey"] = "value"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string> { "validKey", "invalidInt", "requiredButMissingKey" },
                TypeHints = new Dictionary<string, string>
                {
                    ["validKey"] = "string",
                    ["invalidInt"] = "int",
                    ["requiredButMissingKey"] = "string"
                }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert - We get missing key violation (requiredButMissingKey not in config)
            // and unknown key violation (unknownKey not in schema)
            // Type violation for invalidInt is NOT reported because it's not in RequiredKeys
            Assert.Contains(violations, v => v.IsMissing && v.Key == "requiredButMissingKey");
            Assert.Contains(violations, v => v.IsUnknown && v.Key == "unknownKey");
            Assert.DoesNotContain(violations, v => v.Key == "invalidInt");
        }

        [Fact]
        public void Validate_CasingConflict_DetectsKeysDifferingOnlyByCasing()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ApiKey"] = "value1",
                ["apikey"] = "value2"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["ApiKey"] = "string" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var casingViolation = Assert.Single(violations, v => v.IsCasingConflict);
            Assert.Equal("apikey", casingViolation.Key);
            Assert.StartsWith("Key 'apikey' differs only by casing", casingViolation.Message);
            Assert.True(casingViolation.IsCasingConflict);
        }

        [Fact]
        public void Validate_SensitiveData_DetectsConnectionStringCredentials()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ConnectionString"] = "Server=localhost;Database=test;User Id=admin;Password=secret123;"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string> { ["ConnectionString"] = "string" }
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert
            Assert.NotEmpty(violations);
            var sensitiveViolation = Assert.Single(violations, v => v.IsSensitive);
            Assert.Equal("ConnectionString", sensitiveViolation.Key);
            Assert.Contains("connection string with credentials", sensitiveViolation.Message);
            Assert.True(sensitiveViolation.IsSensitive);
        }

        [Fact]
        public void Validate_ReportUnknownKeysDefaultValue_IsTrue()
        {
            // Assert - Default value should be true
            Assert.True(_validator.ReportUnknownKeys);
        }

        [Fact]
        public void Validate_UnknownKeyType_ReportsAsUnknownNotMissing()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["unknownTypeKey"] = "value"
            };

            var schema = new ConfigSchema
            {
                RequiredKeys = new List<string>(),
                TypeHints = new Dictionary<string, string>() // No type hint defined
            };

            // Act
            var violations = _validator.Validate(config, schema);

            // Assert - Should report as unknown key, not as type violation
            Assert.NotEmpty(violations);
            var unknownViolation = Assert.Single(violations, v => v.IsUnknown);
            Assert.Equal("unknownTypeKey", unknownViolation.Key);
            Assert.True(unknownViolation.IsUnknown);
            Assert.False(unknownViolation.IsMissing);
        }
    }
}
