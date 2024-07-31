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

        [Test]
        public void TMModel_Should_Emit_ModelTokensChangedEvent_To_Clean_Highlighted_Lines_When_Setting_A_Null_Grammar_After_Having_Another_Grammar()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "line 1", "line 2", "line 3" });

            TMModel tmModel = new TMModel(modelLines);

            Mock<IModelTokensChangedListener> changesListenerMock = new Mock<IModelTokensChangedListener>(
                MockBehavior.Strict);
            changesListenerMock.Setup(
                c => c.ModelTokensChanged(It.IsAny<ModelTokensChangedEvent>()));

            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            IGrammar grammar = registry.LoadGrammar("source.cs");

            tmModel.SetGrammar(grammar);

            tmModel.AddModelTokensChangedListener(changesListenerMock.Object);
            tmModel.SetGrammar(null);

            // verify the three lines were invalidated
            changesListenerMock.Verify(c => c.ModelTokensChanged(
                It.Is<ModelTokensChangedEvent>(e => IsRangeValid(e, 0, 2))),
                Times.Once());

        }

        static bool IsRangeValid(ModelTokensChangedEvent e, int fromLine, int toLine)
        {
            if (e.Ranges.Count != 1)
                return false;

            if (e.Ranges[0].FromLineNumber != fromLine)
                return false;

            if (e.Ranges[0].ToLineNumber != toLine)
                return false;

            return true;
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