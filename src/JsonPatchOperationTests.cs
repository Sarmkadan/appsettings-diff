using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for JsonPatchOperation to verify RFC 6902 compliance and round-trip serialization.
    /// Tests serialization of different operation types and ensures they map to correct JSON shapes.
    /// </summary>
    public class JsonPatchOperationTests
    {
        /// <summary>
        /// Tests that an 'add' operation serializes to RFC 6902-compliant JSON with 'op', 'path', and 'value' fields.
        /// </summary>
        [Fact]
        public void AddOperation_SerializesCorrectly()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "add",
                Path = "/Section/Key",
                Value = "newValue"
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("add", deserialized.Op);
            Assert.Equal("/Section/Key", deserialized.Path);
            Assert.Equal("newValue", deserialized.Value);
            Assert.Null(deserialized.From);

            // Verify JSON shape matches RFC 6902
            Assert.Contains("\"op\":\"add\"", json);
            Assert.Contains("\"path\":\"/Section/Key\"", json);
            Assert.Contains("\"value\":\"newValue\"", json);
        }

        /// <summary>
        /// Tests that a 'remove' operation serializes to RFC 6902-compliant JSON with only 'op' and 'path' fields.
        /// </summary>
        [Fact]
        public void RemoveOperation_SerializesCorrectly()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "remove",
                Path = "/Section/Key"
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("remove", deserialized.Op);
            Assert.Equal("/Section/Key", deserialized.Path);
            Assert.Null(deserialized.Value);
            Assert.Null(deserialized.From);

            // Verify JSON shape matches RFC 6902 - 'value' field should be absent
            Assert.DoesNotContain("\"value\"", json);
            Assert.Contains("\"op\":\"remove\"", json);
            Assert.Contains("\"path\":\"/Section/Key\"", json);
        }

        /// <summary>
        /// Tests that a 'replace' operation serializes to RFC 6902-compliant JSON with 'op', 'path', and 'value' fields.
        /// </summary>
        [Fact]
        public void ReplaceOperation_SerializesCorrectly()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "replace",
                Path = "/Config/Timeout",
                Value = "30"
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("replace", deserialized.Op);
            Assert.Equal("/Config/Timeout", deserialized.Path);
            Assert.Equal("30", deserialized.Value);
            Assert.Null(deserialized.From);

            // Verify JSON shape matches RFC 6902
            Assert.Contains("\"op\":\"replace\"", json);
            Assert.Contains("\"path\":\"/Config/Timeout\"", json);
            Assert.Contains("\"value\":\"30\"", json);
        }

        /// <summary>
        /// Tests that a 'move' operation serializes to RFC 6902-compliant JSON with 'op', 'path', and 'from' fields.
        /// </summary>
        [Fact]
        public void MoveOperation_SerializesCorrectly()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "move",
                Path = "/NewLocation",
                From = "/OldLocation"
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("move", deserialized.Op);
            Assert.Equal("/NewLocation", deserialized.Path);
            Assert.Null(deserialized.Value);
            Assert.Equal("/OldLocation", deserialized.From);

            // Verify JSON shape matches RFC 6902
            Assert.Contains("\"op\":\"move\"", json);
            Assert.Contains("\"path\":\"/NewLocation\"", json);
            Assert.Contains("\"from\":\"/OldLocation\"", json);
            Assert.DoesNotContain("\"value\"", json);
        }

        /// <summary>
        /// Tests that a 'copy' operation serializes to RFC 6902-compliant JSON with 'op', 'path', and 'from' fields.
        /// </summary>
        [Fact]
        public void CopyOperation_SerializesCorrectly()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "copy",
                Path = "/Target",
                From = "/Source"
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("copy", deserialized.Op);
            Assert.Equal("/Target", deserialized.Path);
            Assert.Null(deserialized.Value);
            Assert.Equal("/Source", deserialized.From);

            // Verify JSON shape matches RFC 6902
            Assert.Contains("\"op\":\"copy\"", json);
            Assert.Contains("\"path\":\"/Target\"", json);
            Assert.Contains("\"from\":\"/Source\"", json);
            Assert.DoesNotContain("\"value\"", json);
        }

        /// <summary>
        /// Tests that a 'test' operation serializes to RFC 6902-compliant JSON with 'op', 'path', and 'value' fields.
        /// </summary>
        [Fact]
        public void TestOperation_SerializesCorrectly()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "test",
                Path = "/Config/Enabled",
                Value = "true"
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("test", deserialized.Op);
            Assert.Equal("/Config/Enabled", deserialized.Path);
            Assert.Equal("true", deserialized.Value);
            Assert.Null(deserialized.From);

            // Verify JSON shape matches RFC 6902
            Assert.Contains("\"op\":\"test\"", json);
            Assert.Contains("\"path\":\"/Config/Enabled\"", json);
            Assert.Contains("\"value\":\"true\"", json);
        }

        /// <summary>
        /// Tests that FromConfigKey correctly converts configuration keys to JSON Pointer format.
        /// </summary>
        [Fact]
        public void FromConfigKey_ConvertsCorrectly()
        {
            // Arrange & Act & Assert
            Assert.Equal("/Section", JsonPatchOperation.FromConfigKey("Section"));
            Assert.Equal("/Section/Key", JsonPatchOperation.FromConfigKey("Section:Key"));
            Assert.Equal("/ConnectionStrings/DefaultConnection", JsonPatchOperation.FromConfigKey("ConnectionStrings:DefaultConnection"));
            Assert.Equal("/Logging/LogLevel/Default", JsonPatchOperation.FromConfigKey("Logging:LogLevel:Default"));
        }

        /// <summary>
        /// Tests that FromConfigKey escapes special JSON Pointer characters '~' and '/'.
        /// </summary>
        [Fact]
        public void FromConfigKey_EscapesSpecialCharacters()
        {
            // Arrange & Act & Assert
            Assert.Equal("/Section~0Key", JsonPatchOperation.FromConfigKey("Section~Key"));
            Assert.Equal("/Section~1Key", JsonPatchOperation.FromConfigKey("Section/Key"));
            Assert.Equal("/Config~0Sub~1Nested/Key", JsonPatchOperation.FromConfigKey("Config~Sub/Nested:Key"));
        }

        /// <summary>
        /// Tests that FromConfigKey throws ArgumentNullException when configKey is null.
        /// </summary>
        [Fact]
        public void FromConfigKey_NullConfigKey_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => JsonPatchOperation.FromConfigKey(null!));
            Assert.Equal("configKey", exception.ParamName);
        }

        /// <summary>
        /// Tests that FromConfigKey throws ArgumentException when configKey is empty or whitespace.
        /// </summary>
        [Fact]
        public void FromConfigKey_EmptyOrWhitespaceConfigKey_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            var exception1 = Assert.Throws<ArgumentException>(() => JsonPatchOperation.FromConfigKey(""));
            Assert.Contains("empty or whitespace", exception1.Message);

            var exception2 = Assert.Throws<ArgumentException>(() => JsonPatchOperation.FromConfigKey("   "));
            Assert.Contains("empty or whitespace", exception2.Message);
        }

        /// <summary>
        /// Tests round-trip serialization of JsonPatchOperation preserves all fields.
        /// </summary>
        [Fact]
        public void JsonPatchOperation_RoundTrip_PreservesAllFields()
        {
            // Arrange - create a complex operation with all fields
            var original = new JsonPatchOperation
            {
                Op = "replace",
                Path = "/Complex/Path/With~Special/Chars",
                Value = "{\"nested\":\"value\",\"array\":[1,2,3]}",
                From = "/Another/Path"
            };

            // Act - serialize and deserialize
            var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<JsonPatchOperation>(json);

            // Assert - all fields should be preserved
            Assert.NotNull(deserialized);
            Assert.Equal(original.Op, deserialized.Op);
            Assert.Equal(original.Path, deserialized.Path);
            Assert.Equal(original.Value, deserialized.Value);
            Assert.Equal(original.From, deserialized.From);

            // Verify JSON is valid and contains all expected fields
            Assert.Contains("\"op\"", json);
            Assert.Contains("\"path\"", json);
            Assert.Contains("\"value\"", json);
            Assert.Contains("\"from\"", json);
        }

        /// <summary>
        /// Tests that JsonPatchOperation with null Value serializes correctly without 'value' field.
        /// </summary>
        [Fact]
        public void JsonPatchOperation_NullValue_SerializesWithoutValueField()
        {
            // Arrange
            var operation = new JsonPatchOperation
            {
                Op = "remove",
                Path = "/Some/Path",
                Value = null
            };

            // Act
            var json = JsonSerializer.Serialize(operation, new JsonSerializerOptions { WriteIndented = false });

            // Assert - 'value' field should not appear in JSON when null
            Assert.DoesNotContain("\"value\"", json);
            Assert.Contains("\"op\":\"remove\"", json);
            Assert.Contains("\"path\":\"/Some/Path\"", json);
        }
    }
}
