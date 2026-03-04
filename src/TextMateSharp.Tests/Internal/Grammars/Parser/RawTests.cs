using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Tests.Internal.Grammars.Parser
{
    [TestFixture]
    public class RawTests
    {
        #region Merge tests

        [Test]
        public void Merge_SingleSource_ReturnsAllKeys()
        {
            // arrange
            Raw source = new Raw
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
            Raw target = new Raw();

            // act
            IRawRepository result = target.Merge(source);

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(2, resultRaw.Count);
            Assert.AreEqual("value1", resultRaw["key1"]);
            Assert.AreEqual("value2", resultRaw["key2"]);
        }

        [Test]
        public void Merge_MultipleSources_MergesAllKeys()
        {
            // arrange
            Raw source1 = new Raw { ["key1"] = "value1" };
            Raw source2 = new Raw { ["key2"] = "value2" };
            Raw source3 = new Raw { ["key3"] = "value3" };
            Raw target = new Raw();

            // act
            IRawRepository result = target.Merge(source1, source2, source3);

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(3, resultRaw.Count);
            Assert.AreEqual("value1", resultRaw["key1"]);
            Assert.AreEqual("value2", resultRaw["key2"]);
            Assert.AreEqual("value3", resultRaw["key3"]);
        }

        [Test]
        public void Merge_OverlappingKeys_LastSourceWins()
        {
            // arrange
            Raw source1 = new Raw { ["key"] = "first" };
            Raw source2 = new Raw { ["key"] = "second" };
            Raw target = new Raw();

            // act
            IRawRepository result = target.Merge(source1, source2);

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual("second", resultRaw["key"]);
        }

        [Test]
        public void Merge_EmptySources_ReturnsEmptyTarget()
        {
            // arrange
            Raw target = new Raw();

            // act
            IRawRepository result = target.Merge();

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(0, resultRaw.Count);
        }

        #endregion Merge tests

        #region Property getter/setter tests

        [Test]
        public void GetProp_ExistingKey_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw();
            Raw propValue = new Raw();
            raw["testProp"] = propValue;

            // act
            IRawRule result = raw.GetProp("testProp");

            // assert
            Assert.AreSame(propValue, result);
        }

        [Test]
        public void GetProp_NonExistingKey_ReturnsNull()
        {
            // arrange
            Raw raw = new Raw();

            // act
            IRawRule result = raw.GetProp("nonExistent");

            // assert
            Assert.IsNull(result);
        }

        [Test]
        public void SetBase_GetBase_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();
            Raw baseRule = new Raw();

            // act
            raw.SetBase(baseRule);
            IRawRule result = raw.GetBase();

            // assert
            Assert.AreSame(baseRule, result);
        }

        [Test]
        public void SetSelf_GetSelf_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();
            Raw selfRule = new Raw();

            // act
            raw.SetSelf(selfRule);
            IRawRule result = raw.GetSelf();

            // assert
            Assert.AreSame(selfRule, result);
        }

        [Test]
        public void SetId_GetId_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();
            RuleId id = RuleId.Of(42);

            // act
            raw.SetId(id);
            RuleId result = raw.GetId();

            // assert
            Assert.AreEqual(id, result);
        }

        [Test]
        public void SetName_GetName_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();

            // act
            // TryGetObject is private, but we can test it indirectly through public methods
            string initialResult = raw.GetName();
            raw.SetName("test value");
            string result = raw.GetName();

            // assert
            Assert.IsNull(initialResult);
            Assert.AreEqual("test value", result);
        }

        [Test]
        public void GetContentName_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["contentName"] = "test.content" };

            // act
            string result = raw.GetContentName();

            // assert
            Assert.AreEqual("test.content", result);
        }

        [Test]
        public void GetMatch_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["match"] = "\\w+" };

            // act
            string result = raw.GetMatch();

            // assert
            Assert.AreEqual("\\w+", result);
        }

        [Test]
        public void GetBegin_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["begin"] = "^\\s*" };

            // act
            string result = raw.GetBegin();

            // assert
            Assert.AreEqual("^\\s*", result);
        }

        [Test]
        public void GetEnd_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["end"] = "$" };

            // act
            string result = raw.GetEnd();

            // assert
            Assert.AreEqual("$", result);
        }

        [Test]
        public void GetWhile_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["while"] = "\\S" };

            // act
            string result = raw.GetWhile();

            // assert
            Assert.AreEqual("\\S", result);
        }

        [Test]
        public void SetInclude_GetInclude_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();
            const string include = "#source";

            // act
            raw.SetInclude(include);
            string result = raw.GetInclude();

            // assert
            Assert.AreEqual(include, result);
        }

        [Test]
        public void GetScopeName_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["scopeName"] = "source.test" };

            // act
            string result = raw.GetScopeName();

            // assert
            Assert.AreEqual("source.test", result);
        }

        [Test]
        public void GetInjectionSelector_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["injectionSelector"] = "L:source.js" };

            // act
            string result = raw.GetInjectionSelector();

            // assert
            Assert.AreEqual("L:source.js", result);
        }

        [Test]
        public void GetFirstLineMatch_ExistingValue_ReturnsValue()
        {
            // arrange
            Raw raw = new Raw { ["firstLineMatch"] = "^#!/bin/bash" };

            // act
            string result = raw.GetFirstLineMatch();

            // assert
            Assert.AreEqual("^#!/bin/bash", result);
        }

        #endregion Property getter/setter tests

        #region Captures tests

        [Test]
        public void GetCaptures_RawCaptures_ReturnsAsIs()
        {
            // arrange
            Raw captures = new Raw { ["1"] = new Raw() };
            Raw raw = new Raw { ["captures"] = captures };

            // act
            IRawCaptures result = raw.GetCaptures();

            // assert
            Assert.AreSame(captures, result);
        }

        [Test]
        public void GetCaptures_ListCaptures_ConvertsToRaw_AndMapsObjectsCorrectly()
        {
            // arrange
            Raw capture1 = new Raw { ["foo"] = "bar" };
            Raw capture2 = new Raw { ["baz"] = "qux" };
            List<object> capturesList = new List<object> { capture1, capture2 };
            Raw raw = new Raw { ["captures"] = capturesList };

            // act
            IRawCaptures result = raw.GetCaptures();

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(2, resultRaw.Count);
            Assert.IsTrue(resultRaw.ContainsKey("1"));
            Assert.IsTrue(resultRaw.ContainsKey("2"));
            Assert.AreSame(capture1, resultRaw["1"]);
            Assert.AreSame(capture2, resultRaw["2"]);
        }

        [Test]
        public void GetCaptures_NonExistent_ReturnsNull()
        {
            // arrange
            Raw raw = new Raw();

            // act
            IRawCaptures result = raw.GetCaptures();

            // assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetBeginCaptures_RawCaptures_ReturnsAsIs()
        {
            // arrange
            Raw captures = new Raw { ["0"] = new Raw() };
            Raw raw = new Raw { ["beginCaptures"] = captures };

            // act
            IRawCaptures result = raw.GetBeginCaptures();

            // assert
            Assert.AreSame(captures, result);
        }

        [Test]
        public void GetBeginCaptures_ListCaptures_ConvertsToRaw()
        {
            // arrange
            Raw capture1 = new Raw();
            Raw capture2 = new Raw();
            Raw capture3 = new Raw();
            List<object> capturesList = new List<object> { capture1, capture2, capture3 };
            Raw raw = new Raw { ["beginCaptures"] = capturesList };

            // act
            IRawCaptures result = raw.GetBeginCaptures();

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(3, resultRaw.Count);
            Assert.IsTrue(resultRaw.ContainsKey("1"));
            Assert.IsTrue(resultRaw.ContainsKey("2"));
            Assert.IsTrue(resultRaw.ContainsKey("3"));
            Assert.AreSame(capture1, resultRaw["1"]);
            Assert.AreSame(capture2, resultRaw["2"]);
            Assert.AreSame(capture3, resultRaw["3"]);
        }

        [Test]
        public void SetBeginCaptures_GetBeginCaptures_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();
            Raw captures = new Raw { ["1"] = new Raw() };

            // act
            raw.SetBeginCaptures(captures);
            IRawCaptures result = raw.GetBeginCaptures();

            // assert
            Assert.AreSame(captures, result);
        }

        [Test]
        public void GetEndCaptures_RawCaptures_ReturnsAsIs()
        {
            // arrange
            Raw captures = new Raw { ["0"] = new Raw() };
            Raw raw = new Raw { ["endCaptures"] = captures };

            // act
            IRawCaptures result = raw.GetEndCaptures();

            // assert
            Assert.AreSame(captures, result);
        }

        [Test]
        public void GetEndCaptures_ListCaptures_ConvertsToRaw_AndMapsObjectsCorrectly()
        {
            // arrange
            Raw capture1 = new Raw { ["foo"] = "bar" };
            Raw capture2 = new Raw { ["baz"] = "qux" };
            List<object> capturesList = new List<object> { capture1, capture2 };
            Raw raw = new Raw { ["endCaptures"] = capturesList };

            // act
            IRawCaptures result = raw.GetEndCaptures();

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(2, resultRaw.Count);
            Assert.IsTrue(resultRaw.ContainsKey("1"));
            Assert.IsTrue(resultRaw.ContainsKey("2"));
            Assert.AreSame(capture1, resultRaw["1"]);
            Assert.AreSame(capture2, resultRaw["2"]);
        }

        [Test]
        public void GetWhileCaptures_ListCaptures_ConvertsToRaw()
        {
            // arrange
            List<object> capturesList = new List<object> { new Raw(), new Raw() };
            Raw raw = new Raw { ["whileCaptures"] = capturesList };

            // act
            IRawCaptures result = raw.GetWhileCaptures();

            // assert
            Raw resultRaw = (Raw)result;
            Assert.AreEqual(2, resultRaw.Count);
        }

        [Test]
        public void GetCapture_ExistingCaptureId_ReturnsCapture()
        {
            // arrange
            Raw capture = new Raw();
            Raw raw = new Raw { ["1"] = capture };

            // act
            IRawRule result = raw.GetCapture("1");

            // assert
            Assert.AreSame(capture, result);
        }

        #endregion Captures tests

        #region Patterns tests

        [Test]
        public void GetPatterns_ExistingList_ReturnsCollection()
        {
            // arrange
            List<object> patternsList = new List<object> { new Raw(), new Raw() };
            Raw raw = new Raw { ["patterns"] = patternsList };

            // act
            ICollection<IRawRule> result = raw.GetPatterns();

            // assert
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetPatterns_NonExistent_ReturnsNull()
        {
            // arrange
            Raw raw = new Raw();

            // act
            ICollection<IRawRule> result = raw.GetPatterns();

            // assert
            Assert.IsNull(result);
        }

        [Test]
        public void SetPatterns_GetPatterns_ReturnsSetPatterns()
        {
            // arrange
            Raw raw = new Raw();
            List<IRawRule> patterns = new List<IRawRule> { new Raw(), new Raw() };

            // act
            raw.SetPatterns(patterns);
            ICollection<IRawRule> result = raw.GetPatterns();

            // assert
            Assert.AreEqual(2, result.Count);
        }

        #endregion Patterns tests

        #region Injections tests

        [Test]
        public void GetInjections_ExistingRaw_ReturnsDictionary()
        {
            // arrange
            Raw injectionsRaw = new Raw
            {
                ["L:source.js"] = new Raw(),
                ["L:text.html"] = new Raw()
            };
            Raw raw = new Raw { ["injections"] = injectionsRaw };

            // act
            Dictionary<string, IRawRule> result = raw.GetInjections();

            // assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey("L:source.js"));
            Assert.IsTrue(result.ContainsKey("L:text.html"));
        }

        [Test]
        public void GetInjections_NonExistent_ReturnsNull()
        {
            // arrange
            Raw raw = new Raw();

            // act
            Dictionary<string, IRawRule> result = raw.GetInjections();

            // assert
            Assert.IsNull(result);
        }

        #endregion Injections tests

        #region Repository tests

        [Test]
        public void SetRepository_GetRepository_ReturnsSetValue()
        {
            // arrange
            Raw raw = new Raw();
            Raw repository = new Raw { ["rule1"] = new Raw() };

            // act
            raw.SetRepository(repository);
            IRawRepository result = raw.GetRepository();

            // assert
            Assert.AreSame(repository, result);
        }

        [Test]
        public void GetRepository_NonExistent_ReturnsNull()
        {
            // arrange
            Raw raw = new Raw();

            // act
            IRawRepository result = raw.GetRepository();

            // assert
            Assert.IsNull(result);
        }

        #endregion Repository tests

        #region IsApplyEndPatternLast tests

        [Test]
        public void IsApplyEndPatternLast_BoolTrue_ReturnsTrue()
        {
            // arrange
            Raw raw = new Raw { ["applyEndPatternLast"] = true };

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsApplyEndPatternLast_BoolFalse_ReturnsFalse()
        {
            // arrange
            Raw raw = new Raw { ["applyEndPatternLast"] = false };

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsApplyEndPatternLast_IntOne_ReturnsTrue()
        {
            // arrange
            Raw raw = new Raw { ["applyEndPatternLast"] = 1 };

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsApplyEndPatternLast_IntZero_ReturnsFalse()
        {
            // arrange
            Raw raw = new Raw { ["applyEndPatternLast"] = 0 };

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsApplyEndPatternLast_IntOther_ReturnsFalse()
        {
            // arrange
            Raw raw = new Raw { ["applyEndPatternLast"] = 42 };

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsApplyEndPatternLast_NonExistent_ReturnsFalse()
        {
            // arrange
            Raw raw = new Raw();

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsApplyEndPatternLast_StringValue_ReturnsFalse()
        {
            // arrange
            Raw raw = new Raw { ["applyEndPatternLast"] = "true" };

            // act
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void SetApplyEndPatternLast_IsApplyEndPatternLast_ReturnsTrue()
        {
            // arrange
            Raw raw = new Raw();

            // act
            raw.SetApplyEndPatternLast(true);
            bool result = raw.IsApplyEndPatternLast();

            // assert
            Assert.IsTrue(result);
        }

        #endregion IsApplyEndPatternLast tests

        #region GetFileTypes tests

        [Test]
        public void GetFileTypes_WithLeadingDots_TrimsDotsAndCaches()
        {
            // arrange
            List<object> fileTypes = new List<object> { ".js", ".ts", ".jsx" };
            Raw raw = new Raw { ["fileTypes"] = fileTypes };

            // act
            ICollection<string> result1 = raw.GetFileTypes();
            ICollection<string> result2 = raw.GetFileTypes();

            // assert
            Assert.AreEqual(3, result1.Count);
            Assert.IsTrue(result1.Contains("js"));
            Assert.IsTrue(result1.Contains("ts"));
            Assert.IsTrue(result1.Contains("jsx"));
            Assert.AreSame(result1, result2); // Cached
        }

        [Test]
        public void GetFileTypes_WithoutLeadingDots_ReturnsAsIs()
        {
            // arrange
            List<object> fileTypes = new List<object> { "js", "ts" };
            Raw raw = new Raw { ["fileTypes"] = fileTypes };

            // act
            ICollection<string> result = raw.GetFileTypes();

            // assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("js"));
            Assert.IsTrue(result.Contains("ts"));
        }

        [Test]
        public void GetFileTypes_NonExistent_ReturnsEmptyList()
        {
            // arrange
            Raw raw = new Raw();

            // act
            ICollection<string> result = raw.GetFileTypes();

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetFileTypes_EmptyList_ReturnsEmptyList()
        {
            // arrange
            Raw raw = new Raw { ["fileTypes"] = new List<object>() };

            // act
            ICollection<string> result = raw.GetFileTypes();

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetFileTypes_MixedDots_CorrectlyProcessesEach()
        {
            // arrange
            List<object> fileTypes = new List<object> { ".cs", "vb", ".fs" };
            Raw raw = new Raw { ["fileTypes"] = fileTypes };

            // act
            ICollection<string> result = raw.GetFileTypes();

            // assert
            List<string> resultList = result.ToList();
            Assert.AreEqual("cs", resultList[0]);
            Assert.AreEqual("vb", resultList[1]);
            Assert.AreEqual("fs", resultList[2]);
        }

        #endregion GetFileTypes tests

        #region Clone tests

        [Test]
        public void Clone_SimpleRaw_CreatesDeepCopy()
        {
            // arrange
            Raw original = new Raw
            {
                ["name"] = "test",
                ["value"] = 42
            };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            Assert.AreNotSame(original, clonedRaw);
            Assert.AreEqual("test", clonedRaw["name"]);
            Assert.AreEqual(42, clonedRaw["value"]);
        }

        [Test]
        public void Clone_NestedRaw_CreatesDeepCopy()
        {
            // arrange
            Raw nested = new Raw { ["inner"] = "value" };
            Raw original = new Raw { ["nested"] = nested };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            Raw clonedNested = (Raw)clonedRaw["nested"];
            Assert.AreNotSame(original, clonedRaw);
            Assert.AreNotSame(nested, clonedNested);
            Assert.AreEqual("value", clonedNested["inner"]);
        }

        [Test]
        public void Clone_WithList_CreatesDeepCopyOfList()
        {
            // arrange
            List<object> list = new List<object> { "item1", 123 };
            Raw original = new Raw { ["list"] = list };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            List<object> clonedList = (List<object>)clonedRaw["list"];
            Assert.AreNotSame(list, clonedList);
            Assert.AreEqual(2, clonedList.Count);
            Assert.AreEqual("item1", clonedList[0]);
            Assert.AreEqual(123, clonedList[1]);
        }

        [Test]
        public void Clone_WithNestedList_CreatesDeepCopy()
        {
            // arrange
            Raw innerRaw = new Raw { ["key"] = "value" };
            List<object> list = new List<object> { innerRaw };
            Raw original = new Raw { ["list"] = list };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            List<object> clonedList = (List<object>)clonedRaw["list"];
            Raw clonedInner = (Raw)clonedList[0];
            Assert.AreNotSame(innerRaw, clonedInner);
            Assert.AreEqual("value", clonedInner["key"]);
        }

        [Test]
        public void Clone_WithBool_CopiesValue()
        {
            // arrange
            Raw original = new Raw { ["flag"] = true };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            Assert.AreEqual(true, clonedRaw["flag"]);
        }

        [Test]
        public void Clone_WithString_PreservesStringReference()
        {
            // arrange
            const string str = "test string";
            Raw original = new Raw { ["str"] = str };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            Assert.AreSame(str, clonedRaw["str"]); // Strings are immutable
        }

        [Test]
        public void Clone_WithInt_CopiesValue()
        {
            // arrange
            Raw original = new Raw { ["num"] = 42 };

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            Assert.AreEqual(42, clonedRaw["num"]);
        }

        [Test]
        public void Clone_EmptyRaw_CreatesEmptyClone()
        {
            // arrange
            Raw original = new Raw();

            // act
            IRawGrammar cloned = original.Clone();

            // assert
            Raw clonedRaw = (Raw)cloned;
            Assert.AreNotSame(original, clonedRaw);
            Assert.AreEqual(0, clonedRaw.Count);
        }

        [Test]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            // arrange
            Raw original = new Raw { ["key"] = "original" };
            IRawGrammar cloned = original.Clone();

            // act
            ((Raw)cloned)["key"] = "modified";

            // assert
            Assert.AreEqual("original", original["key"]);
            Assert.AreEqual("modified", ((Raw)cloned)["key"]);
        }

        #endregion Clone tests

        #region IEnumerable tests

        [Test]
        public void GetEnumerator_String_EnumeratesKeys()
        {
            // arrange
            Raw raw = new Raw
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
                ["key3"] = "value3"
            };

            // act
            List<string> keys = new List<string>();
            IEnumerable<string> enumerable = raw;
            // don't use AddRange here, we want to test the enumerator directly
            foreach (string key in enumerable)
            {
                keys.Add(key);
            }

            // assert
            Assert.AreEqual(3, keys.Count);
            Assert.IsTrue(keys.Contains("key1"));
            Assert.IsTrue(keys.Contains("key2"));
            Assert.IsTrue(keys.Contains("key3"));
        }

        #endregion IEnumerable tests
    }
}