using System;

using NUnit.Framework;

using TextMateSharp.Grammars;

namespace TextMateSharp.Tests.Grammar
{
    [TestFixture]
    public class LineTextTests
    {
        [Test]
        public void Constructor_WithString_ShouldStoreText()
        {
            LineText lineText = new LineText("hello world");

            Assert.AreEqual(11, lineText.Length);
            Assert.AreEqual("hello world", lineText.ToString());
        }

        [Test]
        public void Constructor_WithNullString_ShouldBeEmpty()
        {
            LineText lineText = new LineText((string)null);

            Assert.IsTrue(lineText.IsEmpty);
            Assert.AreEqual(0, lineText.Length);
        }

        [Test]
        public void Constructor_WithReadOnlyMemory_ShouldStoreText()
        {
            ReadOnlyMemory<char> memory = "hello world".AsMemory();
            LineText lineText = new LineText(memory);

            Assert.AreEqual(11, lineText.Length);
            Assert.AreEqual("hello world", lineText.ToString());
        }

        [Test]
        public void Constructor_WithEmptyMemory_ShouldBeEmpty()
        {
            LineText lineText = new LineText(ReadOnlyMemory<char>.Empty);

            Assert.IsTrue(lineText.IsEmpty);
            Assert.AreEqual(0, lineText.Length);
        }

        [Test]
        public void ImplicitConversion_FromString_ShouldWork()
        {
            LineText lineText = "test string";

            Assert.AreEqual("test string", lineText.ToString());
            Assert.AreEqual(11, lineText.Length);
        }

        [Test]
        public void ImplicitConversion_FromReadOnlyMemory_ShouldWork()
        {
            ReadOnlyMemory<char> memory = "test memory".AsMemory();
            LineText lineText = memory;

            Assert.AreEqual("test memory", lineText.ToString());
            Assert.AreEqual(11, lineText.Length);
        }

        [Test]
        public void ImplicitConversion_ToReadOnlyMemory_ShouldWork()
        {
            LineText lineText = "test";
            ReadOnlyMemory<char> memory = lineText;

            Assert.AreEqual(4, memory.Length);
            Assert.AreEqual("test", memory.Span.ToString());
        }

        [Test]
        public void Memory_Property_ShouldReturnUnderlyingMemory()
        {
            LineText lineText = "hello";

            ReadOnlyMemory<char> memory = lineText.Memory;

            Assert.AreEqual(5, memory.Length);
            Assert.AreEqual('h', memory.Span[0]);
            Assert.AreEqual('o', memory.Span[4]);
        }

        [Test]
        public void IsEmpty_WithEmptyString_ShouldReturnTrue()
        {
            LineText lineText = "";

            Assert.IsTrue(lineText.IsEmpty);
        }

        [Test]
        public void IsEmpty_WithNonEmptyString_ShouldReturnFalse()
        {
            LineText lineText = "x";

            Assert.IsFalse(lineText.IsEmpty);
        }

        [Test]
        public void Default_LineText_ShouldBeEmpty()
        {
            LineText lineText = default;

            Assert.IsTrue(lineText.IsEmpty);
            Assert.AreEqual(0, lineText.Length);
        }

        [Test]
        public void ToString_ShouldReturnStringRepresentation()
        {
            LineText lineText = "hello world";

            Assert.AreEqual("hello world", lineText.ToString());
        }

        [Test]
        public void SlicedMemory_ShouldWorkCorrectly()
        {
            char[] buffer = "hello world".ToCharArray();
            ReadOnlyMemory<char> sliced = buffer.AsMemory().Slice(6, 5);
            LineText lineText = sliced;

            Assert.AreEqual("world", lineText.ToString());
            Assert.AreEqual(5, lineText.Length);
        }

        [Test]
        public void UnicodeText_ShouldBeHandledCorrectly()
        {
            LineText lineText = "안녕하세요";

            Assert.AreEqual(5, lineText.Length);
            Assert.AreEqual("안녕하세요", lineText.ToString());
        }

        [Test]
        public void CharArrayMemory_ShouldWorkWithLineText()
        {
            char[] buffer = new char[] { 'a', 'b', 'c', 'd', 'e' };
            LineText lineText = (ReadOnlyMemory<char>)buffer.AsMemory();

            Assert.AreEqual(5, lineText.Length);
            Assert.AreEqual("abcde", lineText.ToString());
        }

        #region Equals Tests

        [Test]
        public void Equals_SameContent_ShouldBeEqual()
        {
            LineText lineText1 = "hello world";
            LineText lineText2 = "hello world";

            Assert.IsTrue(lineText1.Equals(lineText2));
            Assert.IsTrue(lineText2.Equals(lineText1));
        }

        [Test]
        public void Equals_DifferentContent_ShouldNotBeEqual()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "world";

            Assert.IsFalse(lineText1.Equals(lineText2));
            Assert.IsFalse(lineText2.Equals(lineText1));
        }

        [Test]
        public void Equals_DifferentLengths_ShouldNotBeEqual()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "hello world";

            Assert.IsFalse(lineText1.Equals(lineText2));
            Assert.IsFalse(lineText2.Equals(lineText1));
        }

        [Test]
        public void Equals_BothEmpty_ShouldBeEqual()
        {
            LineText lineText1 = "";
            LineText lineText2 = "";

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_BothDefault_ShouldBeEqual()
        {
            LineText lineText1 = default;
            LineText lineText2 = default;

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_EmptyAndDefault_ShouldBeEqual()
        {
            LineText lineText1 = "";
            LineText lineText2 = default;

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_SameMemoryReference_ShouldBeEqual()
        {
            char[] buffer = "hello world".ToCharArray();
            ReadOnlyMemory<char> memory = buffer.AsMemory();

            LineText lineText1 = memory;
            LineText lineText2 = memory;

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_SameArraySameOffset_ShouldUseReferenceEquality()
        {
            char[] buffer = "hello world".ToCharArray();
            ReadOnlyMemory<char> slice1 = buffer.AsMemory().Slice(0, 5);
            ReadOnlyMemory<char> slice2 = buffer.AsMemory().Slice(0, 5);

            LineText lineText1 = slice1;
            LineText lineText2 = slice2;

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_SameArrayDifferentOffsetsSameContent_ShouldBeEqual()
        {
            // Create buffer with repeated content
            char[] buffer = "hellohello".ToCharArray();
            ReadOnlyMemory<char> slice1 = buffer.AsMemory().Slice(0, 5);  // "hello"
            ReadOnlyMemory<char> slice2 = buffer.AsMemory().Slice(5, 5);  // "hello"

            LineText lineText1 = slice1;
            LineText lineText2 = slice2;

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_DifferentArraysSameContent_ShouldBeEqual()
        {
            string buffer1 = "hello";
            string buffer2 = "hello";

            LineText lineText1 = buffer1.AsMemory();
            LineText lineText2 = buffer2.AsMemory();

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_ObjectOverload_WithLineText_ShouldWork()
        {
            LineText lineText1 = "hello";
            object lineText2 = (LineText)"hello";

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_ObjectOverload_WithNull_ShouldReturnFalse()
        {
            LineText lineText = "hello";

            Assert.IsFalse(lineText.Equals(null));
        }

        [Test]
        public void Equals_ObjectOverload_WithDifferentType_ShouldReturnFalse()
        {
            LineText lineText = "hello";
            Assert.IsFalse(lineText.Equals(42));
        }

        [Test]
        public void OperatorEquals_SameContent_ShouldReturnTrue()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "hello";

            Assert.IsTrue(lineText1 == lineText2);
        }

        [Test]
        public void OperatorEquals_DifferentContent_ShouldReturnFalse()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "world";

            Assert.IsFalse(lineText1 == lineText2);
        }

        [Test]
        public void OperatorNotEquals_SameContent_ShouldReturnFalse()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "hello";

            Assert.IsFalse(lineText1 != lineText2);
        }

        [Test]
        public void OperatorNotEquals_DifferentContent_ShouldReturnTrue()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "world";

            Assert.IsTrue(lineText1 != lineText2);
        }

        [Test]
        public void Equals_UnicodeContent_ShouldWork()
        {
            LineText lineText1 = "안녕하세요";
            LineText lineText2 = "안녕하세요";

            Assert.IsTrue(lineText1.Equals(lineText2));
        }

        [Test]
        public void Equals_CaseSensitive_ShouldNotBeEqual()
        {
            LineText lineText1 = "Hello";
            LineText lineText2 = "hello";

            Assert.IsFalse(lineText1.Equals(lineText2));
        }

        #endregion

        #region GetHashCode Tests

        [Test]
        public void GetHashCode_SameContent_ShouldReturnSameHash()
        {
            LineText lineText1 = "hello world";
            LineText lineText2 = "hello world";

            Assert.AreEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentContent_ShouldReturnDifferentHash()
        {
            LineText lineText1 = "hello";
            LineText lineText2 = "world";

            Assert.AreNotEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
        }

        [Test]
        public void GetHashCode_EmptyLineText_ShouldReturnZero()
        {
            LineText lineText = "";

            Assert.AreEqual(0, lineText.GetHashCode());
        }

        [Test]
        public void GetHashCode_DefaultLineText_ShouldReturnZero()
        {
            LineText lineText = default;

            Assert.AreEqual(0, lineText.GetHashCode());
        }

        [Test]
        public void GetHashCode_SameInstance_ShouldBeConsistent()
        {
            LineText lineText = "hello world";

            int hash1 = lineText.GetHashCode();
            int hash2 = lineText.GetHashCode();
            int hash3 = lineText.GetHashCode();

            Assert.AreEqual(hash1, hash2);
            Assert.AreEqual(hash2, hash3);
        }

        [Test]
        public void GetHashCode_DifferentArraysSameContent_ShouldReturnSameHash()
        {
            string buffer1 = "hello";
            string buffer2 = "hello";

            LineText lineText1 = buffer1.AsMemory();
            LineText lineText2 = buffer2.AsMemory();

            Assert.AreEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
        }

        [Test]
        public void GetHashCode_SlicedMemorySameContent_ShouldReturnSameHash()
        {
            char[] buffer = "hello world".ToCharArray();
            ReadOnlyMemory<char> slice = buffer.AsMemory().Slice(6, 5); // "world"

            LineText lineText1 = slice;
            LineText lineText2 = "world";

            Assert.AreEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
        }

        [Test]
        public void GetHashCode_UnicodeContent_ShouldWork()
        {
            LineText lineText1 = "안녕하세요";
            LineText lineText2 = "안녕하세요";

            Assert.AreEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
        }

        [Test]
        public void GetHashCode_SimilarStrings_ShouldProduceDifferentHashes()
        {
            // These are similar but should have different hashes
            LineText lineText1 = "abc";
            LineText lineText2 = "abd";
            LineText lineText3 = "bbc";

            Assert.AreNotEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
            Assert.AreNotEqual(lineText1.GetHashCode(), lineText3.GetHashCode());
            Assert.AreNotEqual(lineText2.GetHashCode(), lineText3.GetHashCode());
        }

        [Test]
        public void GetHashCode_SingleCharacter_ShouldWork()
        {
            LineText lineText1 = "a";
            LineText lineText2 = "a";
            LineText lineText3 = "b";

            Assert.AreEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
            Assert.AreNotEqual(lineText1.GetHashCode(), lineText3.GetHashCode());
        }

        #endregion

        #region HashCode and Equals Contract Tests

        [Test]
        public void HashCodeEqualsContract_EqualObjects_ShouldHaveSameHashCode()
        {
            // If two objects are equal, they must have the same hash code
            LineText lineText1 = "test string";
            LineText lineText2 = "test string";

            Assert.IsTrue(lineText1.Equals(lineText2));
            Assert.AreEqual(lineText1.GetHashCode(), lineText2.GetHashCode());
        }

        [Test]
        public void HashCodeEqualsContract_WorksWithDictionary()
        {
            var dictionary = new System.Collections.Generic.Dictionary<LineText, int>();

            LineText key1 = "hello";
            dictionary[key1] = 42;

            LineText key2 = "hello"; // Different instance, same content
            Assert.IsTrue(dictionary.ContainsKey(key2));
            Assert.AreEqual(42, dictionary[key2]);
        }

        [Test]
        public void HashCodeEqualsContract_WorksWithHashSet()
        {
            var hashSet = new System.Collections.Generic.HashSet<LineText>();

            LineText item1 = "hello";
            hashSet.Add(item1);

            LineText item2 = "hello"; // Different instance, same content
            Assert.IsTrue(hashSet.Contains(item2));
            Assert.IsFalse(hashSet.Add(item2)); // Should return false as it already exists
        }

        #endregion
    }
}
