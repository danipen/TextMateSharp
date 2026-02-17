using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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

        [Test]
        public async Task TMModel_Dispose_Should_Be_NonBlocking()
        {
            // arrange
            await using Stream stream = ResourceReader.OpenStream("sample.cs");
            using StreamReader reader = new StreamReader(stream);

            ModelLinesMock modelLines = new ModelLinesMock(reader.ReadToEnd().Split("\n"));
            TMModel tmModel = new TMModel(modelLines);

            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            IGrammar grammar = registry.LoadGrammar("source.cs");

            TaskCompletionSource<bool> firstCallback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CountingListener listener = new CountingListener(firstCallback);

            tmModel.SetGrammar(grammar);
            tmModel.AddModelTokensChangedListener(listener);

            try
            {
                Task completed = await Task.WhenAny(firstCallback.Task, Task.Delay(5000));
                Assert.AreSame(firstCallback.Task, completed, "Timed out waiting for first tokenization callback.");

                // act
                Stopwatch stopwatch = Stopwatch.StartNew();
                tmModel.Dispose();
                stopwatch.Stop();

                // assert
                const int maxDisposeTimeMs = 250;   // threshold (in milliseconds) for considering Dispose() effectively non-blocking
                Assert.Less(stopwatch.ElapsedMilliseconds, maxDisposeTimeMs, "Dispose() should be best-effort and non-blocking.");
            }
            finally
            {
                // Cleanup, Dispose is expected to be idempotent
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Dispose_Should_Be_Idempotent()
        {
            // arrange
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "line 1" });
            TMModel tmModel = new TMModel(modelLines);

            // act
            tmModel.Dispose();

            // assert
            Assert.DoesNotThrow(() => tmModel.Dispose());
        }

        [Test]
        public async Task TMModel_Should_Stop_Emitting_After_Last_Listener_Removed()
        {
            // arrange
            await using Stream stream = ResourceReader.OpenStream("sample.cs");
            using StreamReader reader = new StreamReader(stream);

            ModelLinesMock modelLines = new ModelLinesMock(reader.ReadToEnd().Split("\n"));
            TMModel tmModel = new TMModel(modelLines);

            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            IGrammar grammar = registry.LoadGrammar("source.cs");

            TaskCompletionSource<bool> firstCallback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CountingListener listener = new CountingListener(firstCallback);

            tmModel.SetGrammar(grammar);
            tmModel.AddModelTokensChangedListener(listener);

            try
            {
                Task completed = await Task.WhenAny(firstCallback.Task, Task.Delay(5000));
                Assert.AreSame(firstCallback.Task, completed, "Timed out waiting for first tokenization callback.");

                int countAtRemove = listener.CallbackCount;

                // act
                tmModel.RemoveModelTokensChangedListener(listener);

                const int delayBetweenInvalidationsMs = 10;
                const int maxInvalidations = 50;
                for (int invalidationCount = 0; invalidationCount < maxInvalidations; invalidationCount++)
                {
                    tmModel.InvalidateLine(0);
                    await Task.Delay(delayBetweenInvalidationsMs);
                }

                await Task.Delay(250);

                int countAfter = listener.CallbackCount;

                // assert
                Assert.LessOrEqual(
                    countAfter,
                    countAtRemove + 1,
                    "At most one in-flight callback is acceptable after listener removal; additional callbacks indicate tokenization continued.");
            }
            finally
            {
                // Cleanup, Dispose is expected to be idempotent
                tmModel.Dispose();
            }
        }

        [Test]
        public async Task TMModel_SetGrammar_FlipLoop_Should_Not_Deadlock_And_Should_Emit_ClearEvents()
        {
            // arrange
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "line 1", "line 2", "line 3" });
            TMModel tmModel = new TMModel(modelLines);

            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            IGrammar grammar = registry.LoadGrammar("source.cs");

            TaskCompletionSource<bool> sawClear = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CountingClearListener listener = new CountingClearListener(sawClear);

            tmModel.AddModelTokensChangedListener(listener);

            try
            {
                // act
                const int maxFlipIterations = 50;
                for (int flipCount = 0; flipCount < maxFlipIterations; flipCount++)
                {
                    tmModel.SetGrammar(grammar);
                    tmModel.SetGrammar(null);
                }

                Task completed = await Task.WhenAny(sawClear.Task, Task.Delay(5000));

                // assert
                Assert.AreSame(sawClear.Task, completed, "Timed out waiting for a clear-highlight event after SetGrammar(null).");
                Assert.GreaterOrEqual(listener.ClearEventCount, 1, "Expected at least one clear-highlight event.");
            }
            finally
            {
                // Cleanup, Dispose is expected to be idempotent
                tmModel.Dispose();
            }
        }

        private static bool IsRangeValid(ModelTokensChangedEvent e, int fromLine, int toLine)
        {
            if (e.Ranges.Count != 1)
                return false;

            if (e.Ranges[0].FromLineNumber != fromLine)
                return false;

            if (e.Ranges[0].ToLineNumber != toLine)
                return false;

            return true;
        }

        internal sealed class CountingListener : IModelTokensChangedListener
        {
            private readonly TaskCompletionSource<bool> _firstCallback;
            private int _callbackCount;

            internal CountingListener(TaskCompletionSource<bool> firstCallback)
            {
                _firstCallback = firstCallback;
            }

            internal int CallbackCount
            {
                get { return Volatile.Read(ref _callbackCount); }
            }

            void IModelTokensChangedListener.ModelTokensChanged(ModelTokensChangedEvent e)
            {
                Interlocked.Increment(ref _callbackCount);
                _firstCallback.TrySetResult(true);
            }
        }

        internal sealed class CountingClearListener : IModelTokensChangedListener
        {
            // This test creates a 3-line model; the "clear highlight" event is expected to cover the full document range.
            private const int FirstLineIndex = 0;
            private const int LastLineIndex = 2;

            private readonly TaskCompletionSource<bool> _sawClear;
            private int _clearEventCount;

            internal CountingClearListener(TaskCompletionSource<bool> sawClear)
            {
                _sawClear = sawClear;
            }

            internal int ClearEventCount
            {
                get { return Volatile.Read(ref _clearEventCount); }
            }

            void IModelTokensChangedListener.ModelTokensChanged(ModelTokensChangedEvent e)
            {
                if (e.Ranges.Count == 1
                    && e.Ranges[0].FromLineNumber == FirstLineIndex
                    && e.Ranges[0].ToLineNumber == LastLineIndex)
                {
                    Interlocked.Increment(ref _clearEventCount);
                    _sawClear.TrySetResult(true);
                }
            }
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

            public override LineText GetLineTextIncludingTerminators(int lineIndex)
            {
                return _lines[lineIndex] + Environment.NewLine;
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