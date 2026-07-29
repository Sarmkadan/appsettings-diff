using System;
using Xunit;
using AppsettingsDiff;

namespace AppsettingsDiff.Tests
{
    public class KeyPathExtensionsTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("Root", null)]
        [InlineData("A.B", "A")]
        [InlineData("A.B.C", "A.B")]
        public void GetParentPath_ReturnsExpected(string? input, string? expected)
        {
            var result = input.GetParentPath();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("Root", "Root")]
        [InlineData("A.B", "B")]
        [InlineData("A.B.C", "C")]
        public void GetLeafKey_ReturnsExpected(string? input, string? expected)
        {
            var result = input.GetLeafKey();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, new string[0])]
        [InlineData("", new string[0])]
        [InlineData("Root", new[] { "Root" })]
        [InlineData("A.B.C", new[] { "A", "B", "C" })]
        public void GetSegments_ReturnsExpected(string? input, string[] expected)
        {
            var result = input.GetSegments();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("", "", false)]
        [InlineData("A.B", null, false)]
        [InlineData(null, "A", false)]
        [InlineData("A.B", "A", true)]
        [InlineData("A.B.C", "A.B", true)]
        [InlineData("A.B", "A.B", false)] // same path is not a child
        [InlineData("A.B", "A.B.C", false)] // parent longer than child
        public void IsChildOf_ReturnsExpected(string? path, string? parent, bool expected)
        {
            var result = path.IsChildOf(parent);
            Assert.Equal(expected, result);
        }
    }
}
