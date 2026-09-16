using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdl.Web.Tridion.Templates.R2.Data;

namespace Sdl.Web.Tridion.Templates.Tests
{
    /// <summary>
    /// Unit tests for <see cref="DataModelBuilder"/>'s sequence prefix stripping.
    /// </summary>
    /// <remarks>
    /// Deliberately does not extend <see cref="TestClass"/>, because these tests must not require a CM Session.
    /// </remarks>
    [TestClass]
    public class StripSequencePrefixTest
    {
        /// <summary>
        /// Exposes the protected static <see cref="DataModelBuilder.StripSequencePrefix"/> to the tests.
        /// </summary>
        private sealed class SequencePrefixAccessor : DataModelBuilder
        {
            private SequencePrefixAccessor() : base(null)
            {
                // Never instantiated; only needed so the type compiles.
            }

            internal static string Strip(string title, out string sequencePrefix)
                => StripSequencePrefix(title, out sequencePrefix);
        }

        private static void AssertStrip(string title, string expectedTitle, string expectedSequencePrefix)
        {
            string sequencePrefix;
            string actualTitle = SequencePrefixAccessor.Strip(title, out sequencePrefix);

            Assert.AreEqual(expectedTitle, actualTitle, "Title for input '{0}'", title);
            Assert.AreEqual(expectedSequencePrefix, sequencePrefix, "Sequence prefix for input '{0}'", title);
        }

        [TestMethod]
        public void StripSequencePrefix_SequencePrefix_Stripped()
        {
            AssertStrip("001 My Keyword", "My Keyword", "001");
            AssertStrip("202 609", "609", "202");
            AssertStrip("001  Double Space", "Double Space", "001");
        }

        [TestMethod]
        public void StripSequencePrefix_NoSequencePrefix_Unchanged()
        {
            AssertStrip("My Keyword", "My Keyword", "");
            AssertStrip("Product", "Product", "");
            AssertStrip(string.Empty, string.Empty, "");
        }

        /// <summary>
        /// Regression test: a purely numeric title must not be mistaken for a sequence prefix.
        /// "202609" used to be truncated to "609" because the separator was optional.
        /// </summary>
        [TestMethod]
        public void StripSequencePrefix_NumericTitleWithoutSeparator_Unchanged()
        {
            AssertStrip("202609", "202609", "");
            AssertStrip("123", "123", "");
            AssertStrip("2026 Budget", "2026 Budget", "");
        }
    }
}
