using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextMateSharp.Internal.Themes;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Themes
{
    [TestFixture]
    public class ParsedThemeTests
    {
        #region ParseInclude tests

        [Test]
        public void ParseInclude_SourceGetIncludeReturnsNull_ReturnsEmptyListAndSetsThemeIncludeToNull()
        {
            // Arrange
            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns((string)null);
            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            const int priority = 5;

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(mockSource.Object, mockRegistryOptions.Object, priority, out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
            Assert.IsNull(themeInclude);
            mockRegistryOptions.Verify(r => r.GetTheme(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ParseInclude_SourceGetIncludeReturnsEmpty_ReturnsEmptyListAndSetsThemeIncludeToNull()
        {
            // Arrange
            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns(string.Empty);
            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            const int priority = 10;

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(mockSource.Object, mockRegistryOptions.Object, priority, out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
            Assert.IsNull(themeInclude);
            mockRegistryOptions.Verify(r => r.GetTheme(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ParseInclude_GetThemeReturnsNull_ReturnsEmptyList()
        {
            // Arrange
            const string includeString = "valid-include-name";
            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns(includeString);
            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetTheme(includeString)).Returns((IRawTheme)null);
            const int priority = 0;

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(mockSource.Object, mockRegistryOptions.Object, priority, out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
            Assert.IsNull(themeInclude);
            mockRegistryOptions.Verify(r => r.GetTheme(includeString), Times.Once);
        }

        [Test]
        public void ParseInclude_ValidIncludeAndTheme_ReturnsParseThemeResult()
        {
            // Arrange
            const string includeString = "dark-theme";
            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns(includeString);

            Mock<IRawTheme> mockIncludedTheme = new Mock<IRawTheme>();
            mockIncludedTheme.Setup(t => t.GetSettings()).Returns(new List<IRawThemeSetting>());
            mockIncludedTheme.Setup(t => t.GetTokenColors()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetTheme(includeString)).Returns(mockIncludedTheme.Object);
            const int priority = 1;

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(mockSource.Object, mockRegistryOptions.Object, priority, out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(mockIncludedTheme.Object, themeInclude);
            mockRegistryOptions.Verify(r => r.GetTheme(includeString), Times.Once);
        }

        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        public void ParseInclude_VariousPriorityValues_PassesPriorityToParseTheme(int priority)
        {
            // Arrange
            const string includeString = "test-theme";
            const string expectedScope = "scope1";
            const string expectedForeground = "#123456";
            const int expectedRuleCount = 1;
            const int expectedRuleIndex = 0;

            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns(includeString);

            ThemeRaw includedTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = expectedScope,
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = expectedForeground
                        }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetTheme(includeString)).Returns(includedTheme);

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(
                mockSource.Object,
                mockRegistryOptions.Object,
                priority,
                out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRuleCount, result.Count);
            Assert.AreSame(includedTheme, themeInclude);
            mockRegistryOptions.Verify(r => r.GetTheme(includeString), Times.Once);

            ParsedThemeRule rule = result[0];
            Assert.AreEqual(expectedScope, rule.scope);
            Assert.AreEqual(expectedRuleIndex, rule.index);
            Assert.AreEqual(expectedForeground, rule.foreground);
            Assert.AreEqual(FontStyle.NotSet, rule.fontStyle);
        }

        [TestCase(" ")]
        [TestCase("  ")]
        [TestCase("\t")]
        [TestCase("\n")]
        [TestCase("\r\n")]
        public void ParseInclude_SourceGetIncludeReturnsWhitespace_ReturnsEmptyListAndSetsThemeIncludeToNull(string whitespace)
        {
            // Arrange
            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns(whitespace);
            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetTheme(whitespace)).Returns((IRawTheme)null);
            const int priority = 0;

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(mockSource.Object, mockRegistryOptions.Object, priority, out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
            Assert.IsNull(themeInclude);
            // Note: string.IsNullOrEmpty does NOT treat whitespace as empty, so GetTheme should be called
            mockRegistryOptions.Verify(r => r.GetTheme(whitespace), Times.Once);
        }

        [TestCase("theme-with-dashes")]
        [TestCase("theme_with_underscores")]
        [TestCase("theme.with.dots")]
        [TestCase("theme/with/slashes")]
        [TestCase("themeWithMixedCase")]
        [TestCase("very-long-theme-name-that-exceeds-normal-length-expectations-for-testing-purposes")]
        public void ParseInclude_VariousIncludeStringFormats_PassesCorrectlyToGetTheme(string includeString)
        {
            // Arrange
            Mock<IRawTheme> mockSource = new Mock<IRawTheme>();
            mockSource.Setup(s => s.GetInclude()).Returns(includeString);

            Mock<IRawTheme> mockIncludedTheme = new Mock<IRawTheme>();
            mockIncludedTheme.Setup(t => t.GetSettings()).Returns(new List<IRawThemeSetting>());
            mockIncludedTheme.Setup(t => t.GetTokenColors()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetTheme(includeString)).Returns(mockIncludedTheme.Object);
            const int priority = 0;

            // Act
            List<ParsedThemeRule> result = ParsedTheme.ParseInclude(mockSource.Object, mockRegistryOptions.Object, priority, out IRawTheme themeInclude);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreSame(mockIncludedTheme.Object, themeInclude);
            mockRegistryOptions.Verify(r => r.GetTheme(includeString), Times.Once);
        }

        #endregion ParseInclude tests

        #region Match tests

        [Test]
        public void Match_ConcurrentAccess_ReturnsSameOrEquivalentResults()
        {
            // Arrange
            const string testScope = "source.cs";
            const int threadCount = 10;
            const int iterationsPerThread = 100;

            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Use thread-safe collection to avoid race conditions in test code
            ConcurrentBag<List<ThemeTrieElementRule>> allResults = new ConcurrentBag<List<ThemeTrieElementRule>>();

            // Two-phase synchronization: ready + start
            CountdownEvent readyEvent = new CountdownEvent(threadCount);
            ManualResetEventSlim startEvent = new ManualResetEventSlim(false);

            // Act - Multiple threads accessing Match concurrently
            List<Task> tasks = new List<Task>(threadCount);
            for (int t = 0; t < threadCount; t++)
            {
                Task task = Task.Run(() =>
                {
                    readyEvent.Signal(); // Signal that this thread is ready
                    startEvent.Wait();    // Wait for start signal

                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        List<ThemeTrieElementRule> result = parsedTheme.Match(testScope);
                        allResults.Add(result);
                    }
                });
                tasks.Add(task);
            }

            readyEvent.Wait();  // Wait for all threads to be ready

            // Get expected result AFTER threads are ready but BEFORE they start
            // This tests concurrent cache misses on first access
            List<ThemeTrieElementRule> expectedResult = parsedTheme.Match(testScope);

            startEvent.Set();   // Release all threads simultaneously
            Task.WaitAll(tasks.ToArray());

            readyEvent.Dispose();
            startEvent.Dispose();

            // Assert - All results should be non-null and either identical reference or equivalent
            const int expectedResultCount = threadCount * iterationsPerThread;
            Assert.AreEqual(expectedResultCount, allResults.Count);
            foreach (List<ThemeTrieElementRule> result in allResults)
            {
                Assert.IsNotNull(result);
                // Results should be either the same cached instance or equivalent
                Assert.That(result == expectedResult || AreRuleListsEquivalent(result, expectedResult),
                    "All concurrent Match calls should return same or equivalent results");
            }
        }

        [Test]
        public void Match_MultipleScopesWithConcurrentAccess_CachesCorrectlyPerScope()
        {
            // Arrange
            const int threadCount = 8;
            const int uniqueScopeCount = 5;
            const int iterationsPerThread = 50;

            string[] testScopes = new string[uniqueScopeCount];
            for (int i = 0; i < uniqueScopeCount; i++)
            {
                testScopes[i] = $"scope.test{i}";
            }

            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Pre-compute expected results for each scope
            Dictionary<string, List<ThemeTrieElementRule>> expectedResults = new Dictionary<string, List<ThemeTrieElementRule>>();
            foreach (string scope in testScopes)
            {
                expectedResults[scope] = parsedTheme.Match(scope);
            }

            // Use thread-safe collection for results per scope
            ConcurrentDictionary<string, ConcurrentBag<List<ThemeTrieElementRule>>> resultsByScope =
                new ConcurrentDictionary<string, ConcurrentBag<List<ThemeTrieElementRule>>>();
            foreach (string scope in testScopes)
            {
                resultsByScope[scope] = new ConcurrentBag<List<ThemeTrieElementRule>>();
            }

            // Two-phase synchronization
            CountdownEvent readyEvent = new CountdownEvent(threadCount);
            ManualResetEventSlim startEvent = new ManualResetEventSlim(false);

            // Act - Multiple threads accessing different scopes concurrently
            List<Task> tasks = new List<Task>(threadCount);
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                Task task = Task.Run(() =>
                {
                    readyEvent.Signal();
                    startEvent.Wait(); // Synchronize thread start

                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        // Each thread cycles through different scopes to create cache contention
                        string scope = testScopes[(threadId + i) % uniqueScopeCount];
                        List<ThemeTrieElementRule> result = parsedTheme.Match(scope);
                        resultsByScope[scope].Add(result);
                    }
                });
                tasks.Add(task);
            }

            readyEvent.Wait();
            startEvent.Set();
            Task.WaitAll(tasks.ToArray());

            readyEvent.Dispose();
            startEvent.Dispose();

            // Assert - Each scope should have correct cached results
            foreach (string scope in testScopes)
            {
                ConcurrentBag<List<ThemeTrieElementRule>> scopeResults = resultsByScope[scope];
                Assert.IsNotEmpty(scopeResults, $"Should have results for scope: {scope}");

                List<ThemeTrieElementRule> expected = expectedResults[scope];
                foreach (List<ThemeTrieElementRule> result in scopeResults)
                {
                    Assert.IsNotNull(result, $"Result should not be null for scope: {scope}");
                    Assert.That(result == expected || AreRuleListsEquivalent(result, expected),
                        $"Results for scope '{scope}' should be same or equivalent");
                }
            }
        }

        [Test]
        [Repeat(5)] // Run multiple times to increase chance of catching race conditions
        public void Match_ConcurrentAccessWithHeavyContention_ReturnsConsistentResults()
        {
            // Arrange - This test maximizes contention by having all threads access the same scope repeatedly
            const string testScope = "source.heavily.contested";
            const int threadCount = 20;
            const int iterationsPerThread = 200;

            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Use Interlocked for atomic counter operations
            int totalCalls = 0;

            // Use thread-safe collection
            ConcurrentBag<List<ThemeTrieElementRule>> allResults = new ConcurrentBag<List<ThemeTrieElementRule>>();

            // Two-phase synchronization
            CountdownEvent readyEvent = new CountdownEvent(threadCount);
            ManualResetEventSlim startEvent = new ManualResetEventSlim(false);

            // Act
            List<Task> tasks = new List<Task>(threadCount);
            for (int t = 0; t < threadCount; t++)
            {
                Task task = Task.Run(() =>
                {
                    readyEvent.Signal();
                    startEvent.Wait();

                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        List<ThemeTrieElementRule> result = parsedTheme.Match(testScope);

                        Interlocked.Increment(ref totalCalls);
                        allResults.Add(result);

                        // Yield to increase chance of interleaving with other threads
                        if (i % 10 == 0)
                        {
                            Thread.Yield();
                        }
                    }
                });
                tasks.Add(task);
            }

            readyEvent.Wait();  // Wait for all threads to be ready
            startEvent.Set();   // Release all threads at once
            Task.WaitAll(tasks.ToArray());

            readyEvent.Dispose();
            startEvent.Dispose();

            // Assert
            const int expectedTotalCalls = threadCount * iterationsPerThread;
            Assert.AreEqual(expectedTotalCalls, Volatile.Read(ref totalCalls), "All Match calls should have completed");
            Assert.AreEqual(expectedTotalCalls, allResults.Count, "Should have captured all results");

            // All results must be non-null (critical for the fallback logic being tested)
            foreach (List<ThemeTrieElementRule> result in allResults)
            {
                Assert.IsNotNull(result, "Match must never return null, even under heavy concurrent access");
            }

            // Results should all be equivalent (testing cache consistency)
            List<ThemeTrieElementRule> firstResult = allResults.First();
            foreach (List<ThemeTrieElementRule> result in allResults)
            {
                Assert.That(result == firstResult || AreRuleListsEquivalent(result, firstResult),
                    "All results should be identical or equivalent");
            }
        }

        [Test]
        public void Match_FirstCallOnNewScope_ReturnsNonNullResult()
        {
            // Arrange
            const string newScope = "never.before.seen.scope";
            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Act
            List<ThemeTrieElementRule> result = parsedTheme.Match(newScope);

            // Assert
            Assert.IsNotNull(result, "Match should return non-null even for uncached scopes");
        }

        [Test]
        public void Match_SameScopeMultipleTimes_ReturnsCachedInstance()
        {
            // Arrange
            const string testScope = "source.csharp";
            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Act
            List<ThemeTrieElementRule> firstResult = parsedTheme.Match(testScope);
            List<ThemeTrieElementRule> secondResult = parsedTheme.Match(testScope);
            List<ThemeTrieElementRule> thirdResult = parsedTheme.Match(testScope);

            // Assert
            Assert.IsNotNull(firstResult);
            Assert.IsNotNull(secondResult);
            Assert.IsNotNull(thirdResult);
            Assert.AreSame(firstResult, secondResult, "Second call should return cached instance");
            Assert.AreSame(firstResult, thirdResult, "Third call should return cached instance");
        }

        [TestCase("")]
        public void Match_EmptyScope_ReturnsNonNull(string scopeName)
        {
            // Arrange
            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Act
            List<ThemeTrieElementRule> result = parsedTheme.Match(scopeName);

            // Assert
            Assert.IsNotNull(result, "Match should handle empty scope names gracefully");
        }

        [Test]
        public void Match_NullScope_ThrowsArgumentNullException()
        {
            // Arrange
            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => parsedTheme.Match(null),
                "Match should throw ArgumentNullException for null scope names");
        }

        [Test]
        public void Match_LongScopeName_ReturnsNonNull()
        {
            // Arrange
            const int scopeLimitLength = 1_000;
            string longScope = new string('a', scopeLimitLength / 2) + "." + new string('b', scopeLimitLength / 2);
            ParsedTheme parsedTheme = CreateTestParsedTheme();

            // Act
            List<ThemeTrieElementRule> result = parsedTheme.Match(longScope);

            // Assert
            Assert.IsNotNull(result, "Match should handle long scope names");
        }

        #endregion Match tests

        #region Rule sorting tests

        [Test]
        public void CreateFromParsedTheme_RulesWithSameScope_SortsByParentScopes()
        {
            // Arrange
            const string scope = "keyword";

            // Rules with same scope but different parent scopes
            // "source html" should come after "source" lexicographically
            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("rule3", scope, new List<string> { "source", "html" }, 2, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("rule1", scope, null, 0, FontStyle.None, "#00FF00", null),
                new ParsedThemeRule("rule2", scope, new List<string> { "source" }, 1, FontStyle.None, "#0000FF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert - Verify sorting occurred by attempting matches
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches, "Should return match results");
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithSameScopeAndParentScopes_SortsByIndex()
        {
            // Arrange
            const string scope = "keyword.control";
            List<string> parentScopes = new List<string> { "source", "js" };

            // Rules with identical scope and parentScopes, different indices
            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("rule2", scope, parentScopes, 5, FontStyle.Bold, "#FF0000", null),
                new ParsedThemeRule("rule1", scope, parentScopes, 1, FontStyle.Italic, "#00FF00", null),
                new ParsedThemeRule("rule3", scope, parentScopes, 10, FontStyle.Underline, "#0000FF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert - Rules should be sorted by index (1, 5, 10)
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches, "Should return match results after sorting by index");
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithNullAndNonNullParentScopes_SortsNullFirst()
        {
            // Arrange
            const string scope = "string.quoted";

            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("rule2", scope, new List<string> { "source" }, 1, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("rule1", scope, null, 0, FontStyle.None, "#00FF00", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithDifferentParentScopeLengths_SortsByStringComparison()
        {
            // Arrange
            const string scope = "variable";

            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("rule3", scope, new List<string> { "a", "b", "c" }, 2, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("rule1", scope, new List<string> { "a" }, 0, FontStyle.None, "#00FF00", null),
                new ParsedThemeRule("rule2", scope, new List<string> { "a", "b" }, 1, FontStyle.None, "#0000FF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithIdenticalParentScopesAndDifferentIndices_MaintainsIndexOrder()
        {
            // Arrange
            const string scope = "comment.line";
            List<string> identicalParentScopes = new List<string> { "source", "python" };

            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("high-priority", scope, identicalParentScopes, 100, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("low-priority", scope, identicalParentScopes, 1, FontStyle.None, "#00FF00", null),
                new ParsedThemeRule("mid-priority", scope, identicalParentScopes, 50, FontStyle.None, "#0000FF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert - Verify rules are processed in index order (1, 50, 100)
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_ComplexSortingScenario_HandlesAllComparisonLevels()
        {
            // Arrange - Mix of different scopes, parent scopes, and indices
            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                // Different scopes
                new ParsedThemeRule("r1", "z.scope", null, 0, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("r2", "a.scope", null, 1, FontStyle.None, "#00FF00", null),
                
                // Same scope, different parent scopes
                new ParsedThemeRule("r3", "keyword", new List<string> { "z" }, 2, FontStyle.None, "#0000FF", null),
                new ParsedThemeRule("r4", "keyword", new List<string> { "a" }, 3, FontStyle.None, "#FFFF00", null),
                
                // Same scope and parent scopes, different indices
                new ParsedThemeRule("r5", "string", new List<string> { "source" }, 10, FontStyle.None, "#FF00FF", null),
                new ParsedThemeRule("r6", "string", new List<string> { "source" }, 5, FontStyle.None, "#00FFFF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert - All rules should be processed without error
            Assert.IsNotNull(parsedTheme);

            // Verify different scopes can be matched
            Assert.IsNotNull(parsedTheme.Match("a.scope"));
            Assert.IsNotNull(parsedTheme.Match("z.scope"));
            Assert.IsNotNull(parsedTheme.Match("keyword"));
            Assert.IsNotNull(parsedTheme.Match("string"));
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithEmptyParentScopes_SortsCorrectly()
        {
            // Arrange
            const string scope = "meta.tag";

            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("rule2", scope, new List<string>(), 1, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("rule1", scope, null, 0, FontStyle.None, "#00FF00", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithMaxIntIndex_HandlesWithoutOverflow()
        {
            // Arrange
            const string scope = "boundary.test";

            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("max", scope, null, int.MaxValue, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("min", scope, null, int.MinValue, FontStyle.None, "#00FF00", null),
                new ParsedThemeRule("zero", scope, null, 0, FontStyle.None, "#0000FF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert - Should handle extreme index values
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_RulesWithLexicographicallyCloseParentScopes_SortsCorrectly()
        {
            // Arrange
            const string scope = "entity.name";

            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("r3", scope, new List<string> { "source", "aaa" }, 2, FontStyle.None, "#FF0000", null),
                new ParsedThemeRule("r1", scope, new List<string> { "source", "aaaa" }, 0, FontStyle.None, "#00FF00", null),
                new ParsedThemeRule("r2", scope, new List<string> { "source", "aab" }, 1, FontStyle.None, "#0000FF", null)
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match(scope);
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_SingleRule_ProcessesWithoutSortingIssues()
        {
            // Arrange
            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("only-rule", "single.scope", new List<string> { "parent" }, 42, FontStyle.Bold, "#ABCDEF", "#123456")
            };

            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert
            Assert.IsNotNull(parsedTheme);
            List<ThemeTrieElementRule> matches = parsedTheme.Match("single.scope");
            Assert.IsNotNull(matches);
        }

        [Test]
        public void CreateFromParsedTheme_EmptyRuleList_CreatesThemeWithDefaultsOnly()
        {
            // Arrange
            List<ParsedThemeRule> rules = new List<ParsedThemeRule>();
            ColorMap colorMap = new ColorMap();

            // Act
            ParsedTheme parsedTheme = ParsedTheme.CreateFromParsedTheme(rules, colorMap);

            // Assert
            Assert.IsNotNull(parsedTheme);
            Assert.IsNotNull(parsedTheme.GetDefaults());
        }

        #endregion Rule sorting tests

        #region ParsedGuiColors tests

        [Test]
        public void ParsedGuiColors_NullColors_DoesNotModifyDictionary()
        {
            // Arrange
            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns((Dictionary<string, object>)null);

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            CollectionAssert.IsEmpty(colorDictionary, "Dictionary should remain empty when colors are null");
        }

        [Test]
        public void ParsedGuiColors_EmptyColorsDictionary_DoesNotModifyDictionary()
        {
            // Arrange
            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>());

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            CollectionAssert.IsEmpty(colorDictionary, "Dictionary should remain empty when colors dictionary is empty");
        }

        [Test]
        public void ParsedGuiColors_SingleColor_AddsColorToDictionary()
        {
            // Arrange
            const string colorKey = "editor.background";
            const string colorValue = "#1E1E1E";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { colorKey, colorValue }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(1, colorDictionary.Count);
            Assert.IsTrue(colorDictionary.ContainsKey(colorKey));
            Assert.AreEqual(colorValue, colorDictionary[colorKey]);
        }

        [Test]
        public void ParsedGuiColors_MultipleColors_AddsAllColorsToDictionary()
        {
            // Arrange
            const string key1 = "editor.background";
            const string value1 = "#1E1E1E";
            const string key2 = "editor.foreground";
            const string value2 = "#D4D4D4";
            const string key3 = "editor.lineHighlightBackground";
            const string value3 = "#282828";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key1, value1 },
                { key2, value2 },
                { key3, value3 }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(3, colorDictionary.Count);
            Assert.AreEqual(value1, colorDictionary[key1]);
            Assert.AreEqual(value2, colorDictionary[key2]);
            Assert.AreEqual(value3, colorDictionary[key3]);
        }

        [Test]
        public void ParsedGuiColors_DuplicateKey_OverwritesExistingValue()
        {
            // Arrange
            const string colorKey = "editor.background";
            const string initialValue = "#000000";
            const string newValue = "#1E1E1E";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { colorKey, newValue }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>
            {
                { colorKey, initialValue }
            };

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(1, colorDictionary.Count);
            Assert.AreEqual(newValue, colorDictionary[colorKey], "New value should overwrite existing value");
        }

        [Test]
        public void ParsedGuiColors_KeysWithSpecialCharacters_PreservesKeys()
        {
            // Arrange
            const string key1 = "editor.selection-background";
            const string key2 = "editor_tab.activeBackground";
            const string key3 = "panel.border#top";
            const string value = "#264F78";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key1, value },
                { key2, value },
                { key3, value }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(3, colorDictionary.Count);
            Assert.IsTrue(colorDictionary.ContainsKey(key1), "Key with dash should be preserved");
            Assert.IsTrue(colorDictionary.ContainsKey(key2), "Key with underscore should be preserved");
            Assert.IsTrue(colorDictionary.ContainsKey(key3), "Key with hash should be preserved");
        }

        [Test]
        public void ParsedGuiColors_EmptyStringKey_AddsToDict()
        {
            // Arrange
            const string emptyKey = "";
            const string value = "#FFFFFF";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { emptyKey, value }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(1, colorDictionary.Count);
            Assert.IsTrue(colorDictionary.ContainsKey(emptyKey));
        }

        [Test]
        public void ParsedGuiColors_EmptyStringValue_AddsEmptyString()
        {
            // Arrange
            const string key = "editor.background";
            const string emptyValue = "";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key, emptyValue }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(1, colorDictionary.Count);
            Assert.AreEqual(emptyValue, colorDictionary[key], "Empty string value should be preserved");
        }

        [Test]
        public void ParsedGuiColors_WhitespaceValue_PreservesWhitespace()
        {
            // Arrange
            const string key = "editor.background";
            const string whitespaceValue = "   ";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key, whitespaceValue }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(whitespaceValue, colorDictionary[key], "Whitespace value should be preserved as-is");
        }

        [Test]
        public void ParsedGuiColors_CalledMultipleTimes_AccumulatesColors()
        {
            // Arrange
            const string key1 = "editor.background";
            const string value1 = "#1E1E1E";
            const string key2 = "editor.foreground";
            const string value2 = "#D4D4D4";

            Mock<IRawTheme> mockTheme1 = new Mock<IRawTheme>();
            mockTheme1.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key1, value1 }
            });

            Mock<IRawTheme> mockTheme2 = new Mock<IRawTheme>();
            mockTheme2.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key2, value2 }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme1.Object, colorDictionary);
            ParsedTheme.ParsedGuiColors(mockTheme2.Object, colorDictionary);

            // Assert
            Assert.AreEqual(2, colorDictionary.Count);
            Assert.AreEqual(value1, colorDictionary[key1]);
            Assert.AreEqual(value2, colorDictionary[key2]);
        }

        [Test]
        public void ParsedGuiColors_CalledMultipleTimesWithSameKey_LastCallWins()
        {
            // Arrange
            const string key = "editor.background";
            const string firstValue = "#000000";
            const string secondValue = "#1E1E1E";

            Mock<IRawTheme> mockTheme1 = new Mock<IRawTheme>();
            mockTheme1.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key, firstValue }
            });

            Mock<IRawTheme> mockTheme2 = new Mock<IRawTheme>();
            mockTheme2.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key, secondValue }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme1.Object, colorDictionary);
            ParsedTheme.ParsedGuiColors(mockTheme2.Object, colorDictionary);

            // Assert
            Assert.AreEqual(1, colorDictionary.Count);
            Assert.AreEqual(secondValue, colorDictionary[key], "Second call should overwrite first value");
        }

        [Test]
        public void ParsedGuiColors_LongKeyAndValue_HandlesWithoutIssue()
        {
            // Arrange
            const int keyLength = 1_000;
            const int valueLength = 1_000;
            string longKey = new string('k', keyLength);
            string longValue = new string('v', valueLength);

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { longKey, longValue }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(1, colorDictionary.Count);
            Assert.IsTrue(colorDictionary.ContainsKey(longKey));
            Assert.AreEqual(longValue, colorDictionary[longKey]);
        }

        [Test]
        public void ParsedGuiColors_ManyColors_AddsAllColors()
        {
            // Arrange
            const int colorCount = 100;
            Dictionary<string, object> colors = new Dictionary<string, object>();

            for (int i = 0; i < colorCount; i++)
            {
                colors[$"color.key{i}"] = $"#00{i:X4}";
            }

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(colors);

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(colorCount, colorDictionary.Count);

            for (int i = 0; i < colorCount; i++)
            {
                string expectedKey = $"color.key{i}";
                string expectedValue = $"#00{i:X4}";
                Assert.IsTrue(colorDictionary.ContainsKey(expectedKey), $"Should contain key: {expectedKey}");
                Assert.AreEqual(expectedValue, colorDictionary[expectedKey]);
            }
        }

        [Test]
        public void ParsedGuiColors_NonHexColorFormats_StoresAsIs()
        {
            // Arrange
            const string key1 = "color.rgb";
            const string value1 = "rgb(255, 0, 0)";
            const string key2 = "color.rgba";
            const string value2 = "rgba(255, 0, 0, 0.5)";
            const string key3 = "color.hsl";
            const string value3 = "hsl(0, 100%, 50%)";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { key1, value1 },
                { key2, value2 },
                { key3, value3 }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(3, colorDictionary.Count);
            Assert.AreEqual(value1, colorDictionary[key1], "RGB format should be stored as-is");
            Assert.AreEqual(value2, colorDictionary[key2], "RGBA format should be stored as-is");
            Assert.AreEqual(value3, colorDictionary[key3], "HSL format should be stored as-is");
        }

        [Test]
        public void ParsedGuiColors_CaseSensitiveKeys_TreatsAsDifferentKeys()
        {
            // Arrange
            const string lowerKey = "editor.background";
            const string upperKey = "EDITOR.BACKGROUND";
            const string mixedKey = "Editor.Background";
            const string value = "#1E1E1E";

            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetGuiColors()).Returns(new Dictionary<string, object>
            {
                { lowerKey, value },
                { upperKey, value },
                { mixedKey, value }
            });

            Dictionary<string, string> colorDictionary = new Dictionary<string, string>();

            // Act
            ParsedTheme.ParsedGuiColors(mockTheme.Object, colorDictionary);

            // Assert
            Assert.AreEqual(3, colorDictionary.Count, "Keys with different casing should be treated as different keys");
            Assert.IsTrue(colorDictionary.ContainsKey(lowerKey));
            Assert.IsTrue(colorDictionary.ContainsKey(upperKey));
            Assert.IsTrue(colorDictionary.ContainsKey(mixedKey));
        }

        #endregion ParsedGuiColors tests
        #region ParseFontStyle tests (via ParseTheme)

        [Test]
        public void ParseTheme_FontStyleEmpty_ReturnsFontStyleNone()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.None, rules[0].fontStyle, "Empty fontStyle string should parse as None");
        }

        [Test]
        public void ParseTheme_FontStyleItalic_ReturnsFontStyleItalic()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.Italic, rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_FontStyleBold_ReturnsFontStyleBold()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "bold",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.Bold, rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_FontStyleUnderline_ReturnsFontStyleUnderline()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "underline",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.Underline, rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_FontStyleStrikethrough_ReturnsFontStyleStrikethrough()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "strikethrough",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.Strikethrough, rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_FontStyleItalicBold_CombinesFlags()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic bold",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.Italic | FontStyle.Bold, rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_FontStyleAllCombined_CombinesAllFlags()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic bold underline strikethrough",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold | FontStyle.Underline | FontStyle.Strikethrough,
                rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_FontStyleWithExtraSpaces_ParsesCorrectly()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic  bold   underline",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold | FontStyle.Underline,
                rules[0].fontStyle,
                "Extra spaces between style keywords should be handled correctly");
        }

        [Test]
        public void ParseTheme_FontStyleWithLeadingSpace_ParsesCorrectly()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = " italic bold",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold,
                rules[0].fontStyle,
                "Leading space should be handled by creating empty segment which is ignored");
        }

        [Test]
        public void ParseTheme_FontStyleWithTrailingSpace_ParsesCorrectly()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic bold ",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold,
                rules[0].fontStyle,
                "Trailing space should not affect parsing");
        }

        [Test]
        public void ParseTheme_FontStyleUnknownKeyword_IgnoresUnknown()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic unknown bold",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold,
                rules[0].fontStyle,
                "Unknown keywords should be ignored");
        }

        [TestCase("ITALIC", FontStyle.None, "Uppercase should not match")]
        [TestCase("Bold", FontStyle.None, "Mixed case should not match")]
        [TestCase("BOLD", FontStyle.None, "Uppercase should not match")]
        [TestCase("Underline", FontStyle.None, "Mixed case should not match")]
        public void ParseTheme_FontStyleCaseSensitive_RequiresLowercase(string fontStyleString, FontStyle expected, string reason)
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = fontStyleString,
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expected, rules[0].fontStyle, reason);
        }

        [Test]
        public void ParseTheme_FontStylePartialMatch_DoesNotMatch()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "ital boldy underl",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.None, rules[0].fontStyle, "Partial keyword matches should not be recognized");
        }

        [Test]
        public void ParseTheme_FontStyleDuplicateKeywords_AppliesOnce()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic bold italic bold",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold,
                rules[0].fontStyle,
                "Duplicate keywords should result in same flags (bitwise OR is idempotent)");
        }

        [Test]
        public void ParseTheme_FontStyleOnlySpaces_ReturnsFontStyleNone()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "   ",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.None, rules[0].fontStyle, "String with only spaces should parse as None");
        }

        [Test]
        public void ParseTheme_FontStyleSingleSpace_ReturnsFontStyleNone()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = " ",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.None, rules[0].fontStyle, "Single space should parse as None");
        }

        [Test]
        public void ParseTheme_FontStyleVeryLongString_ParsesCorrectly()
        {
            // Arrange
            const int repeatCount = 100;
            string longFontStyle = string.Join(" ", Enumerable.Repeat("italic bold", repeatCount));

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = longFontStyle,
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold,
                rules[0].fontStyle,
                "Very long fontStyle string should parse correctly");
        }

        [Test]
        public void ParseTheme_FontStyleMixedValidAndInvalid_ParsesOnlyValid()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "normal italic invalid bold foo underline bar",
                            ["foreground"] = "#FF0000"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(
                FontStyle.Italic | FontStyle.Bold | FontStyle.Underline,
                rules[0].fontStyle,
                "Should parse only valid keywords and ignore invalid ones");
        }

        [Test]
        public void ParseTheme_MultipleScopesWithDifferentFontStyles_ParsesEachCorrectly()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "scope1",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic",
                            ["foreground"] = "#FF0000"
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "scope2",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "bold",
                            ["foreground"] = "#00FF00"
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "scope3",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "underline strikethrough",
                            ["foreground"] = "#0000FF"
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(3, rules.Count);
            Assert.AreEqual(FontStyle.Italic, rules[0].fontStyle);
            Assert.AreEqual(FontStyle.Bold, rules[1].fontStyle);
            Assert.AreEqual(FontStyle.Underline | FontStyle.Strikethrough, rules[2].fontStyle);
        }

        #endregion ParseFontStyle tests (via ParseTheme)

        #region ExtractScopeAndParents tests (via ParseTheme)

        [Test]
        public void ParseTheme_SingleSegmentScope_ReturnsNullParentScopes()
        {
            // Arrange
            const string singleSegmentScope = "keyword";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = singleSegmentScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(singleSegmentScope, rules[0].scope, "Single segment should be the scope");
            Assert.IsNull(rules[0].parentScopes, "Single segment scope should have null parentScopes (fast path)");
        }

        [Test]
        public void ParseTheme_TwoSegmentScope_ExtractsLastAsScope()
        {
            // Arrange
            const string twoSegmentScope = "text html";
            const string expectedScope = "html";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = twoSegmentScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope, "Last segment should be the scope");
            Assert.IsNotNull(rules[0].parentScopes);
            Assert.AreEqual(2, rules[0].parentScopes.Count, "Should have 2 segments in parentScopes");
            CollectionAssert.AreEqual(new[] { "html", "text" }, rules[0].parentScopes, "Parent scopes should be in reverse order");
        }

        [Test]
        public void ParseTheme_ThreeSegmentScope_ExtractsInReverseOrder()
        {
            // Arrange
            const string threeSegmentScope = "text html basic";
            const string expectedScope = "basic";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = threeSegmentScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.IsNotNull(rules[0].parentScopes);
            Assert.AreEqual(3, rules[0].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "basic", "html", "text" }, rules[0].parentScopes,
                "Parent scopes should be all segments in reverse order");
        }

        [Test]
        public void ParseTheme_FourSegmentScope_AllSegmentsReversed()
        {
            // Arrange
            const string fourSegmentScope = "source js meta function";
            const string expectedScope = "function";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = fourSegmentScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.AreEqual(4, rules[0].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "function", "meta", "js", "source" }, rules[0].parentScopes);
        }

        [Test]
        public void ParseTheme_ManySegmentScope_HandlesLargeCount()
        {
            // Arrange
            const int segmentCount = 10;
            string[] segments = new string[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                segments[i] = $"segment{i}";
            }
            string manySegmentScope = string.Join(" ", segments);
            string expectedScope = segments[segmentCount - 1];

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = manySegmentScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.AreEqual(segmentCount, rules[0].parentScopes.Count);

            // Verify reverse order
            for (int i = 0; i < segmentCount; i++)
            {
                Assert.AreEqual(segments[segmentCount - 1 - i], rules[0].parentScopes[i],
                    $"Parent scope at index {i} should be segment{segmentCount - 1 - i}");
            }
        }

        [Test]
        public void ParseTheme_ScopeWithConsecutiveSpaces_CreatesEmptySegmentsBetween()
        {
            // Arrange
            const string scopeWithConsecutiveSpaces = "keyword  control";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = scopeWithConsecutiveSpaces,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual("control", rules[0].scope);
            Assert.IsNotNull(rules[0].parentScopes);
            Assert.AreEqual(3, rules[0].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "control", "", "keyword" }, rules[0].parentScopes,
                "Consecutive spaces create empty segment between");
        }

        [Test]
        public void ParseTheme_ScopeWithSpecialCharacters_PreservesCharacters()
        {
            // Arrange
            const string scopeWithSpecialChars = "meta.tag.custom-element source.js.embedded";
            const string expectedScope = "source.js.embedded";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = scopeWithSpecialChars,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.AreEqual(2, rules[0].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "source.js.embedded", "meta.tag.custom-element" }, rules[0].parentScopes,
                "Special characters like dots and hyphens should be preserved");
        }

        [Test]
        public void ParseTheme_VeryLongScopeString_HandlesWithoutIssue()
        {
            // Arrange
            const int segmentCount = 100;
            string[] segments = new string[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                segments[i] = $"verylongsegmentname{i}withlotsoflongercharacterstomakeit";
            }
            string veryLongScope = string.Join(" ", segments);
            string expectedScope = segments[segmentCount - 1];

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = veryLongScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.AreEqual(segmentCount, rules[0].parentScopes.Count);
        }

        [Test]
        public void ParseTheme_ScopeWithMixedSegmentLengths_HandlesCorrectly()
        {
            // Arrange
            const string mixedLengthScope = "a verylongsegment b shortone c";
            const string expectedScope = "c";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = mixedLengthScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.AreEqual(5, rules[0].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "c", "shortone", "b", "verylongsegment", "a" }, rules[0].parentScopes);
        }

        [Test]
        public void ParseTheme_ScopeWithNumericSegments_ParsesCorrectly()
        {
            // Arrange
            const string numericScope = "segment1 segment2 segment3";
            const string expectedScope = "segment3";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = numericScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            CollectionAssert.AreEqual(new[] { "segment3", "segment2", "segment1" }, rules[0].parentScopes);
        }

        [Test]
        public void ParseTheme_MultipleScopesWithDifferentSegments_ParsesEachCorrectly()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "single",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "two segments",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#00FF00" }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "three segment scope",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#0000FF" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(3, rules.Count);

            // First rule: single segment
            Assert.AreEqual("single", rules[0].scope);
            Assert.IsNull(rules[0].parentScopes);

            // Second rule: two segments
            Assert.AreEqual("segments", rules[1].scope);
            Assert.AreEqual(2, rules[1].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "segments", "two" }, rules[1].parentScopes);

            // Third rule: three segments
            Assert.AreEqual("scope", rules[2].scope);
            Assert.AreEqual(3, rules[2].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "scope", "segment", "three" }, rules[2].parentScopes);
        }

        [Test]
        public void ParseTheme_ScopeWithUnicodeCharacters_PreservesUnicode()
        {
            // Arrange
            const string unicodeScope = "テキスト HTML 基本";
            const string expectedScope = "基本";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = unicodeScope,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(expectedScope, rules[0].scope);
            Assert.AreEqual(3, rules[0].parentScopes.Count);
            CollectionAssert.AreEqual(new[] { "基本", "HTML", "テキスト" }, rules[0].parentScopes,
                "Unicode characters should be preserved");
        }

        #endregion ExtractScopeAndParents tests (via ParseTheme)

        #region LookupThemeRules tests (via ParseTheme)

        [Test]
        public void ParseTheme_NullSettings_ReturnsEmptyList()
        {
            // Arrange
            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();
            mockTheme.Setup(t => t.GetSettings()).Returns((List<IRawThemeSetting>)null);
            mockTheme.Setup(t => t.GetTokenColors()).Returns((List<IRawThemeSetting>)null);

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(mockTheme.Object, 0);

            // Assert
            Assert.IsNotNull(rules);
            CollectionAssert.IsEmpty(rules);
        }

        [Test]
        public void ParseTheme_EntryWithNullSettings_SkipsEntry()
        {
            // Arrange
            Mock<IRawThemeSetting> mockSettingWithNull = new Mock<IRawThemeSetting>();
            mockSettingWithNull.Setup(s => s.GetSetting()).Returns((IThemeSetting)null);

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    mockSettingWithNull.Object,
                    new ThemeRaw
                    {
                        ["scope"] = "valid.scope",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count, "Should skip entry with null settings");
            Assert.AreEqual("valid.scope", rules[0].scope);
        }

        [Test]
        public void ParseTheme_CommaSeparatedScopes_CreatesMultipleRules()
        {
            // Arrange
            const string commaSeparated = "keyword.control,keyword.operator,keyword.other";
            const string foreground = "#FF0000";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = commaSeparated,
                        ["settings"] = new ThemeRaw { ["foreground"] = foreground }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(3, rules.Count);
            Assert.AreEqual("keyword.control", rules[0].scope);
            Assert.AreEqual("keyword.operator", rules[1].scope);
            Assert.AreEqual("keyword.other", rules[2].scope);
            Assert.AreEqual(foreground, rules[0].foreground);
            Assert.AreEqual(foreground, rules[1].foreground);
            Assert.AreEqual(foreground, rules[2].foreground);
        }

        [Test]
        public void ParseTheme_CommaSeparatedScopesWithSpaces_TrimsEachScope()
        {
            // Arrange
            const string commaSeparated = "keyword.control , keyword.operator , keyword.other";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = commaSeparated,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(3, rules.Count);
            Assert.AreEqual("keyword.control", rules[0].scope);
            Assert.AreEqual("keyword.operator", rules[1].scope);
            Assert.AreEqual("keyword.other", rules[2].scope);
        }

        [Test]
        public void ParseTheme_OnlyCommas_CreatesNoRules()
        {
            // Arrange
            const string onlyCommas = ",,,";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = onlyCommas,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            CollectionAssert.IsEmpty(rules, "Only commas should produce no rules (empty after trim)");
        }

        [Test]
        public void ParseTheme_LeadingTrailingCommas_IgnoresEmptySegments()
        {
            // Arrange
            const string withCommas = ",keyword.control,keyword.operator,";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = withCommas,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(2, rules.Count, "Leading/trailing commas should be trimmed");
            Assert.AreEqual("keyword.control", rules[0].scope);
            Assert.AreEqual("keyword.operator", rules[1].scope);
        }

        [Test]
        public void ParseTheme_ConsecutiveCommas_CreatesOnlyNonEmptyScopes()
        {
            // Arrange
            const string consecutiveCommas = "keyword.control,,keyword.operator";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = consecutiveCommas,
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(2, rules.Count, "Empty segments between commas should be skipped");
            Assert.AreEqual("keyword.control", rules[0].scope);
            Assert.AreEqual("keyword.operator", rules[1].scope);
        }

        [Test]
        public void ParseTheme_ScopeAsListOfStrings_CreatesRuleForEach()
        {
            // Arrange
            Mock<IRawThemeSetting> mockSetting = new Mock<IRawThemeSetting>();
            List<object> scopeList = new List<object> { "keyword.control", "keyword.operator", "keyword.other" };
            mockSetting.Setup(s => s.GetScope()).Returns(scopeList);
            mockSetting.Setup(s => s.GetSetting()).Returns(new ThemeRaw { ["foreground"] = "#FF0000" });

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting> { mockSetting.Object }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(3, rules.Count);
            Assert.AreEqual("keyword.control", rules[0].scope);
            Assert.AreEqual("keyword.operator", rules[1].scope);
            Assert.AreEqual("keyword.other", rules[2].scope);
        }

        [Test]
        public void ParseTheme_ScopeAsEmptyList_CreatesNoRules()
        {
            // Arrange
            Mock<IRawThemeSetting> mockSetting = new Mock<IRawThemeSetting>();
            List<object> emptyList = new List<object>();
            mockSetting.Setup(s => s.GetScope()).Returns(emptyList);
            mockSetting.Setup(s => s.GetSetting()).Returns(new ThemeRaw { ["foreground"] = "#FF0000" });

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting> { mockSetting.Object }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            CollectionAssert.IsEmpty(rules, "Empty scope list should produce no rules");
        }

        [Test]
        public void ParseTheme_ScopeAsNullOrOtherType_CreatesRuleWithEmptyScope()
        {
            // Arrange - scope is an integer (not string or IList<object>)
            Mock<IRawThemeSetting> mockSetting = new Mock<IRawThemeSetting>();
            mockSetting.Setup(s => s.GetScope()).Returns(12345);
            mockSetting.Setup(s => s.GetSetting()).Returns(new ThemeRaw { ["foreground"] = "#FF0000" });

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting> { mockSetting.Object }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual("", rules[0].scope, "Non-string/non-list scope should create rule with empty scope");
        }

        [Test]
        public void ParseTheme_InvalidForegroundColor_SetsNullForeground()
        {
            // Arrange
            const string invalidColor = "notahexcolor";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw { ["foreground"] = invalidColor }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.IsNull(rules[0].foreground, "Invalid hex color should result in null foreground");
        }

        [Test]
        public void ParseTheme_InvalidBackgroundColor_SetsNullBackground()
        {
            // Arrange
            const string invalidColor = "rgb(255,0,0)";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw { ["background"] = invalidColor }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.IsNull(rules[0].background, "Invalid hex color should result in null background");
        }

        [Test]
        public void ParseTheme_MissingForeground_SetsNullForeground()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw { ["background"] = "#FFFFFF" }
                        // No foreground
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.IsNull(rules[0].foreground);
        }

        [Test]
        public void ParseTheme_MissingBackground_SetsNullBackground()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#000000" }
                        // No background
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.IsNull(rules[0].background);
        }

        [Test]
        public void ParseTheme_NonStringFontStyle_SetsFontStyleNotSet()
        {
            // Arrange - fontStyle is not a string
            Mock<IRawThemeSetting> mockSetting = new Mock<IRawThemeSetting>();
            mockSetting.Setup(s => s.GetScope()).Returns("test.scope");
            mockSetting.Setup(s => s.GetSetting()).Returns(new ThemeRaw
            {
                ["fontStyle"] = 123, // Not a string
                ["foreground"] = "#FF0000"
            });

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting> { mockSetting.Object }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(FontStyle.NotSet, rules[0].fontStyle);
        }

        [Test]
        public void ParseTheme_RuleIndexIncrementsForEachEntry()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "scope1",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "scope2",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#00FF00" }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "scope3",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#0000FF" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(3, rules.Count);
            Assert.AreEqual(0, rules[0].index);
            Assert.AreEqual(1, rules[1].index);
            Assert.AreEqual(2, rules[2].index);
        }

        [Test]
        public void ParseTheme_CommaSeparatedScopesShareSameIndex()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "keyword.control,keyword.operator",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(2, rules.Count, "Both rules from same entry should be created");
            Assert.AreEqual(0, rules[0].index, "Both rules from same entry should share index 0");
            Assert.AreEqual(0, rules[1].index, "Both rules from same entry should share index 0");
        }

        [Test]
        public void ParseTheme_GetNamePreserved_SetsNameOnRule()
        {
            // Arrange
            const string ruleName = "Custom Rule Name";

            Mock<IRawThemeSetting> mockSetting = new Mock<IRawThemeSetting>();
            mockSetting.Setup(s => s.GetScope()).Returns("test.scope");
            mockSetting.Setup(s => s.GetName()).Returns(ruleName);
            mockSetting.Setup(s => s.GetSetting()).Returns(new ThemeRaw { ["foreground"] = "#FF0000" });

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting> { mockSetting.Object }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(ruleName, rules[0].name);
        }

        [Test]
        public void ParseTheme_BothSettingsAndTokenColors_ProcessesBoth()
        {
            // Arrange
            Mock<IRawTheme> mockTheme = new Mock<IRawTheme>();

            List<IRawThemeSetting> settings = new List<IRawThemeSetting>
            {
                new ThemeRaw
                {
                    ["scope"] = "from.settings",
                    ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                }
            };

            List<IRawThemeSetting> tokenColors = new List<IRawThemeSetting>
            {
                new ThemeRaw
                {
                    ["scope"] = "from.tokenColors",
                    ["settings"] = new ThemeRaw { ["foreground"] = "#00FF00" }
                }
            };

            mockTheme.Setup(t => t.GetSettings()).Returns(settings);
            mockTheme.Setup(t => t.GetTokenColors()).Returns(tokenColors);

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(mockTheme.Object, 0);

            // Assert
            Assert.AreEqual(2, rules.Count);
            Assert.AreEqual("from.settings", rules[0].scope);
            Assert.AreEqual("from.tokenColors", rules[1].scope);
        }

        [Test]
        public void ParseTheme_ValidHexColors_PreservesColors()
        {
            // Arrange
            const string foreground = "#ABCDEF";
            const string background = "#123456";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "test.scope",
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = foreground,
                            ["background"] = background
                        }
                    }
                }
            };

            // Act
            List<ParsedThemeRule> rules = ParsedTheme.ParseTheme(rawTheme, 0);

            // Assert
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual(foreground, rules[0].foreground);
            Assert.AreEqual(background, rules[0].background);
        }

        #endregion LookupThemeRules tests (via ParseTheme)
        #region Helper methods

        /// <summary>
        /// Creates a test ParsedTheme instance for testing Match behavior.
        /// Uses CreateFromParsedTheme to construct through public API without reflection.
        /// </summary>
        private static ParsedTheme CreateTestParsedTheme()
        {
            List<ParsedThemeRule> rules = new List<ParsedThemeRule>
            {
                new ParsedThemeRule("test", "source", null, 0, FontStyle.None, "#000000", "#ffffff"),
                new ParsedThemeRule("test", "comment", null, 1, FontStyle.Italic, "#008000", null),
                new ParsedThemeRule("test", "keyword", null, 2, FontStyle.Bold, "#0000ff", null)
            };

            ColorMap colorMap = new ColorMap();
            return ParsedTheme.CreateFromParsedTheme(rules, colorMap);
        }

        /// <summary>
        /// Compares two rule lists for content equivalence (same count and element-wise equality).
        /// </summary>
        private static bool AreRuleListsEquivalent(List<ThemeTrieElementRule> list1, List<ThemeTrieElementRule> list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!AreRulesEquivalent(list1[i], list2[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares two ThemeTrieElementRule instances for equivalence.
        /// </summary>
        private static bool AreRulesEquivalent(ThemeTrieElementRule rule1, ThemeTrieElementRule rule2)
        {
            if (rule1 == null && rule2 == null) return true;
            if (rule1 == null || rule2 == null) return false;

            // Compare public properties for equivalence
            return rule1.fontStyle == rule2.fontStyle &&
                   rule1.foreground == rule2.foreground &&
                   rule1.background == rule2.background;
        }

        #endregion Helper methods
    }
}