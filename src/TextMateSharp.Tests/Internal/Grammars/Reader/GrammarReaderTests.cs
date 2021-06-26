using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Tests.Internal.Parser.Json
{
    class GrammarReaderTests
    {
        [Test]
        public void TestReadGrammar()
        {
            using (Stream ms = GenerateStreamFromString(_json))
            using (StreamReader reader = new StreamReader(ms))
            {
                IRawGrammar grammar = GrammarReader.ReadGrammarSync(reader);

                Assert.IsNotNull(grammar);
                Assert.AreEqual("C#", grammar.GetName());
                Assert.AreEqual("source.cs", grammar.GetScopeName());

                ICollection<IRawRule> patterns = grammar.GetPatterns();

                Assert.AreEqual(2, patterns.Count);

                int i = 0;
                foreach (IRawRule pattern in patterns)
                {
                    switch (i)
                    {
                        case 0:
                            Assert.AreEqual("#preprocessor", pattern.GetInclude());
                            break;

                        case 1:
                            Assert.AreEqual("#comment", pattern.GetInclude());
                            break;
                    }

                    i++;
                }

                IRawRepository repository = grammar.GetRepository();

                IRawRule rule = repository.GetProp("extern-alias-directive");
                Assert.AreEqual("[beginregex]+", rule.GetBegin());
                Assert.AreEqual("[endregexp]+", rule.GetEnd());

                IRawCaptures captures = rule.GetBeginCaptures();

                IRawRule capture1 = captures.GetCapture("1");
                IRawRule capture2 = captures.GetCapture("2");

                Assert.AreEqual(
                    "keyword.other.extern.cs",
                    capture1.GetName());
                Assert.AreEqual(
                    "keyword.other.alias.cs",
                    capture2.GetName());
            }
        }

        static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        const string _json =
            "{" +
            "    'name': 'C#'," +
            "    'scopeName': 'source.cs'," +
            "    'patterns': [" +
            "        {" +
            "            'include': '#preprocessor'" +
            "        }," +
            "        {" +
            "            'include': '#comment'" +
            "        }" +
            "    ]," +
            "    'repository': {" +
            "          'extern-alias-directive': {" +
            "              'begin': '[beginregex]+'," +
            "              'beginCaptures': {" +
            "                  '1': {" +
            "                      'name': 'keyword.other.extern.cs'" +
            "                  }," +
            "                  '2': {" +
            "                      'name': 'keyword.other.alias.cs'" +
            "                  }" +
            "                }," +
            "                'end': '[endregexp]+'" +
            "          }" +
            "    }" +
            "}";

        public int JSONPListParserIRawGrammar { get; private set; }
    }
}
