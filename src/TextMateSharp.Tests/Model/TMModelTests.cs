using Moq;

using NUnit.Framework;

using System.IO;
using System.Threading.Tasks;

using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Tests.Resources;

namespace TextMateSharp.Tests.Model
{
    [TestFixture]
    internal class TmModelTests
    {
        [Test]
        public void TMModel_Should_Parse_Until_Last_Document_Line()
        {
            using var stream = ResourceReader.OpenStream("sample.cs");
            using var reader = new StreamReader(stream);

            ModelLinesMock modelLines = new ModelLinesMock(reader.ReadToEnd().Split("\n"));

            TMModel tmModel = new TMModel(modelLines);
            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);

            IGrammar grammar = registry.LoadGrammar("source.cs");
            tmModel.SetGrammar(grammar);

            ModelTokensChangedListenerMock listenerMock = new ModelTokensChangedListenerMock(
                modelLines.GetNumberOfLines());
            tmModel.AddModelTokensChangedListener(listenerMock);

            while (!listenerMock.Finished)
            {
                Task.Delay(250).Wait();
            }

            Assert.AreEqual(
                modelLines.GetNumberOfLines(),
                listenerMock.LastParsedLine);
        }

        [Test]
        public void TMModel_Should_Not_Parse_Setting_A_Null_Grammar()
        {
            using var stream = ResourceReader.OpenStream("sample.cs");
            using var reader = new StreamReader(stream);

            ModelLinesMock modelLines = new ModelLinesMock(reader.ReadToEnd().Split("\n"));

            TMModel tmModel = new TMModel(modelLines);

            tmModel.SetGrammar(null);

            Mock<IModelTokensChangedListener> changesListenerMock = new Mock<IModelTokensChangedListener>(
                MockBehavior.Strict);

            tmModel.AddModelTokensChangedListener(changesListenerMock.Object);

            Task.Delay(250).Wait();

            changesListenerMock.Verify(c => c.ModelTokensChanged(
                It.IsAny<ModelTokensChangedEvent>()), Times.Never());
        }

        class ModelLinesMock : AbstractLineList
        {
            string[] _lines;
            internal ModelLinesMock(string[] lines)
            {
                _lines = lines;
                for (int i = 0; i < lines.Length; i++)
                    AddLine(i);
            }
            public override void Dispose()
            {

            }
            public override int GetLineLength(int lineIndex)
            {
                return _lines[lineIndex].Length;
            }
            public override string GetLineText(int lineIndex)
            {
                return _lines[lineIndex];
            }
            public override int GetNumberOfLines()
            {
                return _lines.Length;
            }
            public override void UpdateLine(int lineIndex)
            {
                // no op
            }
        }
        internal class ModelTokensChangedListenerMock : IModelTokensChangedListener
        {
            internal volatile bool Finished;
            internal volatile int LastParsedLine;
            private readonly int _lineCount;
            internal ModelTokensChangedListenerMock(int lineCount)
            {
                _lineCount = lineCount;
            }
            void IModelTokensChangedListener.ModelTokensChanged(ModelTokensChangedEvent e)
            {
                foreach (var range in e.Ranges)
                {
                    LastParsedLine = range.ToLineNumber;

                    if (LastParsedLine >= _lineCount)
                    {
                        Finished = true;
                        break;
                    }
                }
            }
        }
    }
}