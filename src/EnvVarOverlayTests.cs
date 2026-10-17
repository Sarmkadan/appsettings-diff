using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for EnvVarOverlay to improve branch and edge-case coverage.
    /// Tests precedence rules and edge cases for environment variable overlay functionality.
    /// </summary>
    public class EnvVarOverlayTests
    {
        // ... existing tests ...

        /// <summary>
        /// Integration test: loads base appsettings.json via JsonConfigReader, applies environment variables with a prefix using EnvVarOverlay,
        /// verifies that __ is translated to :, keys not present in JSON are added, variables without prefix are ignored,
        /// and that the diff between original and overlaid config shows exactly the env-driven changes.
        /// </summary>
        [Fact]
        public void EnvVarOverlay_Integration_WithJsonConfigReader_AndConfigDiffer()
        {
            // Arrange: create a temporary appsettings.json file
            var tempFile = Path.GetTempFileName();
            try
            {
                const string baseJson = @"
{
  ""Key1"": ""original"",
  ""Section"": {
    ""Key2"": ""original-section""
  }
}";
                File.WriteAllText(tempFile, baseJson);

                // Define environment variables to set
                const string prefix = "PREFIX_";
                var envVarsToSet = new Dictionary<string, string>
                {
                    [$"{prefix}Key1"] = "overridden-value",
                    [$"{prefix}Section__Key2"] = "section-overridden",
                    [$"{prefix}Section__Key3"] = "added-section",
                    [$"{prefix}NewKey"] = "added-key",
                    ["IGNORED"] = "ignore" // should be ignored because no prefix
                };

                // Store original values and set new ones
                var originalValues = new Dictionary<string, string?>();
                foreach (var kvp in envVarsToSet)
                {
                    originalValues[kvp.Key] = Environment.GetEnvironmentVariable(kvp.Key);
                    Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                }

                try
                {
                    // Act: read base config
                    var reader = new JsonConfigReader();
                    var baseConfigDict = reader.ReadJsonConfig(tempFile);
                    var baseline = new FlatConfig();
                    foreach (var kvp in baseConfigDict)
                    {
                        baseline.Values[kvp.Key] = kvp.Value;
                    }

                    // Apply overlay with the specified prefix
                    var envVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string key in Environment.GetEnvironmentVariables().Keys)
                    {
                        string? value = Environment.GetEnvironmentVariable(key);
                        if (value != null)
                        {
                            envVars[key] = value;
                        }
                    }
                    var overlaidDict = EnvVarOverlay.Apply(baseConfigDict, envVars, prefix, out var overriddenKeys);
                    var target = new FlatConfig();
                    foreach (var kvp in overlaidDict)
                    {
                        target.Values[kvp.Key] = kvp.Value;
                    }

                    // Assert: overlaid values win, __ translated to :, keys not present added, non-prefixed ignored
                    Assert.Equal("overridden-value", overlaidDict["Key1"]);
                    Assert.Equal("section-overridden", overlaidDict["Section:Key2"]);
                    Assert.Equal("added-section", overlaidDict["Section:Key3"]);
                    Assert.Equal("added-key", overlaidDict["NewKey"]);
                    Assert.False(overlaidDict.ContainsKey("IGNORED"));

                    // Now feed the overlaid config and the original into ConfigDiffer
                    var detector = new SensitiveKeyDetector();
                    var differ = new ConfigDiffer(detector);
                    var result = differ.Diff(baseline, target);

                    // Assert diff shows exactly the env-driven changes
                    Assert.True(result.HasDifferences, "Expected differences due to environment variables");

                    // We expect 4 differences: Key1 (override), Section:Key2 (override), Section:Key3 (addition), NewKey (addition)
                    Assert.Equal(4, result.Entries.Count);

                    var diffKeys = result.Entries.Select(e => e.Key).ToList();
                    Assert.Contains("Key1", diffKeys);
                    Assert.Contains("Section:Key2", diffKeys);
                    Assert.Contains("Section:Key3", diffKeys);
                    Assert.Contains("NewKey", diffKeys);

                    // Ensure no other keys appear in diff
                    Assert.DoesNotContain("Section", diffKeys); // parent object not a change
                    Assert.DoesNotContain("IGNORED", diffKeys);
                }
                finally
                {
                    // Restore environment variables
                    foreach (var kvp in originalValues)
                    {
                        if (kvp.Value == null)
                            Environment.SetEnvironmentVariable(kvp.Key, null);
                        else
                            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Prefixes that are not valid: whitespace-only, containing whitespace, or containing ':'.
        /// </summary>
        public static IEnumerable<object[]> InvalidPrefixes => new List<object[]>
        {
            new object[] { " " },
            new object[] { "\t" },
            new object[] { "APP " },
            new object[] { " APP" },
            new object[] { "AP P" },
            new object[] { "APP\t" },
            new object[] { "APP:" },
            new object[] { "A:B" },
        };

        [Theory]
        [MemberData(nameof(InvalidPrefixes))]
        public void ReadFromEnvironment_InvalidPrefix_ThrowsArgumentExceptionNamingPrefix(string prefix)
        {
            var ex = Assert.Throws<ArgumentException>(() => EnvVarOverlay.ReadFromEnvironment(prefix));

            Assert.Equal("prefix", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ReadFromEnvironment_NullOrEmptyPrefix_ThrowsArgumentException(string? prefix)
        {
            var ex = Assert.ThrowsAny<ArgumentException>(() => EnvVarOverlay.ReadFromEnvironment(prefix));

            Assert.Equal("prefix", ex.ParamName);
        }

        [Theory]
        [MemberData(nameof(InvalidPrefixes))]
        public void Apply_InvalidPrefix_ThrowsArgumentExceptionNamingPrefix(string prefix)
        {
            var config = new Dictionary<string, string>();
            var envVars = new Dictionary<string, string> { ["APP_Key"] = "value" };

            var ex = Assert.Throws<ArgumentException>(() => EnvVarOverlay.Apply(config, envVars, prefix, out _));

            Assert.Equal("prefix", ex.ParamName);
        }

        [Fact]
        public void Apply_NullConfig_ThrowsArgumentNullExceptionNamingConfig()
        {
            var envVars = new Dictionary<string, string>();

            var ex = Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.Apply(null!, envVars, "APP_", out _));

            Assert.Equal("config", ex.ParamName);
        }

        [Fact]
        public void Apply_NullEnvVars_ThrowsArgumentNullExceptionNamingEnvVars()
        {
            var config = new Dictionary<string, string>();

            var ex = Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.Apply(config, null!, "APP_", out _));

            Assert.Equal("envVars", ex.ParamName);
        }

        [Fact]
        public void Apply_NullEnvVarsWithoutPrefix_ThrowsArgumentNullExceptionNamingEnvVars()
        {
            var config = new Dictionary<string, string>();

            var ex = Assert.Throws<ArgumentNullException>(() => EnvVarOverlay.Apply(config, null!, out _));

            Assert.Equal("envVars", ex.ParamName);
        }

        [Fact]
        public void Apply_PrefixAPP_StripsPrefixAndTranslatesSectionSeparator()
        {
            var config = new Dictionary<string, string>
            {
                ["ConnectionStrings:Default"] = "original"
            };
            var envVars = new Dictionary<string, string>
            {
                ["APP_ConnectionStrings__Default"] = "from-env",
                ["APP_NewSection__Key"] = "added",
                ["OTHER_ConnectionStrings__Default"] = "ignored"
            };

            var result = EnvVarOverlay.Apply(config, envVars, "APP_", out var overridden);

            Assert.Equal("from-env", result["ConnectionStrings:Default"]);
            Assert.Equal("added", result["NewSection:Key"]);
            Assert.DoesNotContain("APP_ConnectionStrings:Default", result.Keys);
            Assert.DoesNotContain("OTHER_ConnectionStrings:Default", result.Keys);
            Assert.Equal(new[] { "ConnectionStrings:Default" }, overridden);
        }

        [Fact]
        public void Apply_NullOrEmptyPrefix_DoesNotFilterOrStrip()
        {
            var config = new Dictionary<string, string>();
            var envVars = new Dictionary<string, string> { ["Plain__Key"] = "value" };

            var nullPrefixResult = EnvVarOverlay.Apply(config, envVars, null, out _);
            var emptyPrefixResult = EnvVarOverlay.Apply(config, envVars, "", out _);

            Assert.Equal("value", nullPrefixResult["Plain:Key"]);
            Assert.Equal("value", emptyPrefixResult["Plain:Key"]);
        }
    }
}