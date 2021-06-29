using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class OnigInteropTests
    {
        [Test]
        public void TestUnicodeCharsWithoutBraces()
        {
            string pattern = "[\\xa0-\\xF7]";

            using (ORegex oRegex = new ORegex(pattern))
            {
                Assert.IsTrue(oRegex.Valid);
            }
        }
    }
}
