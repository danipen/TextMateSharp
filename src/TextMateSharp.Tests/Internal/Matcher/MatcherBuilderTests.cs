using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TextMateSharp.Internal.Matcher;

namespace TextMateSharp.Tests.Internal.Matcher
{
    [TestFixture]
    public class MatcherBuilderTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_EmptyExpression_CreatesEmptyResults()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("", matchesName.Object);

            // assert
            Assert.IsNotNull(builder.Results);
            Assert.AreEqual(0, builder.Results.Count);
        }

        [Test]
        public void Constructor_NullExpression_ThrowsArgumentNullException()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act & assert
            Assert.Throws<System.ArgumentNullException>(() => new MatcherBuilder<string>(null, matchesName.Object));
        }

        [Test]
        public void Constructor_NullMatchesName_ThrowsArgumentNullException()
        {
            // act & assert
            Assert.Throws<System.ArgumentNullException>(() => new MatcherBuilder<string>("identifier", null));
        }

        [Test]
        public void Constructor_SingleIdentifier_CreatesOneMatcher()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("identifier");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("identifier", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.AreEqual(0, builder.Results[0].Priority);
        }

        [Test]
        public void Constructor_MultipleIdentifiersWithComma_CreatesMultipleMatchers()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("id1", "id2");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("id1, id2", matchesName.Object);

            // assert
            Assert.AreEqual(2, builder.Results.Count);
        }

        #endregion Constructor Tests

        #region Priority Tests

        [Test]
        public void Constructor_RightPriority_SetsPositivePriority()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("identifier");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("R: identifier", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.AreEqual(1, builder.Results[0].Priority);
        }

        [Test]
        public void Constructor_LeftPriority_SetsNegativePriority()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("identifier");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("L: identifier", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.AreEqual(-1, builder.Results[0].Priority);
        }

        [Test]
        public void Constructor_MultiplePriorities_AppliesEachCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b", "c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("R: a, L: b, c", matchesName.Object);

            // assert
            Assert.AreEqual(3, builder.Results.Count);
            Assert.AreEqual(1, builder.Results[0].Priority);  // R: a
            Assert.AreEqual(-1, builder.Results[1].Priority); // L: b
            Assert.AreEqual(0, builder.Results[2].Priority);  // c (no priority)
        }

        [Test]
        public void Constructor_InvalidPriorityPrefix_TreatsAsIdentifier()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("X:");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("X: identifier", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.AreEqual(0, builder.Results[0].Priority);
        }

        #endregion Priority Tests

        #region Conjunction Tests (AND)

        [Test]
        public void Constructor_ConjunctionOfIdentifiers_AllMustMatch()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "match")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("a") && ids.Contains("b"));
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "nomatch")))
                .Returns(false);

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a b", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("match"));
            Assert.IsFalse(builder.Results[0].Matcher("nomatch"));
        }

        [Test]
        public void Constructor_MultipleIdentifiersSpaceSeparated_CreatesConjunction()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b", "c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a b c", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsNotNull(builder.Results[0].Matcher);
        }

        #endregion Conjunction Tests

        #region Disjunction Tests (OR)

        [Test]
        public void Constructor_DisjunctionWithPipe_CreatesOrMatcher()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "a")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("a"));
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "b")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("b"));

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("(a | b)", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("a"));
            Assert.IsTrue(builder.Results[0].Matcher("b"));
        }

        [Test]
        public void Constructor_MultiplePipesIgnoresConsecutive()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("(a || b)", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void Constructor_MultipleCommasIgnoresConsecutive()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("(a ,, b)", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        #endregion Disjunction Tests

        #region Negation Tests

        [Test]
        public void Constructor_Negation_InvertsMatch()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "match")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("a"));
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "nomatch")))
                .Returns<ICollection<string>, string>((ids, _) => !ids.Contains("a"));

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("- a", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsFalse(builder.Results[0].Matcher("match"));
            Assert.IsTrue(builder.Results[0].Matcher("nomatch"));
        }

        [Test]
        public void Constructor_DoubleNegation_RestoresOriginalMatch()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "match")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("a"));

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("- - a", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("match"));
        }

        [Test]
        public void Constructor_NegationOfNull_ReturnsTrue()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("- -", matchesName.Object);

            // assert
            // The expression "- -" means: negate (negate nothing)
            // First "-" consumes and parses second "-"
            // Second "-" returns a lambda that checks null and returns false
            // First "-" negates that false result, returning true
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("anything"));
        }

        #endregion Negation Tests

        #region Parentheses Tests

        [Test]
        public void Constructor_Parentheses_GroupsExpression()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("(a)", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void Constructor_NestedParentheses_HandlesCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("((a) | (b))", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void Constructor_UnmatchedOpenParenthesis_HandlesGracefully()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("(a", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        #endregion Parentheses Tests

        #region IsIdentifier Tests

        [Test]
        public void IsIdentifier_ValidIdentifier_ReturnsTrue()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("valid.identifier");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("valid.identifier", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void IsIdentifier_WithDots_ReturnsTrue()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a.b.c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a.b.c", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void IsIdentifier_WithColons_ReturnsTrue()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a:b:c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a:b:c", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void IsIdentifier_WithUnderscores_ReturnsTrue()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a_b_c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a_b_c", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void IsIdentifier_WithNumbers_ReturnsTrue()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("abc123");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("abc123", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void Tokenizer_HyphenWithoutSpaces_TokenizedButRejected()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b");

            // act
            // "a-b" (no spaces) is tokenized by the regex as a single token "a-b"
            // However, IsIdentifier("a-b") returns false because hyphen is not an allowed character
            // So ParseOperand returns null and nothing is added to Results
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a-b", matchesName.Object);

            // assert
            Assert.AreEqual(0, builder.Results.Count);
        }

        #endregion IsIdentifier Tests

        #region Complex Expression Tests

        [Test]
        public void Constructor_ComplexExpression_ParsesCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b", "c", "d");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("R: a b, L: (c | d)", matchesName.Object);

            // assert
            Assert.AreEqual(2, builder.Results.Count);
            Assert.AreEqual(1, builder.Results[0].Priority);
            Assert.AreEqual(-1, builder.Results[1].Priority);
        }

        [Test]
        public void Constructor_ConjunctionAndDisjunction_ParsesCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b", "c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a b | c", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void Constructor_NegationWithParentheses_ParsesCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("- (a | b)", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        [Test]
        public void Constructor_MultipleMatchers_AllParsed()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b", "c");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a, b, c", matchesName.Object);

            // assert
            Assert.AreEqual(3, builder.Results.Count);
        }

        #endregion Complex Expression Tests

        #region Edge Cases

        [Test]
        public void Constructor_OnlyOperators_HandlesGracefully()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("| , - ()", matchesName.Object);

            // assert
            Assert.IsNotNull(builder.Results);
        }

        [Test]
        public void Constructor_OnlyParentheses_HandlesGracefully()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("()", matchesName.Object);

            // assert
            Assert.IsNotNull(builder.Results);
        }

        [Test]
        public void Constructor_WhitespaceOnly_CreatesEmptyResults()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("   ", matchesName.Object);

            // assert
            Assert.AreEqual(0, builder.Results.Count);
        }

        [Test]
        public void Constructor_HyphenAsNegationOperator_CreatesConjunction()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "match")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("a") && !ids.Contains("b"));
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.Is<string>(s => s == "nomatch")))
                .Returns<ICollection<string>, string>((ids, _) => ids.Contains("b"));

            // act
            // "a - b" with spaces is tokenized as three separate tokens: "a", "-", "b"
            // This creates: a AND (NOT b)
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a - b", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("match"));
            Assert.IsFalse(builder.Results[0].Matcher("nomatch"));
        }

        [Test]
        public void Constructor_ConsecutiveCommas_HandlesGracefully()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a", "b");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a,,,b", matchesName.Object);

            // assert
            Assert.IsTrue(builder.Results.Count >= 2);
        }

        [Test]
        public void Constructor_TrailingComma_HandlesGracefully()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("a,", matchesName.Object);

            // assert
            Assert.IsTrue(builder.Results.Count >= 1);
        }

        [Test]
        public void Constructor_LeadingComma_HandlesGracefully()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>(",a", matchesName.Object);

            // assert
            Assert.IsNotNull(builder.Results);
        }

        #endregion Edge Cases

        #region Tokenizer Tests

        [Test]
        public void Tokenizer_EmptyString_ReturnsNull()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName();

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("", matchesName.Object);

            // assert
            Assert.AreEqual(0, builder.Results.Count);
        }

        [Test]
        public void Tokenizer_PriorityTokens_ParsedCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("id");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("R: id", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.AreEqual(1, builder.Results[0].Priority);
        }

        [Test]
        public void Tokenizer_SpecialCharacters_ParsedAsTokens()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("a");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("(a)|-(a),(a)", matchesName.Object);

            // assert
            Assert.IsTrue(builder.Results.Count > 0);
        }

        [Test]
        public void Tokenizer_MixedIdentifiers_ParsedCorrectly()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = CreateMockMatchesName("abc", "def.ghi", "jkl:mno");

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("abc def.ghi jkl:mno", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
        }

        #endregion Tokenizer Tests

        #region Integration Tests

        [Test]
        public void Matcher_RealWorldExample_LanguageScope()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.IsAny<string>()))
                .Returns<ICollection<string>, string>((ids, input) =>
                {
                    if (input == "source.cs")
                    {
                        return ids.Contains("source.cs");
                    }

                    if (input == "source.js")
                    {
                        return ids.Contains("source.js");
                    }

                    return false;
                });

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("R: source.cs, L: source.js", matchesName.Object);

            // assert
            Assert.AreEqual(2, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("source.cs"));
            Assert.IsFalse(builder.Results[0].Matcher("source.js"));
            Assert.IsTrue(builder.Results[1].Matcher("source.js"));
            Assert.IsFalse(builder.Results[1].Matcher("source.cs"));
        }

        [Test]
        public void Matcher_RealWorldExample_ExcludePattern()
        {
            // arrange
            Mock<IMatchesName<string>> matchesName = new Mock<IMatchesName<string>>();
            matchesName.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.IsAny<string>()))
                .Returns<ICollection<string>, string>(static (ids, input) =>
                {
                    if (input == "match")
                    {
                        return ids.Contains("text") && !ids.Contains("comment");
                    }

                    if (input == "excluded")
                    {
                        return ids.Contains("comment");
                    }

                    return false;
                });

            // act
            MatcherBuilder<string> builder = new MatcherBuilder<string>("text - comment", matchesName.Object);

            // assert
            Assert.AreEqual(1, builder.Results.Count);
            Assert.IsTrue(builder.Results[0].Matcher("match"));
            Assert.IsFalse(builder.Results[0].Matcher("excluded"));
        }

        #endregion Integration Tests

        #region Test Helpers

        private static Mock<IMatchesName<string>> CreateMockMatchesName(params string[] matchingIdentifiers)
        {
            Mock<IMatchesName<string>> mock = new Mock<IMatchesName<string>>();
            mock.Setup(m => m.Match(It.IsAny<ICollection<string>>(), It.IsAny<string>()))
                .Returns<ICollection<string>, string>((identifiers, _) => identifiers.Any(matchingIdentifiers.Contains));
            return mock;
        }

        #endregion Test Helpers
    }
}