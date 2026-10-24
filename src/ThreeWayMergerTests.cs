using System.Collections.Generic;
using Xunit;

namespace AppsettingsDiff;

/// <summary>
/// Tests the array-merge semantics of <see cref="ThreeWayMerger"/>: flattened array elements are merged
/// as a whole unit, so insert, delete and reorder on either side never produce misaligned per-index results.
/// </summary>
public class ThreeWayMergerTests
{
    private static Dictionary<string, string> Items(params string[] values)
    {
        var config = new Dictionary<string, string>();
        for (var i = 0; i < values.Length; i++)
            config[$"Items:{i}"] = values[i];

        return config;
    }

    /// <summary>
    /// Tests that an insert on our side alone is taken as a whole array without recording a conflict.
    /// </summary>
    [Fact]
    public void Merge_InsertOnOursOnly_TakesOursWholeWithoutConflict()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("x", "a", "b", "c");
        var theirs = Items("a", "b", "c");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal(ours, result.Merged);
        Assert.Empty(result.Conflicts);
    }

    /// <summary>
    /// Tests that a delete on their side alone is taken as a whole array without leaving the trailing index behind.
    /// </summary>
    [Fact]
    public void Merge_DeleteOnTheirsOnly_TakesTheirsWholeWithoutConflict()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("a", "b", "c");
        var theirs = Items("a", "c");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal(theirs, result.Merged);
        Assert.False(result.Merged.ContainsKey("Items:2"));
        Assert.Empty(result.Conflicts);
    }

    /// <summary>
    /// Tests that a reorder on our side alone is taken as a whole array without recording a conflict.
    /// </summary>
    [Fact]
    public void Merge_ReorderOnOursOnly_TakesOursWholeWithoutConflict()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("c", "b", "a");
        var theirs = Items("a", "b", "c");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal(ours, result.Merged);
        Assert.Empty(result.Conflicts);
    }

    /// <summary>
    /// Tests that deleting the whole array on one side while the other side leaves it untouched removes every element.
    /// </summary>
    [Fact]
    public void Merge_ArrayDeletedOnOneSideOnly_RemovesWholeArray()
    {
        var baseConfig = Items("a", "b");
        var ours = Items("a", "b");
        var theirs = new Dictionary<string, string>();

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Empty(result.Merged);
        Assert.Empty(result.Conflicts);
    }

    /// <summary>
    /// Tests that both sides inserting the identical element is not treated as a conflict.
    /// </summary>
    [Fact]
    public void Merge_IdenticalInsertOnBothSides_NoConflict()
    {
        var baseConfig = Items("a", "b");
        var ours = Items("x", "a", "b");
        var theirs = Items("x", "a", "b");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal(ours, result.Merged);
        Assert.Empty(result.Conflicts);
    }

    /// <summary>
    /// Tests that an insert on our side and an append on their side are recorded as one array-level conflict
    /// with a reason, and that the default Manual strategy keeps our side as a whole.
    /// </summary>
    [Fact]
    public void Merge_InsertOnOursAndAppendOnTheirs_RecordsArrayConflictKeepingOurs()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("x", "a", "b", "c");
        var theirs = Items("a", "b", "c", "d");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal(ours, result.Merged);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("Items", conflict.Key);
        Assert.False(conflict.AutoResolved);
        Assert.NotNull(conflict.Reason);
        Assert.Contains("merged as a whole value", conflict.Reason);
    }

    /// <summary>
    /// Tests that deletes on both sides of different elements are recorded as one array-level conflict.
    /// </summary>
    [Fact]
    public void Merge_DeleteOnBothSides_RecordsArrayConflict()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("b", "c");
        var theirs = Items("a", "b");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal(ours, result.Merged);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("Items", conflict.Key);
        Assert.Equal("Items:0=a;Items:1=b;Items:2=c", conflict.BaseValue);
        Assert.Equal("Items:0=b;Items:1=c", conflict.OurValue);
        Assert.Equal("Items:0=a;Items:1=b", conflict.TheirValue);
    }

    /// <summary>
    /// Tests that a reorder on our side and a delete on their side under PreferTheirs resolves to their whole array
    /// and marks the array conflict as auto-resolved.
    /// </summary>
    [Fact]
    public void Merge_ReorderOnOursAndDeleteOnTheirs_PreferTheirsTakesTheirsWhole()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("c", "b", "a");
        var theirs = Items("a", "c");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs, ConflictResolutionStrategy.PreferTheirs);

        Assert.Equal(theirs, result.Merged);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("Items", conflict.Key);
        Assert.True(conflict.AutoResolved);
    }

    /// <summary>
    /// Tests that an insert on our side and a reorder on their side under PreferOurs resolves to our whole array
    /// and marks the array conflict as auto-resolved.
    /// </summary>
    [Fact]
    public void Merge_InsertOnOursAndReorderOnTheirs_PreferOursTakesOursWhole()
    {
        var baseConfig = Items("a", "b", "c");
        var ours = Items("x", "a", "b", "c");
        var theirs = Items("c", "b", "a");

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs, ConflictResolutionStrategy.PreferOurs);

        Assert.Equal(ours, result.Merged);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("Items", conflict.Key);
        Assert.True(conflict.AutoResolved);
    }

    /// <summary>
    /// Tests that a scalar key outside the array is still merged key-by-key when the array is changed on one side only.
    /// </summary>
    [Fact]
    public void Merge_ScalarOutsideArray_MergedIndependentlyOfArrayChange()
    {
        var baseConfig = Items("a", "b");
        baseConfig["Name"] = "base";
        var ours = Items("x", "a", "b");
        ours["Name"] = "base";
        var theirs = Items("a", "b");
        theirs["Name"] = "theirs";

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal("x", result.Merged["Items:0"]);
        Assert.Equal("theirs", result.Merged["Name"]);
        Assert.Empty(result.Conflicts);
    }

    /// <summary>
    /// Tests that nested array elements sharing a root are grouped under that root, so concurrent changes to
    /// different nested elements are recorded as one array-level conflict rather than per-index conflicts.
    /// </summary>
    [Fact]
    public void Merge_NestedArrayChangedOnBothSides_RecordsSingleConflictOnRoot()
    {
        var baseConfig = new Dictionary<string, string>
        {
            ["Outer:0:Name"] = "a",
            ["Outer:1:Name"] = "b"
        };
        var ours = new Dictionary<string, string>
        {
            ["Outer:0:Name"] = "a2",
            ["Outer:1:Name"] = "b"
        };
        var theirs = new Dictionary<string, string>
        {
            ["Outer:0:Name"] = "a",
            ["Outer:1:Name"] = "b2"
        };

        var result = ThreeWayMerger.Merge(baseConfig, ours, theirs);

        Assert.Equal("a2", result.Merged["Outer:0:Name"]);
        Assert.Equal("b", result.Merged["Outer:1:Name"]);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("Outer", conflict.Key);
    }
}
