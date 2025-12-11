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
    }
}
