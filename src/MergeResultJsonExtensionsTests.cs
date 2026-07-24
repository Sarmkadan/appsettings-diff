using System;
using System.Collections.Generic;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for MergeResultJsonExtensions to verify JSON serialization and deserialization
    /// preserves all fields including collections, and handles edge cases like null values correctly.
    /// </summary>
    public class MergeResultJsonExtensionsTests
    {
        /// <summary>
        /// Tests that ToJson serializes MergeResult to valid JSON with correct structure.
        /// </summary>
        [Fact]
        public void ToJson_SerializesMergeResultCorrectly()
        {
            // Arrange
            var conflict = new MergeConflict
            {
                Key = "ConflictedKey",
                BaseValue = "baseValue",
                OurValue = "ourValue",
                TheirValue = "theirValue",
                AutoResolved = true,
                Reason = "Both sides changed the value differently"
            };

            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" }, { "Key2", "value2" } },
                Conflicts = new List<MergeConflict> { conflict }
            };

            // Act
            var json = result.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"merged\"", json);
            Assert.Contains("\"conflicts\"", json);
            Assert.Contains("\"Key1\"", json);
            Assert.Contains("\"value1\"", json);
            Assert.Contains("\"ConflictedKey\"", json);
        }

        /// <summary>
        /// Tests that ToJson with indented=true produces formatted JSON.
        /// </summary>
        [Fact]
        public void ToJson_WithIndentedTrue_ProducesFormattedJson()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var json = result.ToJson(indented: true);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("{\n", json); // Indented JSON starts with newline
            Assert.Contains("  ", json); // Indented JSON has indentation
        }

        /// <summary>
        /// Tests that ToJson with indented=false produces compact JSON.
        /// </summary>
        [Fact]
        public void ToJson_WithIndentedFalse_ProducesCompactJson()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var json = result.ToJson(indented: false);

            // Assert
            Assert.NotNull(json);
            Assert.DoesNotContain("{\n", json); // Compact JSON doesn't start with newline
            Assert.DoesNotContain("  ", json); // Compact JSON has no indentation
        }

        /// <summary>
        /// Tests that FromJson deserializes valid JSON back to MergeResult.
        /// </summary>
        [Fact]
        public void FromJson_DeserializesValidJsonToMergeResult()
        {
            // Arrange - create JSON manually
            var json = "{"
                + "\"Merged\":{\"Key1\":\"value1\",\"Key2\":\"value2\"},"
                + "\"Conflicts\":["
                + "{\"Key\":\"ConflictKey\",\"BaseValue\":\"base\",\"OurValue\":\"our\",\"TheirValue\":\"their\",\"AutoResolved\":false,\"Reason\":null}"
                + "]}";

            // Act
            var result = MergeResultJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Merged.Count);
            Assert.Equal("value1", result.Merged["Key1"]);
            Assert.Equal("value2", result.Merged["Key2"]);
            Assert.Single(result.Conflicts);
            Assert.Equal("ConflictKey", result.Conflicts[0].Key);
            Assert.Equal("base", result.Conflicts[0].BaseValue);
            Assert.Equal("our", result.Conflicts[0].OurValue);
            Assert.Equal("their", result.Conflicts[0].TheirValue);
            Assert.False(result.Conflicts[0].AutoResolved);
        }

        /// <summary>
        /// Tests that FromJson handles empty merged dictionary correctly.
        /// </summary>
        [Fact]
        public void FromJson_HandlesEmptyMergedDictionary()
        {
            // Arrange
            var json = "{"
                + "\"Merged\":{},"
                + "\"Conflicts\":[]}";

            // Act
            var result = MergeResultJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Merged);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// Tests that FromJson handles empty conflicts list correctly.
        /// </summary>
        [Fact]
        public void FromJson_HandlesEmptyConflictsList()
        {
            // Arrange
            var json = "{"
                + "\"Merged\":{\"Key1\":\"value1\"},"
                + "\"Conflicts\":[]}";

            // Act
            var result = MergeResultJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Merged);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// Tests that FromJson deserializes null values correctly (they should be preserved).
        /// </summary>
        [Fact]
        public void FromJson_PreservesNullValuesInConflicts()
        {
            // Arrange - JSON with null values
            var json = "{"
                + "\"Merged\":{\"Key1\":\"value1\"},"
                + "\"Conflicts\":["
                + "{\"Key\":\"Key1\",\"BaseValue\":null,\"OurValue\":null,\"TheirValue\":null,\"AutoResolved\":false,\"Reason\":null}"
                + "]}";

            // Act
            var result = MergeResultJsonExtensions.FromJson(json);

            // Assert - null values should be preserved
            Assert.NotNull(result);
            Assert.Single(result.Conflicts);
            Assert.Null(result.Conflicts[0].BaseValue);
            Assert.Null(result.Conflicts[0].OurValue);
            Assert.Null(result.Conflicts[0].TheirValue);
        }

        /// <summary>
        /// Tests that TryFromJson returns true for valid JSON.
        /// </summary>
        [Fact]
        public void TryFromJson_ReturnsTrueForValidJson()
        {
            // Arrange
            var json = "{"
                + "\"Merged\":{\"Key1\":\"value1\"},"
                + "\"Conflicts\":[]}";

            // Act
            var success = MergeResultJsonExtensions.TryFromJson(json, out var result);

            // Assert
            Assert.True(success);
            Assert.NotNull(result);
        }

        /// <summary>
        /// Tests that TryFromJson returns false for invalid JSON.
        /// </summary>
        [Fact]
        public void TryFromJson_ReturnsFalseForInvalidJson()
        {
            // Arrange
            var json = "invalid json { not valid";

            // Act
            var success = MergeResultJsonExtensions.TryFromJson(json, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that TryFromJson throws ArgumentException for null JSON.
        /// </summary>
        [Fact]
        public void TryFromJson_ThrowsArgumentExceptionForNullJson()
        {
            // Arrange
            string? json = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => MergeResultJsonExtensions.TryFromJson(json!, out _));
        }

        /// <summary>
        /// Tests that TryFromJson throws ArgumentException for empty JSON.
        /// </summary>
        [Fact]
        public void TryFromJson_ThrowsArgumentExceptionForEmptyJson()
        {
            // Arrange
            var json = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => MergeResultJsonExtensions.TryFromJson(json, out _));
        }

        /// <summary>
        /// Tests that FromJson throws ArgumentException for null JSON.
        /// </summary>
        [Fact]
        public void FromJson_ThrowsArgumentExceptionForNullJson()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => MergeResultJsonExtensions.FromJson(null!));
            Assert.Equal("Value cannot be null or empty. (Parameter 'json')", exception.Message);
        }

        /// <summary>
        /// Tests that FromJson throws ArgumentException for empty JSON.
        /// </summary>
        [Fact]
        public void FromJson_ThrowsArgumentExceptionForEmptyJson()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => MergeResultJsonExtensions.FromJson(""));
            Assert.Equal("Value cannot be null or empty. (Parameter 'json')", exception.Message);
        }

        /// <summary>
        /// Tests that FromJson throws ArgumentException for whitespace-only JSON.
        /// </summary>
        [Fact]
        public void FromJson_ThrowsArgumentExceptionForWhitespaceJson()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => MergeResultJsonExtensions.FromJson("   "));
            Assert.Equal("Value cannot be null or empty. (Parameter 'json')", exception.Message);
        }

        /// <summary>
        /// Tests that round-trip serialization (ToJson -> FromJson) preserves all fields.
        /// </summary>
        [Fact]
        public void MergeResultJsonExtensions_RoundTrip_PreservesAllFields()
        {
            // Arrange - create a complex MergeResult with various field values
            var conflict = new MergeConflict
            {
                Key = "TestKey",
                BaseValue = "baseValue",
                OurValue = "ourValue",
                TheirValue = "theirValue",
                AutoResolved = true,
                Reason = "Test conflict reason"
            };

            var original = new MergeResult
            {
                Merged = new Dictionary<string, string>
                {
                    { "SimpleKey", "simpleValue" },
                    { "Key:With:Colons", "valueWith:Colons" },
                    { "EmptyValueKey", "" },
                    { "NullValueKey", null }
                },
                Conflicts = new List<MergeConflict> { conflict }
            };

            // Act - serialize and deserialize
            var json = original.ToJson();
            var deserialized = MergeResultJsonExtensions.FromJson(json);

            // Assert - all fields should be preserved
            Assert.NotNull(deserialized);
            Assert.Equal(original.Merged.Count, deserialized.Merged.Count);
            Assert.Equal("simpleValue", deserialized.Merged["SimpleKey"]);
            Assert.Equal("valueWith:Colons", deserialized.Merged["Key:With:Colons"]);
            Assert.Equal("", deserialized.Merged["EmptyValueKey"]);
            Assert.Null(deserialized.Merged["NullValueKey"]);

            Assert.Single(deserialized.Conflicts);
            Assert.Equal("TestKey", deserialized.Conflicts[0].Key);
            Assert.Equal("baseValue", deserialized.Conflicts[0].BaseValue);
            Assert.Equal("ourValue", deserialized.Conflicts[0].OurValue);
            Assert.Equal("theirValue", deserialized.Conflicts[0].TheirValue);
            Assert.True(deserialized.Conflicts[0].AutoResolved);
            Assert.Equal("Test conflict reason", deserialized.Conflicts[0].Reason);
        }

        /// <summary>
        /// Tests that MergeResultJsonExtensions handles collections correctly in round-trip.
        /// </summary>
        [Fact]
        public void MergeResultJsonExtensions_RoundTrip_PreservesCollections()
        {
            // Arrange - create MergeResult with multiple conflicts
            var conflicts = new List<MergeConflict>();
            for (int i = 0; i < 10; i++)
            {
                conflicts.Add(new MergeConflict
                {
                    Key = $"Key{i}",
                    BaseValue = $"base{i}",
                    OurValue = $"our{i}",
                    TheirValue = $"their{i}",
                    AutoResolved = i % 2 == 0
                });
            }

            var original = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = conflicts
            };

            // Act - serialize and deserialize
            var json = original.ToJson();
            var deserialized = MergeResultJsonExtensions.FromJson(json);

            // Assert - collection should be preserved
            Assert.NotNull(deserialized);
            Assert.Equal(10, deserialized.Conflicts.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal($"Key{i}", deserialized.Conflicts[i].Key);
                Assert.Equal($"base{i}", deserialized.Conflicts[i].BaseValue);
                Assert.Equal($"our{i}", deserialized.Conflicts[i].OurValue);
                Assert.Equal($"their{i}", deserialized.Conflicts[i].TheirValue);
            }
        }

        /// <summary>
        /// Tests that MergeResultJsonExtensions does not silently drop keys with null values.
        /// </summary>
        [Fact]
        public void MergeResultJsonExtensions_DoesNotDropKeysWithNullValues()
        {
            // Arrange - create MergeResult with null values
            var original = new MergeResult
            {
                Merged = new Dictionary<string, string>
                {
                    { "NullKey1", null },
                    { "NullKey2", null },
                    { "NonNullKey", "value" }
                },
                Conflicts = new List<MergeConflict>()
            };

            // Act - serialize and deserialize
            var json = original.ToJson();
            var deserialized = MergeResultJsonExtensions.FromJson(json);

            // Assert - null value keys should be preserved
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Merged.Count);
            Assert.Null(deserialized.Merged["NullKey1"]);
            Assert.Null(deserialized.Merged["NullKey2"]);
            Assert.Equal("value", deserialized.Merged["NonNullKey"]);

            // Verify JSON contains the null keys
            Assert.Contains("\"NullKey1\"", json);
            Assert.Contains("\"NullKey2\"", json);
        }
    }
}
