using NUnit.Framework;
using System;
using System.Collections.Generic;
using TextMateSharp.Internal.Matcher;

namespace TextMateSharp.Tests.Internal.Matcher
{
    [TestFixture]
    public class NameMatcherTests
    {
        private NameMatcher _matcher;

        [SetUp]
        public void SetUp()
        {
            _matcher = new NameMatcher();
        }

        #region Default Instance Tests

        [Test]
        public void Default_Should_ReturnSingletonInstance()
        {
            // arrange & act
            var instance1 = NameMatcher.Default;
            var instance2 = NameMatcher.Default;

            // assert
            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2);
        }

        #endregion

        #region Null/Empty Tests

        [Test]
        public void Match_NullIdentifiers_ThrowsArgumentNullException()
        {
            // arrange
            List<string> scopes = new List<string> { "source.cs" };

            // act & assert
            Assert.Throws<ArgumentNullException>(() => _matcher.Match(null, scopes));
        }

        [Test]
        public void Match_NullScopes_ThrowsArgumentNullException()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source" };

            // act & assert
            Assert.Throws<ArgumentNullException>(() => _matcher.Match(identifiers, null));
        }

        [Test]
        public void Match_EmptyIdentifiers_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string>();
            List<string> scopes = new List<string> { "source.cs" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_EmptyScopes_WithEmptyIdentifiers_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string>();
            List<string> scopes = new List<string>();

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_EmptyScopes_WithNonEmptyIdentifiers_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source" };
            List<string> scopes = new List<string>();

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Exact Match Tests

        [Test]
        public void Match_SingleIdentifier_ExactMatch_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs" };
            List<string> scopes = new List<string> { "source.cs" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_MultipleIdentifiers_AllExactMatch_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs", "meta.class" };
            List<string> scopes = new List<string> { "source.cs", "meta.class" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_SingleIdentifier_NoMatch_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs" };
            List<string> scopes = new List<string> { "source.java" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Prefix Match Tests

        [Test]
        public void Match_PrefixMatch_WithDot_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source" };
            List<string> scopes = new List<string> { "source.cs" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_PrefixMatch_MultipleSegments_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "meta.class" };
            List<string> scopes = new List<string> { "meta.class.body.cs" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_PrefixMatch_WithoutDot_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source" };
            List<string> scopes = new List<string> { "sourcecontrol" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Match_PartialPrefix_NoDot_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "sour" };
            List<string> scopes = new List<string> { "source.cs" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Sequential Matching Tests

        [Test]
        public void Match_SequentialIdentifiers_InOrder_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source", "meta" };
            List<string> scopes = new List<string> { "source.cs", "meta.class" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_SequentialIdentifiers_WithGap_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source", "meta.class" };
            List<string> scopes = new List<string> { "source.cs", "region.cs", "meta.class.body" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_SequentialIdentifiers_OutOfOrder_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "meta", "source" };
            List<string> scopes = new List<string> { "source.cs", "meta.class" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Match_ThreeIdentifiers_InOrder_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs", "meta.class", "entity.name" };
            List<string> scopes = new List<string> { "source.cs", "meta.class.body", "entity.name.type" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_IdentifierNotFound_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source", "keyword" };
            List<string> scopes = new List<string> { "source.cs", "meta.class" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Count Comparison Tests

        [Test]
        public void Match_MoreIdentifiersThanScopes_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source", "meta", "entity" };
            List<string> scopes = new List<string> { "source.cs", "meta.class" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Match_FewerIdentifiersThanScopes_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source" };
            List<string> scopes = new List<string> { "source.cs", "meta.class", "entity.name" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Null Scope Tests

        [Test]
        public void Match_NullScopeInList_SkipsAndContinues()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "meta" };
            List<string> scopes = new List<string> { "source.cs", null, "meta.class" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_AllNullScopes_ReturnsFalse()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source" };
            List<string> scopes = new List<string> { null, null };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Complex Real-World Scenarios

        [Test]
        public void Match_CSharpClassDefinition_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs", "meta.class", "entity.name.type" };
            List<string> scopes = new List<string>
            {
                "source.cs",
                "meta.class.cs",
                "entity.name.type.class.cs"
            };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_JavaScriptFunctionCall_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.js", "meta.function-call" };
            List<string> scopes = new List<string>
            {
                "source.js",
                "meta.function-call.js",
                "entity.name.function"
            };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_HTMLWithEmbeddedCSS_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "text.html", "source.css" };
            List<string> scopes = new List<string>
            {
                "text.html.basic",
                "source.css.embedded.html",
                "meta.property-list.css"
            };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_DeepNestedScopes_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source", "meta.block", "meta.function" };
            List<string> scopes = new List<string>
            {
                "source.cs",
                "meta.block.cs",
                "meta.method.cs",
                "meta.function.body.cs",
                "keyword.control.cs"
            };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Match_IdentifierMatchesLastScope_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "entity.name" };
            List<string> scopes = new List<string> { "source.cs", "meta.class", "entity.name.type" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_IdentifierMatchesFirstScope_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs" };
            List<string> scopes = new List<string> { "source.cs", "meta.class", "entity.name.type" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_DuplicateIdentifiers_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "meta", "meta" };
            List<string> scopes = new List<string> { "source.cs", "meta.class", "meta.method" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_DuplicateScopes_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source", "source" };
            List<string> scopes = new List<string> { "source.cs", "source.cs" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_SingleCharacterIdentifier_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "a" };
            List<string> scopes = new List<string> { "a.b.c" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Match_VeryLongScopeName_ReturnsTrue()
        {
            // arrange
            ICollection<string> identifiers = new List<string> { "source.cs.embedded.html.css.js" };
            List<string> scopes = new List<string> { "source.cs.embedded.html.css.js.meta.function" };

            // act
            bool result = _matcher.Match(identifiers, scopes);

            // assert
            Assert.IsTrue(result);
        }

        #endregion
    }
}