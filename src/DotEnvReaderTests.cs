using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for DotEnvReader to improve branch and edge-case coverage.
    /// Tests parsing behavior for edge cases, malformed lines, and special characters.
    /// </summary>
    public class DotEnvReaderTests
    {
        /// <summary>
        /// Tests that ReadFile throws ArgumentException when path is null.
        /// </summary>
        [Fact]
        public void ReadFile_NullPath_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => DotEnvReader.ReadFile(null!));
            Assert.Equal("Path cannot be null or empty. (Parameter 'path')", exception.Message);
        }

        /// <summary>
        /// Tests that ReadFile throws ArgumentException when path is empty.
        /// </summary>
        [Fact]
        public void ReadFile_EmptyPath_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => DotEnvReader.ReadFile(""));
            Assert.Equal("Path cannot be null or empty. (Parameter 'path')", exception.Message);
        }

        /// <summary>
        /// Tests that ReadFile throws ArgumentException when path is whitespace.
        /// </summary>
        [Fact]
        public void ReadFile_WhitespacePath_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => DotEnvReader.ReadFile("   "));
            Assert.Equal("Path cannot be null or empty. (Parameter 'path')", exception.Message);
        }

        /// <summary>
        /// Tests that ReadFile throws FileNotFoundException when file does not exist.
        /// </summary>
        [Fact]
        public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = "/tmp/nonexistent_env_file_" + Guid.NewGuid().ToString() + ".env";

            // Act & Assert
            var exception = Assert.Throws<FileNotFoundException>(() => DotEnvReader.ReadFile(nonExistentPath));
            Assert.Contains("The .env file was not found", exception.Message);
        }

        /// <summary>
        /// Tests that comment lines (starting with #) are skipped.
        /// </summary>
        [Fact]
        public void ReadFile_CommentLinesStartingWithHash_AreSkipped()
        {
            // Arrange
            var envContent = "# This is a comment\n" +
                            "# Another comment\n" +
                            "KEY1=value1\n" +
                            "# Comment after a key\n" +
                            "KEY2=value2\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that comment lines (starting with ;) are skipped.
        /// </summary>
        [Fact]
        public void ReadFile_CommentLinesStartingWithSemicolon_AreSkipped()
        {
            // Arrange
            var envContent = "; This is a comment\n" +
                            "; Another comment\n" +
                            "KEY1=value1\n" +
                            "; Comment after a key\n" +
                            "KEY2=value2\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that blank lines are skipped.
        /// </summary>
        [Fact]
        public void ReadFile_BlankLines_AreSkipped()
        {
            // Arrange
            var envContent = "\n" +
                            "\n" +
                            "KEY1=value1\n" +
                            "\n" +
                            "\n" +
                            "KEY2=value2\n" +
                            "\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that lines without '=' are ignored (malformed lines).
        /// </summary>
        [Fact]
        public void ReadFile_LinesWithoutEqualsSign_AreIgnored()
        {
            // Arrange
            var envContent = "KEY1=value1\n" +
                            "KEY2\n" +
                            "KEY3=value3\n" +
                            "malformed\n" +
                            "KEY4=value4\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(3, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value3", result["KEY3"]);
                Assert.Equal("value4", result["KEY4"]);
                Assert.DoesNotContain(result, kvp => kvp.Key == "KEY2");
                Assert.DoesNotContain(result, kvp => kvp.Key == "malformed");
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that lines with '=' at the beginning are ignored (no key).
        /// </summary>
        [Fact]
        public void ReadFile_LineStartingWithEqualsSign_Ignored()
        {
            // Arrange
            var envContent = "KEY1=value1\n" +
                            "=value2\n" +
                            "KEY3=value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value3", result["KEY3"]);
                Assert.DoesNotContain(result, kvp => kvp.Key == "");
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that keys with leading/trailing whitespace are trimmed.
        /// </summary>
        [Fact]
        public void ReadFile_KeysWithWhitespace_AreTrimmed()
        {
            // Arrange
            var envContent = "  KEY1  =value1\n" +
                            "KEY2  =  value2\n" +
                            "  KEY3  =  value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(3, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
                Assert.Equal("value3", result["KEY3"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that values with leading/trailing whitespace are trimmed.
        /// </summary>
        [Fact]
        public void ReadFile_ValuesWithWhitespace_AreTrimmed()
        {
            // Arrange
            var envContent = "KEY1=  value1\n" +
                            "KEY2=  value2\n" +
                            "KEY3=value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(3, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
                Assert.Equal("value3", result["KEY3"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that double-quoted values are parsed correctly.
        /// </summary>
        [Fact]
        public void ReadFile_DoubleQuotedValues_AreParsedCorrectly()
        {
            // Arrange
            var envContent = "KEY1=\"value with spaces\"\n" +
                            "KEY2=\"value with equals=sign\"\n" +
                            "KEY3=\"value with # comment\"\n" +
                            "KEY4=\"value with 'single quotes' inside\"\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(4, result.Count);
                Assert.Equal("value with spaces", result["KEY1"]);
                Assert.Equal("value with equals=sign", result["KEY2"]);
                Assert.Equal("value with # comment", result["KEY3"]);
                Assert.Equal("value with 'single quotes' inside", result["KEY4"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that single-quoted values are parsed correctly.
        /// </summary>
        [Fact]
        public void ReadFile_SingleQuotedValues_AreParsedCorrectly()
        {
            // Arrange
            var envContent = "KEY1='value with spaces'\n" +
                            "KEY2='value with equals=sign'\n" +
                            "KEY3='value with # comment'\n" +
                            "KEY4='value with \"double quotes\" inside'\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(4, result.Count);
                Assert.Equal("value with spaces", result["KEY1"]);
                Assert.Equal("value with equals=sign", result["KEY2"]);
                Assert.Equal("value with # comment", result["KEY3"]);
                Assert.Equal("value with \"double quotes\" inside", result["KEY4"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that duplicate keys follow last-wins behavior.
        /// </summary>
        [Fact]
        public void ReadFile_DuplicateKeys_LastWins()
        {
            // Arrange
            var envContent = "KEY1=first\n" +
                            "KEY1=second\n" +
                            "KEY1=third\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert - Last value should win
                Assert.Single(result);
                Assert.Equal("third", result["KEY1"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that keys are case-insensitive (ordinal ignore case).
        /// </summary>
        [Fact]
        public void ReadFile_CaseInsensitiveKeys()
        {
            // Arrange
            var envContent = "key1=value1\n" +
                            "KEY1=value2\n" +
                            "Key1=value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert - Should be case-insensitive, last one wins
                Assert.Single(result);
                Assert.Equal("value3", result["key1"]);
                Assert.Equal("value3", result["KEY1"]);
                Assert.Equal("value3", result["Key1"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that export prefix is removed correctly.
        /// </summary>
        [Fact]
        public void ReadFile_ExportPrefix_IsRemoved()
        {
            // Arrange
            var envContent = "export KEY1=value1\n" +
                            "export KEY2=value2\n" +
                            "KEY3=value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(3, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
                Assert.Equal("value3", result["KEY3"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that export prefix with mixed case is removed correctly.
        /// </summary>
        [Fact]
        public void ReadFile_ExportPrefixMixedCase_IsRemoved()
        {
            // Arrange
            var envContent = "EXPORT KEY1=value1\n" +
                            "exPort KEY2=value2\n" +
                            "ExPort KEY3=value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(3, result.Count);
                Assert.Equal("value1", result["KEY1"]);
                Assert.Equal("value2", result["KEY2"]);
                Assert.Equal("value3", result["KEY3"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that empty values are preserved.
        /// </summary>
        [Fact]
        public void ReadFile_EmptyValues_ArePreserved()
        {
            // Arrange
            var envContent = "KEY1=\n" +
                            "KEY2=\n" +
                            "KEY3=value3\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(3, result.Count);
                Assert.Equal("", result["KEY1"]);
                Assert.Equal("", result["KEY2"]);
                Assert.Equal("value3", result["KEY3"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that values with special characters are preserved.
        /// </summary>
        [Fact]
        public void ReadFile_ValuesWithSpecialCharacters_ArePreserved()
        {
            // Arrange
            var envContent = "KEY1=value with spaces\n" +
                            "KEY2=value=with=equals\n" +
                            "KEY3=value#with#hash\n" +
                            "KEY4=value;with;semicolon\n" +
                            "KEY5=value\twith\ttabs\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(5, result.Count);
                Assert.Equal("value with spaces", result["KEY1"]);
                Assert.Equal("value=with=equals", result["KEY2"]);
                Assert.Equal("value#with#hash", result["KEY3"]);
                Assert.Equal("value;with;semicolon", result["KEY4"]);
                Assert.Equal("value\twith\ttabs", result["KEY5"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that quoted values with embedded equals signs are parsed correctly (not truncated at =).
        /// This is a regression test for the issue where '#' in quoted values would truncate the value.
        /// </summary>
        [Fact]
        public void ReadFile_QuotedValuesWithEmbeddedEquals_NotTruncated()
        {
            // Arrange
            var envContent = "KEY1=\"value=with=equals\"\n" +
                            "KEY2='value=with=equals'\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value=with=equals", result["KEY1"]);
                Assert.Equal("value=with=equals", result["KEY2"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that quoted values with embedded hash characters are parsed correctly (not truncated at #).
        /// This is a regression test for the issue where '#' in quoted values would truncate the value.
        /// </summary>
        [Fact]
        public void ReadFile_QuotedValuesWithEmbeddedHash_NotTruncated()
        {
            // Arrange
            var envContent = "KEY1=\"value # with hash\"\n" +
                            "KEY2='value # with hash'\n" +
                            "KEY3=\"# starts with hash\"\n" +
                            "KEY4=\"value ends with hash #\"\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(4, result.Count);
                Assert.Equal("value # with hash", result["KEY1"]);
                Assert.Equal("value # with hash", result["KEY2"]);
                Assert.Equal("# starts with hash", result["KEY3"]);
                Assert.Equal("value ends with hash #", result["KEY4"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests that values with trailing backslash are preserved.
        /// </summary>
        [Fact]
        public void ReadFile_ValuesWithTrailingBackslash_ArePreserved()
        {
            // Arrange
            var envContent = "KEY1=value\\\n" +
                            "KEY2=value\\\\\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(2, result.Count);
                Assert.Equal("value\\", result["KEY1"]);
                Assert.Equal("value\\\\", result["KEY2"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }

        /// <summary>
        /// Tests a complete realistic .env file with mixed content.
        /// </summary>
        [Fact]
        public void ReadFile_CompleteRealisticEnvFile_ParsesCorrectly()
        {
            // Arrange
            var envContent = "# Database configuration\n" +
                            "DATABASE_URL=postgres://localhost:5432/mydb\n" +
                            "DATABASE_USER=admin\n" +
                            "DATABASE_PASSWORD=secret123\n" +
                            "\n" +
                            "# Redis configuration\n" +
                            "REDIS_HOST=localhost\n" +
                            "REDIS_PORT=6379\n" +
                            "\n" +
                            "# Application settings\n" +
                            "APPLICATION_NAME=My App\n" +
                            "APPLICATION_VERSION=1.0.0\n" +
                            "\n" +
                            "# Feature flags\n" +
                            "enable_feature_x=true\n" +
                            "enable_feature_y=false\n" +
                            "\n" +
                            "# Comments can appear anywhere\n" +
                            "# KEY=should_not_be_parsed\n" +
                            "\n" +
                            "# Empty lines are fine too\n" +
                            "\n" +
                            "export API_KEY=secret_api_key_123\n" +
                            "export SECRET_KEY=secret_key_456\n";
            var envPath = Path.GetTempFileName();
            File.WriteAllText(envPath, envContent);

            try
            {
                // Act
                var result = DotEnvReader.ReadFile(envPath);

                // Assert
                Assert.Equal(11, result.Count);
                Assert.Equal("postgres://localhost:5432/mydb", result["DATABASE_URL"]);
                Assert.Equal("admin", result["DATABASE_USER"]);
                Assert.Equal("secret123", result["DATABASE_PASSWORD"]);
                Assert.Equal("localhost", result["REDIS_HOST"]);
                Assert.Equal("6379", result["REDIS_PORT"]);
                Assert.Equal("My App", result["APPLICATION_NAME"]);
                Assert.Equal("1.0.0", result["APPLICATION_VERSION"]);
                Assert.Equal("true", result["enable_feature_x"]);
                Assert.Equal("false", result["enable_feature_y"]);
                Assert.Equal("secret_api_key_123", result["API_KEY"]);
                Assert.Equal("secret_key_456", result["SECRET_KEY"]);
            }
            finally
            {
                File.Delete(envPath);
            }
        }
    }
}
