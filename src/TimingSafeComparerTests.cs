using System;
using Xunit;

namespace AppsettingsDiff;

public class TimingSafeComparerTests
{
    [Fact]
    public void FixedTimeEquals_WithNullStringA_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TimingSafeComparer.FixedTimeEquals(null!, "value"));
    }

    [Fact]
    public void FixedTimeEquals_WithNullStringB_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TimingSafeComparer.FixedTimeEquals("value", null!));
    }

    [Fact]
    public void FixedTimeEquals_WithEmptyStrings_ReturnsTrue()
    {
        Assert.True(TimingSafeComparer.FixedTimeEquals("", ""));
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentLengthStrings_ReturnsFalse()
    {
        Assert.False(TimingSafeComparer.FixedTimeEquals("short", "longer"));
    }

    [Fact]
    public void FixedTimeEquals_WithIdenticalStrings_ReturnsTrue()
    {
        Assert.True(TimingSafeComparer.FixedTimeEquals("exact", "exact"));
    }

    [Fact]
    public void FixedTimeEquals_WithCaseInsensitiveComparison_ReturnsTrueForDifferentCase()
    {
        Assert.True(TimingSafeComparer.FixedTimeEquals("Value", "value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FixedTimeEquals_WithCaseSensitiveComparison_ReturnsFalseForDifferentCase()
    {
        Assert.False(TimingSafeComparer.FixedTimeEquals("Value", "value", StringComparison.Ordinal));
    }

    [Fact]
    public void FixedTimeEquals_WithNullByteArrayA_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TimingSafeComparer.FixedTimeEquals(null!, new byte[] { 1 }));
    }

    [Fact]
    public void FixedTimeEquals_WithNullByteArrayB_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TimingSafeComparer.FixedTimeEquals(new byte[] { 1 }, null!));
    }

    [Fact]
    public void FixedTimeEquals_WithEmptyByteArrays_ReturnsTrue()
    {
        Assert.True(TimingSafeComparer.FixedTimeEquals(Array.Empty<byte>(), Array.Empty<byte>()));
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentLengthByteArrays_ReturnsFalse()
    {
        Assert.False(TimingSafeComparer.FixedTimeEquals(new byte[] { 1 }, new byte[] { 1, 2 }));
    }

    [Fact]
    public void FixedTimeEquals_WithIdenticalByteArrays_ReturnsTrue()
    {
        var bytes = new byte[] { 1, 2, 3 };
        Assert.True(TimingSafeComparer.FixedTimeEquals(bytes, bytes));
    }

    [Fact]
    public void FixedTimeEquals_WithDifferentLengthSpans_ReturnsFalse()
    {
        var a = new byte[] { 1, 2 };
        var b = new byte[] { 1, 2, 3 };
        Assert.False(TimingSafeComparer.FixedTimeEquals(a.AsSpan(), b.AsSpan()));
    }

    [Fact]
    public void FixedTimeEquals_WithIdenticalSpans_ReturnsTrue()
    {
        var bytes = new byte[] { 1, 2, 3 };
        Assert.True(TimingSafeComparer.FixedTimeEquals(bytes.AsSpan(), bytes.AsSpan()));
    }
}
