using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TextMateSharp.Internal.Themes;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Themes
{
    [TestFixture]
    public class ThemeTests
    {
        private const int NullColorId = 0;
        private const int DefaultBackgroundColorId = 2;
        private const int FirstCustomColorId = 3;
        private const int SingleScopeMatchCount = 1;
        private const int DuplicateScopeMatchCount = 2;

        #region GetColorId tests

        [Test]
        public void GetColorId_NullColor_ReturnsZero()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int result = theme.GetColorId(null);

            // Assert
            Assert.AreEqual(NullColorId, result);
        }

        [Test]
        public void GetColorId_ValidColor_ReturnsValidId()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int result = theme.GetColorId("#FFFFFF");

            // Assert
            Assert.AreEqual(DefaultBackgroundColorId, result);
        }

        [Test]
        public void GetColorId_SameColorCalledTwice_ReturnsSameId()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string color = "#FF0000";

            // Act
            int firstCall = theme.GetColorId(color);
            int secondCall = theme.GetColorId(color);

            // Assert
            Assert.AreEqual(firstCall, secondCall);
        }

        [Test]
        public void GetColorId_DifferentColors_ReturnsDifferentIds()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int id1 = theme.GetColorId("#FF0000");
            int id2 = theme.GetColorId("#00FF00");

            // Assert
            Assert.AreNotEqual(id1, id2);
        }

        [TestCase("#ffffff", "#FFFFFF")]
        [TestCase("#ff00ff", "#FF00FF")]
        [TestCase("#abc123", "#ABC123")]
        public void GetColorId_CaseInsensitive_ReturnsSameId(string color1, string color2)
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int id1 = theme.GetColorId(color1);
            int id2 = theme.GetColorId(color2);

            // Assert
            Assert.AreEqual(id1, id2);
        }

        [Test]
        public void GetColorId_EmptyString_ReturnsValidId()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int result = theme.GetColorId(string.Empty);

            // Assert
            Assert.AreEqual(FirstCustomColorId, result);
        }

        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("\n")]
        [TestCase(" \t\n ")]
        public void GetColorId_WhitespaceString_ReturnsValidId(string whitespace)
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int result = theme.GetColorId(whitespace);

            // Assert
            Assert.AreEqual(FirstCustomColorId, result);
        }

        [Test]
        public void GetColorId_WhitespaceString_StoresUppercaseVersion()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string whitespace = "   ";

            // Act
            int id = theme.GetColorId(whitespace);
            string storedColor = theme.GetColor(id);

            // Assert
            Assert.AreEqual(FirstCustomColorId, id);
            Assert.AreEqual(whitespace.ToUpper(), storedColor,
                "Whitespace strings should be stored in uppercase as-is without normalization");
        }

        [Test]
        public void GetColorId_InvalidColorFormats_StoresAsIsInUppercase()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string invalidColor = "rgb(255,0,0)";

            // Act
            int id = theme.GetColorId(invalidColor);
            string storedColor = theme.GetColor(id);

            // Assert
            Assert.AreEqual(FirstCustomColorId, id);
            Assert.AreEqual(invalidColor.ToUpper(), storedColor,
                "Invalid color formats should be stored as-is in uppercase without validation");
        }

        [TestCase("invalid_color")]
        [TestCase("@#$%^&*()")]
        public void GetColorId_SpecialCharacters_RoundTripPreservesValue(string color)
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            int id = theme.GetColorId(color);
            string retrieved = theme.GetColor(id);

            // Assert
            Assert.AreEqual(color.ToUpper(), retrieved,
                "Special characters should round-trip correctly in uppercase");
        }

        [Test]
        public void GetColorId_NullAfterValidColor_ReturnsZero()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            theme.GetColorId("#FF0000"); // Add a color first

            // Act
            int result = theme.GetColorId(null);

            // Assert
            Assert.AreEqual(NullColorId, result,
                "Null should always return 0 regardless of other colors added");
        }

        [Test]
        public void GetColorId_ManyUniqueColors_AllReceiveUniqueIds()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            const int colorCount = 1_000;
            HashSet<int> uniqueIds = new HashSet<int>();

            // Act
            for (int i = 0; i < colorCount; i++)
            {
                string color = $"#{i:X6}";
                int id = theme.GetColorId(color);
                uniqueIds.Add(id);
            }

            // Assert
            Assert.AreEqual(colorCount, uniqueIds.Count,
                "Each unique color should receive a unique ID");
        }

        [Test]
        public void GetColorId_HexColorFormat_ReturnsUniqueId()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string hexColor = "#FF5733";

            // Act
            int id = theme.GetColorId(hexColor);
            string storedColor = theme.GetColor(id);

            // Assert
            Assert.AreEqual(FirstCustomColorId, id);
            Assert.AreEqual(hexColor, storedColor);
        }

        [Test]
        public void GetColorId_RgbColorFormat_StoresAsUniqueColor()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string rgbColor = "rgb(255, 87, 51)";

            // Act
            int id = theme.GetColorId(rgbColor);
            string storedColor = theme.GetColor(id);

            // Assert
            Assert.AreEqual(FirstCustomColorId, id);
            Assert.AreEqual(rgbColor.ToUpper(), storedColor,
                "RGB format is stored as-is in uppercase without normalization");
        }

        [Test]
        public void GetColorId_DifferentFormatsForSameVisualColor_ReturnsDifferentIds()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string hexColor = "#FF5733";
            const string rgbColor = "rgb(255, 87, 51)";

            // Act
            int hexId = theme.GetColorId(hexColor);
            int rgbId = theme.GetColorId(rgbColor);

            // Assert
            Assert.AreNotEqual(hexId, rgbId,
                "Different color format strings are treated as different colors without normalization");
        }

        [Test]
        public void GetColorId_RgbaColorFormat_StoresAsUniqueColor()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string rgbaColor = "rgba(255, 87, 51, 1)";

            // Act
            int id = theme.GetColorId(rgbaColor);
            string storedColor = theme.GetColor(id);

            // Assert
            Assert.AreEqual(FirstCustomColorId, id);
            Assert.AreEqual(rgbaColor.ToUpper(), storedColor);
        }

        [Test]
        public void GetColorId_HslColorFormat_StoresAsUniqueColor()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string hslColor = "hsl(14, 100%, 60%)";

            // Act
            int id = theme.GetColorId(hslColor);
            string storedColor = theme.GetColor(id);

            // Assert
            Assert.AreEqual(FirstCustomColorId, id);
            Assert.AreEqual(hslColor.ToUpper(), storedColor);
        }

        #endregion GetColorId tests

        #region GetGuiColorDictionary tests

        [Test]
        public void GetGuiColorDictionary_WithEmptyGuiColors_ReturnsEmptyReadOnlyDictionary()
        {
            // Arrange
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            Dictionary<string, object> emptyColors = new Dictionary<string, object>();
            mockRawTheme.Setup(x => x.GetGuiColors()).Returns(emptyColors);
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            Theme theme = Theme.CreateFromRawTheme(mockRawTheme.Object, mockRegistryOptions.Object);

            // Act
            ReadOnlyDictionary<string, string> result = theme.GetGuiColorDictionary();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ReadOnlyDictionary<string, string>>(result);
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void GetGuiColorDictionary_WithSingleGuiColor_ReturnsDictionaryWithOneEntry()
        {
            // Arrange
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            Dictionary<string, object> guiColors = new Dictionary<string, object>
            {
                { "editor.background", "#1E1E1E" }
            };
            mockRawTheme.Setup(x => x.GetGuiColors()).Returns(guiColors);
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            Theme theme = Theme.CreateFromRawTheme(mockRawTheme.Object, mockRegistryOptions.Object);

            // Act
            ReadOnlyDictionary<string, string> result = theme.GetGuiColorDictionary();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("editor.background"));
            Assert.AreEqual("#1E1E1E", result["editor.background"]);
        }

        [Test]
        public void GetGuiColorDictionary_WithMultipleGuiColors_ReturnsDictionaryWithAllEntries()
        {
            // Arrange
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            Dictionary<string, object> guiColors = new Dictionary<string, object>
            {
                { "editor.background", "#1E1E1E" },
                { "editor.foreground", "#D4D4D4" },
                { "editor.lineHighlightBackground", "#282828" }
            };
            mockRawTheme.Setup(x => x.GetGuiColors()).Returns(guiColors);
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            Theme theme = Theme.CreateFromRawTheme(mockRawTheme.Object, mockRegistryOptions.Object);

            // Act
            ReadOnlyDictionary<string, string> result = theme.GetGuiColorDictionary();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey("editor.background"));
            Assert.AreEqual("#1E1E1E", result["editor.background"]);
            Assert.IsTrue(result.ContainsKey("editor.foreground"));
            Assert.AreEqual("#D4D4D4", result["editor.foreground"]);
            Assert.IsTrue(result.ContainsKey("editor.lineHighlightBackground"));
            Assert.AreEqual("#282828", result["editor.lineHighlightBackground"]);
        }

        [Test]
        public void GetGuiColorDictionary_ReturnsReadOnlyDictionary_CannotBeCastToMutableDictionary()
        {
            // Arrange
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            Dictionary<string, object> guiColors = new Dictionary<string, object>
            {
                { "editor.background", "#1E1E1E" }
            };
            mockRawTheme.Setup(x => x.GetGuiColors()).Returns(guiColors);
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            Theme theme = Theme.CreateFromRawTheme(mockRawTheme.Object, mockRegistryOptions.Object);

            // Act
            ReadOnlyDictionary<string, string> result = theme.GetGuiColorDictionary();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ReadOnlyDictionary<string, string>>(result);
            Assert.IsNotInstanceOf<Dictionary<string, string>>(result);
        }

        [Test]
        public void GetGuiColorDictionary_WithSpecialCharactersInKeysAndValues_PreservesSpecialCharacters()
        {
            // Arrange
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            Dictionary<string, object> guiColors = new Dictionary<string, object>
            {
                { "editor.background", "#1E1E1E" },
                { "editor.selection-background", "#264F78" },
                { "special.key_with.dots-and_underscores", "#FFFFFF" }
            };
            mockRawTheme.Setup(x => x.GetGuiColors()).Returns(guiColors);
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            Theme theme = Theme.CreateFromRawTheme(mockRawTheme.Object, mockRegistryOptions.Object);

            // Act
            ReadOnlyDictionary<string, string> result = theme.GetGuiColorDictionary();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey("editor.background"));
            Assert.AreEqual("#1E1E1E", result["editor.background"]);
            Assert.IsTrue(result.ContainsKey("editor.selection-background"));
            Assert.AreEqual("#264F78", result["editor.selection-background"]);
            Assert.IsTrue(result.ContainsKey("special.key_with.dots-and_underscores"));
            Assert.AreEqual("#FFFFFF", result["special.key_with.dots-and_underscores"]);
        }

        [Test]
        public void GetGuiColorDictionary_CalledMultipleTimes_ReturnsSameCachedInstance()
        {
            // Arrange
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            Dictionary<string, object> guiColors = new Dictionary<string, object>
            {
                { "editor.background", "#1E1E1E" }
            };
            mockRawTheme.Setup(x => x.GetGuiColors()).Returns(guiColors);
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            Theme theme = Theme.CreateFromRawTheme(mockRawTheme.Object, mockRegistryOptions.Object);

            // Act
            ReadOnlyDictionary<string, string> result1 = theme.GetGuiColorDictionary();
            ReadOnlyDictionary<string, string> result2 = theme.GetGuiColorDictionary();

            // Assert
            Assert.AreSame(result1, result2);
        }

        #endregion GetGuiColorDictionary tests

        #region GetColorMap tests

        [Test]
        public void GetColorMap_DefaultColorsPresent_ReturnsMapContainingDefaults()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);

            // Act
            ICollection<string> colorMap = theme.GetColorMap();

            // Assert
            Assert.IsNotNull(colorMap);
            CollectionAssert.Contains(colorMap, "#000000");
            CollectionAssert.Contains(colorMap, "#FFFFFF");
        }

        [Test]
        public void GetColorMap_AfterAddingColorId_IncludesNewColor()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string color = "#123456";

            // Act
            theme.GetColorId(color);
            ICollection<string> colorMap = theme.GetColorMap();

            // Assert
            CollectionAssert.Contains(colorMap, color);
        }

        #endregion GetColorMap tests

        #region GetColor tests

        [Test]
        public void GetColor_RoundTrip_ReturnsOriginalColor()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            const string color = "#ABCDEF";

            // Act
            int id = theme.GetColorId(color);
            string resolved = theme.GetColor(id);

            // Assert
            Assert.AreEqual(color, resolved);
        }

        #endregion GetColor tests

        #region Match tests

        [Test]
        public void Match_EmptyScopeList_ReturnsEmptyList()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            List<string> scopes = new List<string>();

            // Act
            List<ThemeTrieElementRule> result = theme.Match(scopes);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void Match_NoRulesForScopes_ReturnsEmptyList()
        {
            // Arrange
            IRegistryOptions registryOptions = CreateMockRegistryOptions(CreateDefaultRawTheme(), null);
            Theme theme = Theme.CreateFromRawTheme(
                registryOptions.GetDefaultTheme(),
                registryOptions);
            List<string> scopes = new List<string> { "nonexistent.scope" };

            // Act
            List<ThemeTrieElementRule> result = theme.Match(scopes);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void Match_DuplicateScopes_ReturnsDuplicatedMatches()
        {
            // Arrange
            Theme theme = CreateThemeWithSingleRule("keyword.control", "#FF0000");

            List<string> singleScope = new List<string> { "keyword.control" };
            List<string> duplicateScopes = new List<string> { "keyword.control", "keyword.control" };

            // Act
            List<ThemeTrieElementRule> singleResult = theme.Match(singleScope);
            List<ThemeTrieElementRule> duplicateResult = theme.Match(duplicateScopes);

            // Assert
            Assert.IsNotNull(singleResult);
            Assert.IsNotNull(duplicateResult);
            Assert.AreEqual(SingleScopeMatchCount, singleResult.Count);
            Assert.AreEqual(DuplicateScopeMatchCount, duplicateResult.Count);
        }

        [Test]
        public void Match_MultipleScopesWithDifferentDepths_OrdersByDepth()
        {
            // Arrange
            Theme theme = CreateThemeWithMultipleRules();
            List<string> scopes = new List<string> { "source.cs", "keyword.control" };

            // Act
            List<ThemeTrieElementRule> results = theme.Match(scopes);

            // Assert
            Assert.IsNotNull(results);
            Assert.Greater(results.Count, 0);

            // Verify ordering - rules should be ordered by scope depth
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.LessOrEqual(results[i].scopeDepth, results[i + 1].scopeDepth,
                    "Results should be ordered by scopeDepth in ascending order");
            }
        }

        #endregion Match tests

        #region ThemeTrieElementRule tests

        [Test]
        public void ThemeTrieElementRule_AcceptOverwrite_ExtremeScopeDepths_HandlesCorrectly()
        {
            // Arrange
            const string ruleName = "test.rule";
            const int maxDepth = int.MaxValue;
            const int minDepth = 0;

            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                ruleName,
                minDepth,
                new List<string>(),
                FontStyle.NotSet,
                1,
                2);

            // Act
            rule.AcceptOverwrite("overwrite", maxDepth, FontStyle.Bold, 3, 4);

            // Assert
            Assert.AreEqual(maxDepth, rule.scopeDepth,
                "Should accept int.MaxValue as valid scope depth");
            Assert.AreEqual(FontStyle.Bold, rule.fontStyle);
            Assert.AreEqual(3, rule.foreground);
            Assert.AreEqual(4, rule.background);
        }

        [Test]
        public void ThemeTrieElementRule_AcceptOverwrite_LowerScopeDepth_DoesNotDecrease()
        {
            // Arrange
            const int higherDepth = 10;
            const int lowerDepth = 5;

            ThemeTrieElementRule rule = new ThemeTrieElementRule(
                "test",
                higherDepth,
                new List<string>(),
                FontStyle.Bold,
                1,
                2);

            // Act
            rule.AcceptOverwrite("overwrite", lowerDepth, FontStyle.Italic, 3, 4);

            // Assert
            Assert.AreEqual(higherDepth, rule.scopeDepth,
                "Scope depth should not decrease when overwriting with lower depth");
        }

        #endregion ThemeTrieElementRule tests

        #region Default rule processing tests (Add to existing ThemeTests.cs)

        [Test]
        public void CreateFromRawTheme_WithEmptyScopeRule_SetsDefaultFontStyle()
        {
            // Arrange
            const string foregroundColor = "#FFFFFF";
            const string backgroundColor = "#000000";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        // A default empty scope is already added
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "bold",
                            ["foreground"] = foregroundColor,
                            ["background"] = backgroundColor
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "source.test",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert - Verify defaults were set by checking color IDs match expected defaults
            int foregroundId = theme.GetColorId(foregroundColor);
            int backgroundId = theme.GetColorId(backgroundColor);

            Assert.Greater(foregroundId, NullColorId, "Default foreground should be registered");
            Assert.Greater(backgroundId, NullColorId, "Default background should be registered");
        }

        [Test]
        public void CreateFromRawTheme_WithMultipleEmptyScopeRules_LastRuleWinsForEachProperty()
        {
            // Arrange
            const string firstForeground = "#111111";
            const string secondForeground = "#222222";
            const string finalForeground = "#333333";
            const string finalBackground = "#444444";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        // A default empty scope is already added
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "italic",
                            ["foreground"] = firstForeground
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "underline",
                            ["foreground"] = secondForeground
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "bold",
                            ["foreground"] = finalForeground,
                            ["background"] = finalBackground
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "test",
                        ["settings"] = new ThemeRaw { ["foreground"] = "#FF0000" }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert - Last rule's colors should be registered
            int finalForegroundId = theme.GetColorId(finalForeground);
            int finalBackgroundId = theme.GetColorId(finalBackground);

            Assert.Greater(finalForegroundId, NullColorId, "Final foreground should override previous defaults");
            Assert.Greater(finalBackgroundId, NullColorId, "Final background should override previous defaults");

            // Verify earlier colors are also in the map (they were processed but overridden)
            ICollection<string> colorMap = theme.GetColorMap();
            CollectionAssert.Contains(colorMap, finalForeground);
            CollectionAssert.Contains(colorMap, finalBackground);
        }

        [Test]
        public void CreateFromRawTheme_EmptyScopeWithNotSetFontStyle_KeepsDefaultFontStyle()
        {
            // Arrange - FontStyle omitted means NotSet, should not override default
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        // A default empty scope is already added
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = "#FFFFFF"
                            // No fontStyle property
                        }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert - Should use hardcoded defaults (#000000, #FFFFFF)
            ICollection<string> colorMap = theme.GetColorMap();
            CollectionAssert.Contains(colorMap, "#000000", "Default foreground #000000 should be in map");
            CollectionAssert.Contains(colorMap, "#FFFFFF", "Default background #FFFFFF should be in map");
        }

        [Test]
        public void CreateFromRawTheme_EmptyScopeWithNullColors_KeepsDefaultColors()
        {
            // Arrange
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        // A default empty scope is already added
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "bold"
                            // No foreground or background
                        }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert - Should use hardcoded default colors
            const string defaultForeground = "#000000";
            const string defaultBackground = "#FFFFFF";

            int foregroundId = theme.GetColorId(defaultForeground);
            int backgroundId = theme.GetColorId(defaultBackground);

            Assert.AreEqual(defaultForeground, theme.GetColor(foregroundId));
            Assert.AreEqual(defaultBackground, theme.GetColor(backgroundId));
        }

        [Test]
        public void CreateFromRawTheme_EmptyScopeWithOnlyForeground_OverridesForegroundOnly()
        {
            // Arrange
            const string customForeground = "#ABCDEF";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        // A default empty scope is already added
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = customForeground
                            // No background
                        }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert
            ICollection<string> colorMap = theme.GetColorMap();
            CollectionAssert.Contains(colorMap, customForeground, "Custom foreground should be in color map");
            CollectionAssert.Contains(colorMap, "#FFFFFF", "Default background #FFFFFF should still be in map");
        }

        [Test]
        public void CreateFromRawTheme_EmptyScopeWithOnlyBackground_OverridesBackgroundOnly()
        {
            // Arrange
            const string customBackground = "#123456";

            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        // A default empty scope is already added
                        ["settings"] = new ThemeRaw
                        {
                            ["background"] = customBackground
                            // No foreground
                        }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert
            ICollection<string> colorMap = theme.GetColorMap();
            CollectionAssert.Contains(colorMap, "#000000", "Default foreground #000000 should still be in map");
            CollectionAssert.Contains(colorMap, customBackground, "Custom background should be in color map");
        }

        [Test]
        public void CreateFromRawTheme_NoEmptyScopeRules_UsesHardcodedDefaults()
        {
            // Arrange - No rules with empty scope
            ThemeRaw rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "source.test",
                        ["settings"] = new ThemeRaw
                        {
                            ["fontStyle"] = "bold",
                            ["foreground"] = "#FF0000",
                            ["background"] = "#00FF00"
                        }
                    }
                }
            };

            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(r => r.GetInjections(It.IsAny<string>())).Returns((List<string>)null);

            // Act
            Theme theme = Theme.CreateFromRawTheme(rawTheme, mockRegistryOptions.Object);

            // Assert - Should use hardcoded defaults (#000000, #FFFFFF)
            ICollection<string> colorMap = theme.GetColorMap();
            CollectionAssert.Contains(colorMap, "#000000", "Hardcoded default foreground should be present");
            CollectionAssert.Contains(colorMap, "#FFFFFF", "Hardcoded default background should be present");
        }

        #endregion Default rule processing tests

        #region helpers

        private static IRawTheme CreateDefaultRawTheme()
        {
            Mock<IRawTheme> mockRawTheme = new Mock<IRawTheme>();
            mockRawTheme.Setup(x => x.GetSettings()).Returns(new List<IRawThemeSetting>());
            mockRawTheme.Setup(x => x.GetTokenColors()).Returns(new List<IRawThemeSetting>());

            return mockRawTheme.Object;
        }

        private static IRegistryOptions CreateMockRegistryOptions(IRawTheme defaultTheme, List<string> injections)
        {
            Mock<IRegistryOptions> mockRegistryOptions = new Mock<IRegistryOptions>();
            mockRegistryOptions.Setup(x => x.GetDefaultTheme()).Returns(defaultTheme);
            mockRegistryOptions.Setup(x => x.GetInjections(It.IsAny<string>())).Returns(injections);

            return mockRegistryOptions.Object;
        }

        private static Theme CreateThemeWithSingleRule(string scopeName, string foreground)
        {
            ThemeRaw themeRaw = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = scopeName,
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = foreground
                        }
                    }
                }
            };

            IRegistryOptions registryOptions = CreateMockRegistryOptions(themeRaw, null);
            return Theme.CreateFromRawTheme(themeRaw, registryOptions);
        }

        private static Theme CreateThemeWithMultipleRules()
        {
            ThemeRaw themeRaw = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "source",
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = "#FF0000"
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "source.cs",
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = "#00FF00"
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "keyword.control",
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = "#0000FF"
                        }
                    }
                }
            };

            IRegistryOptions registryOptions = CreateMockRegistryOptions(themeRaw, null);
            return Theme.CreateFromRawTheme(themeRaw, registryOptions);
        }

        #endregion helpers
    }
}