using System;
using System.Collections.Generic;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for EnvVarOverlay to improve branch and edge-case coverage.
    /// Tests precedence rules and edge cases for environment variable overlay functionality.
    /// </summary>
    public class EnvVarOverlayTests
    {
        /// <summary>
        /// Tests that ReadFromEnvironment throws ArgumentNullException when prefix is null.
        /// </summary>
        [Fact]
        public void ReadFromEnvironment_NullPrefix_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.ReadFromEnvironment(null));
        }

        /// <summary>
        /// Tests that ReadFromEnvironment filters variables by prefix correctly.
        /// </summary>
        [Fact]
        public void ReadFromEnvironment_WithPrefix_ReturnsFilteredVariables()
        {
            // Arrange
            var originalValue = Environment.GetEnvironmentVariable("TEST_PREFIX_VAR");
            try
            {
                Environment.SetEnvironmentVariable("TEST_PREFIX_VAR", "value1");
                Environment.SetEnvironmentVariable("OTHER_VAR", "value2");
                Environment.SetEnvironmentVariable("TEST_PREFIX_ANOTHER", "value3");

                // Act
                var result = EnvVarOverlay.ReadFromEnvironment("TEST_PREFIX_");

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value1", result["TEST_PREFIX_VAR"]);
                Assert.Equal("value3", result["TEST_PREFIX_ANOTHER"]);
                Assert.DoesNotContain(result, kvp => kvp.Key == "OTHER_VAR");
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("TEST_PREFIX_VAR", originalValue);
                Environment.SetEnvironmentVariable("OTHER_VAR", null);
                Environment.SetEnvironmentVariable("TEST_PREFIX_ANOTHER", null);
            }
        }

        /// <summary>
        /// Tests that Normalize throws ArgumentNullException when input is null.
        /// </summary>
        [Fact]
        public void Normalize_NullInput_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.Normalize(null!));
        }

        /// <summary>
        /// Tests that Normalize returns empty dictionary for empty input.
        /// </summary>
        [Fact]
        public void Normalize_EmptyDictionary_ReturnsEmptyDictionary()
        {
            // Arrange
            var envVars = new Dictionary<string, string>();

            // Act
            var result = EnvVarOverlay.Normalize(envVars);

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that Normalize removes ASPNETCORE_ prefix correctly.
        /// </summary>
        [Fact]
        public void Normalize_RemovesAspNetCorePrefix()
        {
            // Arrange
            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ASPNETCORE_ConnectionString"] = "Server=localhost",
                ["ASPNETCORE_Timeout"] = "30",
                ["OtherVar"] = "value"
            };

            // Act
            var result = EnvVarOverlay.Normalize(envVars);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Server=localhost", result["ConnectionString"]);
            Assert.Equal("30", result["Timeout"]);
            Assert.Equal("value", result["OtherVar"]);
        }

        /// <summary>
        /// Tests that Normalize removes DOTNET_ prefix correctly.
        /// </summary>
        [Fact]
        public void Normalize_RemovesDotnetPrefix()
        {
            // Arrange
            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DOTNET_Environment"] = "Development",
                ["DOTNET_EnableDebug"] = "true"
            };

            // Act
            var result = EnvVarOverlay.Normalize(envVars);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Development", result["Environment"]);
            Assert.Equal("true", result["EnableDebug"]);
        }

        /// <summary>
        /// Tests that Normalize replaces double underscore with colon correctly.
        /// </summary>
        [Fact]
        public void Normalize_ReplacesDoubleUnderscoreWithColon()
        {
            // Arrange
            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Database__ConnectionString"] = "Server=localhost",
                ["Logging__LogLevel__Default"] = "Information",
                ["Nested__Deep__Value"] = "test"
            };

            // Act
            var result = EnvVarOverlay.Normalize(envVars);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Server=localhost", result["Database:ConnectionString"]);
            Assert.Equal("Information", result["Logging:LogLevel:Default"]);
            Assert.Equal("test", result["Nested:Deep:Value"]);
        }

        /// <summary>
        /// Tests that Normalize handles case-insensitive keys correctly.
        /// </summary>
        [Fact]
        public void Normalize_CaseInsensitiveKeys()
        {
            // Arrange
            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DATABASE__CONNECTIONSTRING"] = "Server1",
                ["database__connectionstring"] = "Server2"
            };

            // Act
            var result = EnvVarOverlay.Normalize(envVars);

            // Assert - Last one should win due to case-insensitive dictionary
            Assert.Single(result);
            Assert.Equal("Server2", result["Database:ConnectionString"]);
        }

        /// <summary>
        /// Tests that Apply correctly overrides existing values.
        /// </summary>
        [Fact]
        public void Apply_BasicOverride_Precedence()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "OriginalValue",
                ["Timeout"] = "10"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "OverriddenValue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("OverriddenValue", result["ConnectionString"]);
            Assert.Equal("10", result["Timeout"]);
            Assert.Single(overriddenKeys);
            Assert.Equal("ConnectionString", overriddenKeys[0]);
        }

        /// <summary>
        /// Tests that Apply throws ArgumentNullException when config is null.
        /// </summary>
        [Fact]
        public void Apply_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var envVars = new Dictionary<string, string> { ["key"] = "value" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.Apply(null!, envVars, out _));
        }

        /// <summary>
        /// Tests that Apply throws ArgumentNullException when envVars is null.
        /// </summary>
        [Fact]
        public void Apply_NullEnvVars_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new Dictionary<string, string> { ["key"] = "value" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.Apply(config, null!, out _));
        }

        /// <summary>
        /// Tests that Apply correctly handles empty string values (overrides existing).
        /// </summary>
        [Fact]
        public void Apply_EmptyStringValue_OverridesExistingValue()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "OriginalValue"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = ""
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert - Empty string should override the original value
            Assert.Single(result);
            Assert.Equal("", result["Setting"]);
            Assert.Single(overriddenKeys);
            Assert.Equal("Setting", overriddenKeys[0]);
        }

        /// <summary>
        /// Tests that Apply correctly handles empty string values (creates new key).
        /// </summary>
        [Fact]
        public void Apply_EmptyStringValue_CreatesNewKey()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NewSetting"] = ""
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert - Empty string should create the key
            Assert.Single(result);
            Assert.Equal("", result["NewSetting"]);
            Assert.Empty(overriddenKeys); // No override since key didn't exist
        }

        /// <summary>
        /// Tests that Apply correctly handles nested keys via double underscore convention.
        /// </summary>
        [Fact]
        public void Apply_NestedKeyViaDoubleUnderscore_OverridesNestedValue()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Database:ConnectionString"] = "Server=original;Database=test",
                ["Database:Timeout"] = "30"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Database__ConnectionString"] = "Server=overridden;Database=newtest"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Server=overridden;Database=newtest", result["Database:ConnectionString"]);
            Assert.Equal("30", result["Database:Timeout"]);
            Assert.Single(overriddenKeys);
            Assert.Equal("Database:ConnectionString", overriddenKeys[0]);
        }

        /// <summary>
        /// Tests that Apply creates deeply nested paths when they don't exist.
        /// </summary>
        [Fact]
        public void Apply_DeeplyNestedKeyViaDoubleUnderscore_CreatesNewPath()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App:Features:Logging"] = "true"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App__Features__Database__ConnectionString"] = "Server=localhost"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert - Should create the full nested path
            Assert.Equal(2, result.Count);
            Assert.Equal("true", result["App:Features:Logging"]);
            Assert.Equal("Server=localhost", result["App:Features:Database:ConnectionString"]);
            Assert.Empty(overriddenKeys); // No override since key didn't exist
        }

        /// <summary>
        /// Tests that Apply handles case-insensitive matching correctly.
        /// </summary>
        [Fact]
        public void Apply_CaseInsensitiveMatching_OverridesWithDifferentCase()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "OriginalValue"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["connectionstring"] = "OverriddenValue",
                ["CONNECTIONSTRING"] = "AnotherValue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert - Should match case-insensitively and last one wins
            Assert.Single(result);
            Assert.Equal("AnotherValue", result["ConnectionString"]);
            Assert.Single(overriddenKeys);
            // The overridden key is in the result dictionary (case-insensitive comparison)
            Assert.Contains("ConnectionString", overriddenKeys, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that Apply works correctly with custom prefix filtering and stripping.
        /// </summary>
        [Fact]
        public void Apply_WithCustomPrefix_FiltersAndStripsPrefix()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting1"] = "value1",
                ["Setting2"] = "value2"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["APP_SETTING1"] = "newvalue1",
                ["APP_SETTING2"] = "newvalue2",
                ["APP_NewSetting"] = "newvalue3",
                ["OTHER_SETTING1"] = "ignored"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, "APP_", out var overriddenKeys);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("newvalue1", result["Setting1"]);
            Assert.Equal("newvalue2", result["Setting2"]);
            Assert.Equal("newvalue3", result["NewSetting"]);
            Assert.Equal(2, overriddenKeys.Count);
            // The overridden keys are in the result dictionary (case-insensitive comparison)
            Assert.Contains("Setting1", overriddenKeys, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Setting2", overriddenKeys, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests that Apply returns unchanged config when no variables match custom prefix.
        /// </summary>
        [Fact]
        public void Apply_WithCustomPrefix_NoMatchingPrefix_ReturnsUnchangedConfig()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "value"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTHER_Setting"] = "newvalue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, "APP_", out var overriddenKeys);

            // Assert
            Assert.Single(result);
            Assert.Equal("value", result["Setting"]);
            Assert.Empty(overriddenKeys);
        }

        /// <summary>
        /// Tests that Apply with empty prefix uses all variables.
        /// </summary>
        [Fact]
        public void Apply_WithCustomPrefix_EmptyPrefix_UsesAllVariables()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "value"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "newvalue",
                ["OtherSetting"] = "othervalue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, "", out var overriddenKeys);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("newvalue", result["Setting"]);
            Assert.Equal("othervalue", result["OtherSetting"]);
            Assert.Single(overriddenKeys);
        }

        /// <summary>
        /// Tests that Apply with null prefix uses all variables.
        /// </summary>
        [Fact]
        public void Apply_WithCustomPrefix_NullPrefix_UsesAllVariables()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "value"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "newvalue",
                ["OtherSetting"] = "othervalue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, null, out var overriddenKeys);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("newvalue", result["Setting"]);
            Assert.Equal("othervalue", result["OtherSetting"]);
            Assert.Single(overriddenKeys);
        }

        /// <summary>
        /// Tests that Apply creates new keys when they don't exist in original config.
        /// </summary>
        [Fact]
        public void Apply_CreatesNewKeys_WhenKeyDoesNotExist()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ExistingKey"] = "value"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NewKey1"] = "newvalue1",
                ["NewKey2"] = "newvalue2"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("value", result["ExistingKey"]);
            Assert.Equal("newvalue1", result["NewKey1"]);
            Assert.Equal("newvalue2", result["NewKey2"]);
            Assert.Empty(overriddenKeys);
        }

        /// <summary>
        /// Tests that Apply correctly handles ASPNETCORE_ prefix removal and application.
        /// </summary>
        [Fact]
        public void Apply_WithAspNetCorePrefix_RemovesPrefixAndApplies()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "OriginalValue"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ASPNETCORE_ConnectionString"] = "OverriddenValue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Single(result);
            Assert.Equal("OverriddenValue", result["ConnectionString"]);
            Assert.Single(overriddenKeys);
            Assert.Equal("ConnectionString", overriddenKeys[0]);
        }

        /// <summary>
        /// Tests that Apply correctly handles DOTNET_ prefix removal and application.
        /// </summary>
        [Fact]
        public void Apply_WithDotnetPrefix_RemovesPrefixAndApplies()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Environment"] = "Production"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["DOTNET_Environment"] = "Development"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Single(result);
            Assert.Equal("Development", result["Environment"]);
            Assert.Single(overriddenKeys);
            Assert.Equal("Environment", overriddenKeys[0]);
        }

        /// <summary>
        /// Tests that Apply correctly applies multiple environment variables in order.
        /// </summary>
        [Fact]
        public void Apply_MultipleEnvVars_AllAppliedInOrder()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key1"] = "original1",
                ["Key2"] = "original2",
                ["Key3"] = "original3"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key1"] = "new1",
                ["Key3"] = "new3",
                ["Key4"] = "new4"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Equal("new1", result["Key1"]);
            Assert.Equal("original2", result["Key2"]);
            Assert.Equal("new3", result["Key3"]);
            Assert.Equal("new4", result["Key4"]);
            Assert.Equal(2, overriddenKeys.Count);
            Assert.Contains("Key1", overriddenKeys);
            Assert.Contains("Key3", overriddenKeys);
        }

        /// <summary>
        /// Tests that Apply preserves whitespace in values exactly as provided.
        /// </summary>
        [Fact]
        public void Apply_WhitespaceInValues_PreservedAsIs()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "original"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Setting"] = "  value with spaces  "
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Single(result);
            Assert.Equal("  value with spaces  ", result["Setting"]);
        }

        /// <summary>
        /// Tests that Apply preserves special characters in values exactly as provided.
        /// </summary>
        [Fact]
        public void Apply_SpecialCharactersInValues_PreservedAsIs()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "original"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "Server=localhost;Database=test;User=admin@domain.com;Password=P@ssw0rd!#$%"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Single(result);
            Assert.Equal("Server=localhost;Database=test;User=admin@domain.com;Password=P@ssw0rd!#$%", result["ConnectionString"]);
        }

        /// <summary>
        /// Tests that Apply correctly tracks all overridden keys in the output list.
        /// </summary>
        [Fact]
        public void Apply_OverriddenKeysList_ContainsAllOverriddenKeys()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key1"] = "value1",
                ["Key2"] = "value2",
                ["Key3"] = "value3"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key1"] = "new1",
                ["Key2"] = "new2"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Equal(2, overriddenKeys.Count);
            Assert.Contains("Key1", overriddenKeys);
            Assert.Contains("Key2", overriddenKeys);
            Assert.DoesNotContain(overriddenKeys, k => k == "Key3");
        }

        /// <summary>
        /// Tests that Apply returns empty overridden keys list when no overrides occur.
        /// </summary>
        [Fact]
        public void Apply_OverriddenKeysList_EmptyWhenNoOverrides()
        {
            // Arrange
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key1"] = "value1"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Key2"] = "value2"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert
            Assert.Empty(overriddenKeys);
        }

        /// <summary>
        /// Tests Linux-style lowercase environment variable names override Windows-style mixed case config keys.
        /// </summary>
        [Fact]
        public void Apply_LinuxStyleCasing_OverridesWindowsStyleConfigKeys()
        {
            // Arrange - Linux-style lowercase env var should match Windows-style PascalCase config key
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "OriginalValue"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["connectionstring"] = "OverriddenValue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert - Should match case-insensitively regardless of platform
            Assert.Single(result);
            Assert.Equal("OverriddenValue", result["ConnectionString"]);
            Assert.Single(overriddenKeys);
            Assert.Contains("ConnectionString", overriddenKeys, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Tests Windows-style mixed case environment variable names override Linux-style lowercase config keys.
        /// </summary>
        [Fact]
        public void Apply_WindowsStyleCasing_OverridesLinuxStyleConfigKeys()
        {
            // Arrange - Windows-style mixed case env var should match Linux-style lowercase config key
            var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["connectionstring"] = "OriginalValue"
            };

            var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ConnectionString"] = "OverriddenValue"
            };

            // Act
            var result = EnvVarOverlay.Apply(config, envVars, out var overriddenKeys);

            // Assert - Should match case-insensitively regardless of platform
            Assert.Single(result);
            Assert.Equal("OverriddenValue", result["ConnectionString"]);
            Assert.Single(overriddenKeys);
            Assert.Contains("ConnectionString", overriddenKeys, StringComparer.OrdinalIgnoreCase);
        }

    }
}
