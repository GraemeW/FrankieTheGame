using NUnit.Framework;

namespace LowDefMustard.UIBox.Tests.Editor
{
    public class TextScanBoxUnescapeTextTests
    {
        [TestCase(null)]
        [TestCase("")]
        public void UnescapeText_NullOrEmpty_ReturnsUnchanged(string input)
        {
            Assert.AreEqual(input, TextScanBox.UnescapeText(input));
        }

        [TestCase("a\\nb", "a\nb")]
        [TestCase("a\\tb", "a\tb")]
        [TestCase("a\\rb", "a\rb")]
        [TestCase("\\\\", "\\")]
        [TestCase("\\\"", "\"")]
        [TestCase("\\'", "'")]
        [TestCase("\\a", "\a")]
        [TestCase("\\b", "\b")]
        [TestCase("\\f", "\f")]
        [TestCase("\\v", "\v")]
        [TestCase("\\0", "\0")]
        public void UnescapeText_KnownSingleCharEscapes_ResolveCorrectly(string input, string expected)
        {
            Assert.AreEqual(expected, TextScanBox.UnescapeText(input));
        }

        [Test]
        public void UnescapeText_ValidFourDigitUnicodeEscape_ResolvesToChar()
        {
            Assert.AreEqual("A", TextScanBox.UnescapeText("\\u0041"));
        }

        [Test]
        public void UnescapeText_UnicodeEscapeWithTooFewDigits_PassesThroughLiterally()
        {
            // Only 3 hex digits follow \u -- not a valid \uXXXX escape
            Assert.AreEqual("\\u041", TextScanBox.UnescapeText("\\u041"));
        }

        [Test]
        public void UnescapeText_TwoDigitHexEscape_ResolvesToChar()
        {
            Assert.AreEqual("A", TextScanBox.UnescapeText("\\x41"));
        }

        [Test]
        public void UnescapeText_HexEscapeIsGreedyUpToFourDigits()
        {
            // \x is variable-length (1-4 hex digits) - greedy, takes all 4, not just 2
            Assert.AreEqual(((char)0x1F60).ToString(), TextScanBox.UnescapeText("\\x1F60"));
        }

        [Test]
        public void UnescapeText_UnrecognizedEscape_PassesThroughBothChars()
        {
            Assert.AreEqual("\\q", TextScanBox.UnescapeText("\\q"));
        }

        [Test]
        public void UnescapeText_TrailingLoneBackslash_PassesThroughLiterally()
        {
            Assert.AreEqual("trail\\", TextScanBox.UnescapeText("trail\\"));
        }

        [Test]
        public void UnescapeText_MixedTextAndEscapes_ResolvesOnlyTheEscapes()
        {
            Assert.AreEqual("Line one\nLine two\tend", TextScanBox.UnescapeText("Line one\\nLine two\\tend"));
        }
    }
}
