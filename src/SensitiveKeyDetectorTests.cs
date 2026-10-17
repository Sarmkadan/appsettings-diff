using System;
using System.Linq;
using Xunit;

namespace SensitiveKeyDetection
{
    public class SensitiveKeyDetectorTests
    {
        private static SensitiveKeyDetectorOptions OptionsWith(params string?[] patterns)
            => new SensitiveKeyDetectorOptions(patterns!, Array.Empty<string>(), caseSensitive: false);

        [Fact]
        public void Constructor_NullPatternList_ThrowsArgumentException()
        {
            var options = new SensitiveKeyDetectorOptions(null, Array.Empty<string>(), caseSensitive: false);

            var ex = Assert.Throws<ArgumentException>(() => new SensitiveKeyDetector(options));

            Assert.Contains("At least one", ex.Message);
        }

        [Fact]
        public void Constructor_EmptyPatternList_ThrowsArgumentException()
        {
            var options = OptionsWith();

            Assert.Throws<ArgumentException>(() => new SensitiveKeyDetector(options));
        }

        [Fact]
        public void Constructor_NullPattern_ThrowsArgumentExceptionNamingIndex()
        {
            var options = OptionsWith("password", null);

            var ex = Assert.Throws<ArgumentException>(() => new SensitiveKeyDetector(options));

            Assert.Contains("index 1", ex.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Constructor_WhitespacePattern_ThrowsArgumentExceptionNamingIndex(string pattern)
        {
            var options = OptionsWith(pattern);

            var ex = Assert.Throws<ArgumentException>(() => new SensitiveKeyDetector(options));

            Assert.Contains("index 0", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidRegex_ThrowsInvalidSensitivePatternExceptionWithPatternText()
        {
            const string invalid = "(unclosed";
            var options = OptionsWith(invalid);

            var ex = Assert.Throws<InvalidSensitivePatternException>(() => new SensitiveKeyDetector(options));

            Assert.Equal(invalid, ex.Pattern);
            Assert.Contains(invalid, ex.Message);
            Assert.IsType<System.Text.RegularExpressions.RegexParseException>(ex.InnerException);
        }

        [Fact]
        public void Constructor_InvalidRegexAfterValidOne_ReportsItsIndex()
        {
            const string invalid = "[abc";
            var options = OptionsWith("password", invalid);

            var ex = Assert.Throws<InvalidSensitivePatternException>(() => new SensitiveKeyDetector(options));

            Assert.Equal(1, ex.Index);
            Assert.Equal(invalid, ex.Pattern);
        }

        [Fact]
        public void IsSensitive_CustomPattern_MatchesNestedKeyLikePaymentStripeKey()
        {
            var options = OptionsWith(@"StripeKey$");
            var detector = new SensitiveKeyDetector(options);

            Assert.True(detector.IsSensitive("Payment:StripeKey"));
            Assert.False(detector.IsSensitive("Payment:Currency"));
        }

        [Fact]
        public void Builder_UseDefaultsWithCustomPattern_MatchesPaymentStripeKey()
        {
            var detector = new SensitiveKeyDetectorBuilder()
                .UseDefaults()
                .AddPattern(@"StripeKey$")
                .Build();

            Assert.True(detector.IsSensitive("Payment:StripeKey"));
            Assert.True(detector.IsSensitive("ConnectionStrings:Default"));
        }
    }
}
