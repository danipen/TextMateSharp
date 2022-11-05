using System.Linq;
using NUnit.Framework;

using System.Collections.Generic;

using TextMateSharp.Internal.Matcher;

namespace TextMateSharp.Tests.Internal.MatcherTest
{
    [TestFixture]
    internal class MatcherTests
    {
        [TestCase("foo", new string[] { "foo" }, true)]
        [TestCase("foo", new string[] { "bar" }, false)]
        [TestCase("- foo", new string[] { "foo" }, false)]
        [TestCase("- foo", new string[] { "bar" }, true)]
        [TestCase("- - foo", new string[] { "bar" }, false)]
        [TestCase("bar foo", new string[] { "foo" }, false)]
        [TestCase("bar foo", new string[] { "bar" }, false)]
        [TestCase("bar foo", new string[] { "bar", "foo" }, true)]
        [TestCase("bar - foo", new string[] { "bar" }, true)]
        [TestCase("bar - foo", new string[] { "foo", "bar" }, false)]
        [TestCase("bar - foo", new string[] { "foo" }, false)]
        [TestCase("bar, foo", new string[] { "foo" }, true)]
        [TestCase("bar, foo", new string[] { "bar" }, true)]
        [TestCase("bar, foo", new string[] { "bar", "foo" }, true)]
        [TestCase("bar, -foo", new string[] { "bar", "foo" }, true)]
        [TestCase("bar, -foo", new string[] { "yo" }, true)]
        [TestCase("bar, -foo", new string[] { "foo" }, false)]
        [TestCase("(foo)", new string[] { "foo" }, true)]
        [TestCase("(foo - bar)", new string[] { "foo" }, true)]
        [TestCase("(foo - bar)", new string[] { "foo", "bar" }, false)]
        [TestCase("foo bar - (yo man)", new string[] { "foo", "bar" }, true)]
        [TestCase("foo bar - (yo man)", new string[] { "foo", "bar", "yo" }, true)]
        [TestCase("foo bar - (yo man)", new string[] { "foo", "bar", "yo", "man" }, false)]
        [TestCase("foo bar - (yo | man)", new string[] { "foo", "bar", "yo", "man" }, false)]
        [TestCase("foo bar - (yo | man)", new string[] { "foo", "bar", "yo" }, false)]
        public void Matcher_Should_Work(string expression, string[] input, bool expectedResult)
        {
            var matchers = Matcher.CreateMatchers(expression);
            bool actualResult = false;
            foreach (var item in matchers)
            {
                actualResult |= item.Matcher.Invoke(new List<string>(input));
            }

            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}
