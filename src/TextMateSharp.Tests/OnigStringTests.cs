
using System;

using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests
{
    class OnigStringTests
    {
        [Test]
        public void TestUtf8Utf16Conversions()
        {
            OnigString onigString = new OnigString("áé");
            Assert.AreEqual(onigString.utf8_value.Length, 4);
            Assert.AreEqual(onigString._string.Length, 2);
            Assert.AreEqual(onigString.ConvertUtf8OffsetToUtf16(0), 0);
        }

        [Test]
        public void TestUtf8Utf16Conversions2()
        {

            char c1 = 'm';
            char c2 = 'õ';

            int bytes = System.Text.Encoding.UTF8.GetByteCount(c1.ToString());
            int bytes2 = System.Text.Encoding.UTF8.GetByteCount(c2.ToString());

            string str = "myááçóúôõaab";
            OnigString utf8WithCharLen = new OnigString(str);

            Assert.AreEqual(0, utf8WithCharLen.ConvertUtf16OffsetToUtf8(0));
            Assert.AreEqual(1, utf8WithCharLen.ConvertUtf16OffsetToUtf8(1));
            Assert.AreEqual(2, utf8WithCharLen.ConvertUtf16OffsetToUtf8(2));
            Assert.AreEqual(4, utf8WithCharLen.ConvertUtf16OffsetToUtf8(3));
            Assert.AreEqual(6, utf8WithCharLen.ConvertUtf16OffsetToUtf8(4));
            Assert.AreEqual(8, utf8WithCharLen.ConvertUtf16OffsetToUtf8(5));
            Assert.AreEqual(10, utf8WithCharLen.ConvertUtf16OffsetToUtf8(6));
            Assert.AreEqual(12, utf8WithCharLen.ConvertUtf16OffsetToUtf8(7));
            try
            {
                utf8WithCharLen.ConvertUtf16OffsetToUtf8(55);
                Assert.Fail("Expected error");
            }
            catch (Exception e)
            {
            }

            Assert.AreEqual(0, utf8WithCharLen.ConvertUtf8OffsetToUtf16(0));
            Assert.AreEqual(1, utf8WithCharLen.ConvertUtf8OffsetToUtf16(1));
            Assert.AreEqual(2, utf8WithCharLen.ConvertUtf8OffsetToUtf16(2));
            Assert.AreEqual(2, utf8WithCharLen.ConvertUtf8OffsetToUtf16(3));
            Assert.AreEqual(3, utf8WithCharLen.ConvertUtf8OffsetToUtf16(4));
            Assert.AreEqual(3, utf8WithCharLen.ConvertUtf8OffsetToUtf16(5));
            Assert.AreEqual(4, utf8WithCharLen.ConvertUtf8OffsetToUtf16(6));
            Assert.AreEqual(4, utf8WithCharLen.ConvertUtf8OffsetToUtf16(7));
            Assert.AreEqual(5, utf8WithCharLen.ConvertUtf8OffsetToUtf16(8));
            Assert.AreEqual(6, utf8WithCharLen.ConvertUtf8OffsetToUtf16(10));
            Assert.AreEqual(7, utf8WithCharLen.ConvertUtf8OffsetToUtf16(12));
            try
            {
                utf8WithCharLen.ConvertUtf8OffsetToUtf16(55);
                Assert.Fail("Expected error");
            }
            catch (Exception e)
            {
            }

        }
    }
}
