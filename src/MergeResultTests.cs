using System;
using System.Collections.Generic;
using Xunit;

namespace AppsettingsDiff
{
    /// <summary>
    /// Test scenarios for MergeResult and MergeResultExtensions to verify merge behavior,
    /// conflict resolution precedence, and JSON round-trip serialization.
    /// </summary>
    public class MergeResultTests
    {
        /// <summary>
        /// Tests that MergeResult with conflicting keys from two sources respects documented precedence.
        /// When both sides changed a key differently and strategy is Manual, our side should win (backward compatibility).
        /// </summary>
        [Fact]
        public void MergeResult_ConflictingKeys_ManualStrategy_PrefersOurSide()
        {
            // Arrange - base config has value "base"
            var baseConfig = new Dictionary<string, string> { { "Key1", "base" } };
            var ours = new Dictionary<string, string> { { "Key1", "ourValue" } };
            var theirs = new Dictionary<string, string> { { "Key1", "theirValue" } };

            // Act - merge with Manual strategy (default)
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs, ConflictResolutionStrategy.Manual);

            // Assert - our side should win for backward compatibility
            Assert.Single(result.Merged);
            Assert.Equal("ourValue", result.Merged["Key1"]);
            Assert.Single(result.Conflicts);
            Assert.Equal("Key1", result.Conflicts[0].Key);
            Assert.Equal("base", result.Conflicts[0].BaseValue);
            Assert.Equal("ourValue", result.Conflicts[0].OurValue);
            Assert.Equal("theirValue", result.Conflicts[0].TheirValue);
            Assert.False(result.Conflicts[0].AutoResolved);
        }

        /// <summary>
        /// Tests that MergeResult with conflicting keys and PreferOurs strategy automatically resolves to our side.
        /// </summary>
        [Fact]
        public void MergeResult_ConflictingKeys_PreferOursStrategy_AutoResolvesToOurSide()
        {
            // Arrange - base config has value "base"
            var baseConfig = new Dictionary<string, string> { { "Key1", "base" } };
            var ours = new Dictionary<string, string> { { "Key1", "ourValue" } };
            var theirs = new Dictionary<string, string> { { "Key1", "theirValue" } };

            // Act - merge with PreferOurs strategy
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs, ConflictResolutionStrategy.PreferOurs);

            // Assert - our side should win and be auto-resolved
            Assert.Single(result.Merged);
            Assert.Equal("ourValue", result.Merged["Key1"]);
            Assert.Single(result.Conflicts);
            Assert.Equal("Key1", result.Conflicts[0].Key);
            Assert.True(result.Conflicts[0].AutoResolved);
        }

        /// <summary>
        /// Tests that MergeResult with conflicting keys and PreferTheirs strategy automatically resolves to their side.
        /// </summary>
        [Fact]
        public void MergeResult_ConflictingKeys_PreferTheirsStrategy_AutoResolvesToTheirSide()
        {
            // Arrange - base config has value "base"
            var baseConfig = new Dictionary<string, string> { { "Key1", "base" } };
            var ours = new Dictionary<string, string> { { "Key1", "ourValue" } };
            var theirs = new Dictionary<string, string> { { "Key1", "theirValue" } };

            // Act - merge with PreferTheirs strategy
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs, ConflictResolutionStrategy.PreferTheirs);

            // Assert - their side should win and be auto-resolved
            Assert.Single(result.Merged);
            Assert.Equal("theirValue", result.Merged["Key1"]);
            Assert.Single(result.Conflicts);
            Assert.Equal("Key1", result.Conflicts[0].Key);
            Assert.True(result.Conflicts[0].AutoResolved);
        }

        /// <summary>
        /// Tests that MergeResult with no conflicts returns empty conflicts list.
        /// </summary>
        [Fact]
        public void MergeResult_NoConflicts_ReturnsEmptyConflictsList()
        {
            // Arrange - all three configs have same values
            var baseConfig = new Dictionary<string, string> { { "Key1", "value1" }, { "Key2", "value2" } };
            var ours = new Dictionary<string, string> { { "Key1", "value1" }, { "Key2", "value2" } };
            var theirs = new Dictionary<string, string> { { "Key1", "value1" }, { "Key2", "value2" } };

            // Act
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

            // Assert
            Assert.Equal(2, result.Merged.Count);
            Assert.Empty(result.Conflicts);
            Assert.False(result.HasConflicts);
        }

        /// <summary>
        /// Tests that MergeResult with zero inputs (empty dictionaries) returns empty result without throwing.
        /// </summary>
        [Fact]
        public void MergeResult_EmptyDictionaries_ReturnsEmptyResultWithoutThrowing()
        {
            // Arrange - all empty dictionaries
            var baseConfig = new Dictionary<string, string>();
            var ours = new Dictionary<string, string>();
            var theirs = new Dictionary<string, string>();

            // Act - should not throw
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

            // Assert
            Assert.Empty(result.Merged);
            Assert.Empty(result.Conflicts);
            Assert.False(result.HasConflicts);
        }

        /// <summary>
        /// Tests that MergeResult with only base changes (no conflicts) takes their side.
        /// </summary>
        [Fact]
        public void MergeResult_OnlyTheirSideChanged_TakesTheirSide()
        {
            // Arrange - base and ours have same value, theirs changed
            var baseConfig = new Dictionary<string, string> { { "Key1", "baseValue" } };
            var ours = new Dictionary<string, string> { { "Key1", "baseValue" } };
            var theirs = new Dictionary<string, string> { { "Key1", "theirValue" } };

            // Act
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

            // Assert - their side should win
            Assert.Single(result.Merged);
            Assert.Equal("theirValue", result.Merged["Key1"]);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// Tests that MergeResult with only our side changed (no conflicts) takes our side.
        /// </summary>
        [Fact]
        public void MergeResult_OnlyOurSideChanged_TakesOurSide()
        {
            // Arrange - base and theirs have same value, ours changed
            var baseConfig = new Dictionary<string, string> { { "Key1", "baseValue" } };
            var ours = new Dictionary<string, string> { { "Key1", "ourValue" } };
            var theirs = new Dictionary<string, string> { { "Key1", "baseValue" } };

            // Act
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

            // Assert - our side should win
            Assert.Single(result.Merged);
            Assert.Equal("ourValue", result.Merged["Key1"]);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// Tests that MergeResult with both sides making same change (no conflicts) takes that side.
        /// </summary>
        [Fact]
        public void MergeResult_BothSidesSameChange_TakesThatSide()
        {
            // Arrange - base has value, both ours and theirs changed to same value
            var baseConfig = new Dictionary<string, string> { { "Key1", "baseValue" } };
            var ours = new Dictionary<string, string> { { "Key1", "newValue" } };
            var theirs = new Dictionary<string, string> { { "Key1", "newValue" } };

            // Act
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

            // Assert - the new value should win
            Assert.Single(result.Merged);
            Assert.Equal("newValue", result.Merged["Key1"]);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// Tests that MergeResult with deletion on one side (no conflict) removes the key.
        /// </summary>
        [Fact]
        public void MergeResult_DeletionOnOneSide_RemovesKey()
        {
            // Arrange - base has value, theirs deleted it (key not present)
            var baseConfig = new Dictionary<string, string> { { "Key1", "baseValue" } };
            var ours = new Dictionary<string, string> { { "Key1", "baseValue" } };
            var theirs = new Dictionary<string, string>(); // Key1 not present

            // Act
            var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

            // Assert - key should be removed
            Assert.Empty(result.Merged);
            Assert.Empty(result.Conflicts);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetValueOrDefault returns default when key not found.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetValueOrDefault_ReturnsDefaultWhenKeyNotFound()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "ExistingKey", "value" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var value = result.GetValueOrDefault("NonExistentKey", "defaultValue");

            // Assert
            Assert.Equal("defaultValue", value);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetValueOrDefault returns merged value when key exists.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetValueOrDefault_ReturnsMergedValueWhenKeyExists()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "mergedValue" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var value = result.GetValueOrDefault("Key1", "defaultValue");

            // Assert
            Assert.Equal("mergedValue", value);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetValueOrDefault returns empty string when key not found and no default provided.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetValueOrDefault_ReturnsEmptyStringWhenKeyNotFoundAndNoDefault()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var value = result.GetValueOrDefault("NonExistentKey");

            // Assert
            Assert.Equal("", value);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.ContainsKey returns true for existing key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_ContainsKey_ReturnsTrueForExistingKey()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act & Assert
            Assert.True(result.ContainsKey("Key1"));
        }

        /// <summary>
        /// Tests that MergeResultExtensions.ContainsKey returns false for non-existent key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_ContainsKey_ReturnsFalseForNonExistentKey()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act & Assert
            Assert.False(result.ContainsKey("NonExistentKey"));
        }

        /// <summary>
        /// Tests that MergeResultExtensions.Count returns correct number of merged keys.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_Count_ReturnsCorrectCount()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" }, { "Key2", "value2" }, { "Key3", "value3" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act & Assert
            Assert.Equal(3, result.Count());
        }

        /// <summary>
        /// Tests that MergeResultExtensions.Count returns 0 for empty merge result.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_Count_ReturnsZeroForEmptyResult()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict>()
            };

            // Act & Assert
            Assert.Equal(0, result.Count());
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetKeys returns all merged keys.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetKeys_ReturnsAllKeys()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" }, { "Key2", "value2" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var keys = result.GetKeys();

            // Assert
            Assert.Equal(2, keys.Count());
            Assert.Contains("Key1", keys);
            Assert.Contains("Key2", keys);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.TryGetValue returns true for existing key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_TryGetValue_ReturnsTrueForExistingKey()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var success = result.TryGetValue("Key1", out var value);

            // Assert
            Assert.True(success);
            Assert.Equal("value1", value);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.TryGetValue returns false for non-existent key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_TryGetValue_ReturnsFalseForNonExistentKey()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var success = result.TryGetValue("NonExistentKey", out var value);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetConflicts returns all conflicts.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetConflicts_ReturnsAllConflicts()
        {
            // Arrange
            var conflict1 = new MergeConflict { Key = "Key1", BaseValue = "base1", OurValue = "our1", TheirValue = "their1" };
            var conflict2 = new MergeConflict { Key = "Key2", BaseValue = "base2", OurValue = "our2", TheirValue = "their2" };
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict> { conflict1, conflict2 }
            };

            // Act
            var conflicts = result.GetConflicts();

            // Assert
            Assert.Equal(2, conflicts.Count);
            Assert.Equal("Key1", conflicts[0].Key);
            Assert.Equal("Key2", conflicts[1].Key);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.HasConflicts returns true when conflicts exist.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_HasConflicts_ReturnsTrueWhenConflictsExist()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict> { new MergeConflict { Key = "Key1" } }
            };

            // Act & Assert
            Assert.True(result.HasConflicts());
        }

        /// <summary>
        /// Tests that MergeResultExtensions.HasConflicts returns false when no conflicts exist.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_HasConflicts_ReturnsFalseWhenNoConflicts()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act & Assert
            Assert.False(result.HasConflicts());
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetConflict returns the first conflict for a key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetConflict_ReturnsFirstConflictForKey()
        {
            // Arrange
            var conflict1 = new MergeConflict { Key = "Key1", BaseValue = "base1", OurValue = "our1", TheirValue = "their1" };
            var conflict2 = new MergeConflict { Key = "Key1", BaseValue = "base2", OurValue = "our2", TheirValue = "their2" };
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict> { conflict1, conflict2 }
            };

            // Act
            var conflict = result.GetConflict("Key1");

            // Assert
            Assert.NotNull(conflict);
            Assert.Equal("Key1", conflict.Key);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetConflict returns null when no conflict exists for key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetConflict_ReturnsNullWhenNoConflictForKey()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string> { { "Key1", "value1" } },
                Conflicts = new List<MergeConflict>()
            };

            // Act
            var conflict = result.GetConflict("NonExistentKey");

            // Assert
            Assert.Null(conflict);
        }

        /// <summary>
        /// Tests that MergeResultExtensions.GetConflictedKeys returns all keys with conflicts.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_GetConflictedKeys_ReturnsAllConflictedKeys()
        {
            // Arrange
            var conflict1 = new MergeConflict { Key = "Key1" };
            var conflict2 = new MergeConflict { Key = "Key2" };
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict> { conflict1, conflict2 }
            };

            // Act
            var conflictedKeys = result.GetConflictedKeys();

            // Assert
            Assert.Equal(2, conflictedKeys.Count);
            Assert.Contains("Key1", conflictedKeys);
            Assert.Contains("Key2", conflictedKeys);
        }

        /// <summary>
        /// Tests that MergeResultExtensions methods throw ArgumentNullException for null result.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_AllMethods_ThrowArgumentNullExceptionForNullResult()
        {
            // Arrange
            MergeResult? nullResult = null;

            // Act & Assert - all extension methods should throw ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => nullResult.GetValueOrDefault("key"));
            Assert.Throws<ArgumentNullException>(() => nullResult.GetValueOrDefaultAsInt("key"));
            Assert.Throws<ArgumentNullException>(() => nullResult.GetValueOrDefaultAsBool("key"));
            Assert.Throws<ArgumentNullException>(() => nullResult.GetValueOrDefaultAsDecimal("key"));
            Assert.Throws<ArgumentNullException>(() => nullResult.ContainsKey("key"));
            Assert.Throws<ArgumentNullException>(() => nullResult.Count());
            Assert.Throws<ArgumentNullException>(() => nullResult.GetKeys());
            Assert.Throws<ArgumentNullException>(() => nullResult.TryGetValue("key", out _));
            Assert.Throws<ArgumentNullException>(() => nullResult.GetConflicts());
            Assert.Throws<ArgumentNullException>(() => nullResult.HasConflicts());
            Assert.Throws<ArgumentNullException>(() => nullResult.GetConflict("key"));
            Assert.Throws<ArgumentNullException>(() => nullResult.GetConflictedKeys());
        }

        /// <summary>
        /// Tests that MergeResultExtensions methods throw ArgumentNullException for null key.
        /// </summary>
        [Fact]
        public void MergeResultExtensions_AllMethods_ThrowArgumentNullExceptionForNullKey()
        {
            // Arrange
            var result = new MergeResult
            {
                Merged = new Dictionary<string, string>(),
                Conflicts = new List<MergeConflict>()
            };

            // Act & Assert - methods that take key parameter should throw
            Assert.Throws<ArgumentNullException>(() => result.GetValueOrDefault(null!));
            Assert.Throws<ArgumentNullException>(() => result.GetValueOrDefaultAsInt(null!));
            Assert.Throws<ArgumentNullException>(() => result.GetValueOrDefaultAsBool(null!));
            Assert.Throws<ArgumentNullException>(() => result.GetValueOrDefaultAsDecimal(null!));
            Assert.Throws<ArgumentNullException>(() => result.ContainsKey(null!));
            Assert.Throws<ArgumentNullException>(() => result.TryGetValue(null!, out _));
            Assert.Throws<ArgumentNullException>(() => result.GetConflict(null!));
        }
    }
}
