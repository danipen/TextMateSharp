using NUnit.Framework;

using System.Collections.Generic;

using TextMateSharp.Internal.Utils;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Themes
{
    [TestFixture]
    internal class StrCmpTests
    {
        [Test]
        public void Str_Arr_Cmp_Should_Work()
        {
            AssertStrArrCmp(null, null, 0);
            AssertStrArrCmp(null, new List<string>(), -1);
            AssertStrArrCmp(null, new List<string>() { "a" }, -1);
            AssertStrArrCmp(new List<string>(), null, 1);
            AssertStrArrCmp(new List<string>() { "a" }, null, 1);
            AssertStrArrCmp(new List<string>(), new List<string>(), 0);
            AssertStrArrCmp(new List<string>(), new List<string>() { "a" }, -1);
            AssertStrArrCmp(new List<string>() { "a" }, new List<string>(), 1);
            AssertStrArrCmp(new List<string>() { "a" }, new List<string>() { "a" }, 0);
            AssertStrArrCmp(new List<string>() { "a", "b" }, new List<string>() { "a" }, 1);
            AssertStrArrCmp(new List<string>() { "a" }, new List<string>() { "a", "b" }, -1);
            AssertStrArrCmp(new List<string>() { "a", "b" }, new List<string>() { "a", "b" }, 0);
            AssertStrArrCmp(new List<string>() { "a", "b" }, new List<string>() { "a", "c" }, -1);
            AssertStrArrCmp(new List<string>() { "a", "c" }, new List<string>() { "a", "b" }, 1);
        }

        static void AssertStrArrCmp(List<string> a, List<string> b, int expected)
        {
            Assert.AreEqual(expected, StringUtils.StrArrCmp(a, b));
        }
    }
}
