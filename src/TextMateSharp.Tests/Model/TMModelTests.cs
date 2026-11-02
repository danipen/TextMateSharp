using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        [Test]
        public void TMModel_Should_Tokenize_Lines_After_Grammar_Finishes_Compiling_On_Another_Thread()
        {
            // https://github.com/danipen/TextMateSharp/issues/131
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "if (x) { return 42; }" });
            TMModel tmModel = new TMModel(modelLines);

            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            CompilingGrammar grammar = new CompilingGrammar(registry.LoadGrammar("source.js"));

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                // another thread is compiling the grammar when the model starts tokenizing
                grammar.IsCompiling = true;
                tmModel.SetGrammar(grammar);

                // wait until the tokenizer thread has checked IsCompiling twice: the first
                // wait consumes the signal from SetGrammar, the second one blocks
                Assert.IsTrue(
                    SpinWait.SpinUntil(() => grammar.CompilingChecks >= 2, 3000),
                    "The tokenizer thread did not check IsCompiling.");

                // the compilation ends; nobody invalidates any line
                grammar.IsCompiling = false;

                bool tokenized = SpinWait.SpinUntil(() => tmModel.GetLineTokens(0) != null, 3000);

                Assert.IsTrue(tokenized, "Line 0 was never tokenized after the grammar finished compiling.");
            }
            finally
            {
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_ForceTokenization_Should_Tokenize_Lines_Skipped_While_Grammar_Is_Compiling()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "if (x) { return 42; }" });
            TMModel tmModel = new TMModel(modelLines);

            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            CompilingGrammar grammar = new CompilingGrammar(registry.LoadGrammar("source.js"));

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);

                Assert.IsTrue(
                    SpinWait.SpinUntil(() => !modelLines.Get(0).IsInvalid && !modelLines.Get(1).IsInvalid, 3000),
                    "The lines were not tokenized initially.");

                // the line is invalid but not queued, so only ForceTokenization can tokenize it
                modelLines.Get(1).IsInvalid = true;

                grammar.IsCompiling = true;
                tmModel.ForceTokenization(1);
                grammar.IsCompiling = false;

                bool tokenized = SpinWait.SpinUntil(() => !modelLines.Get(1).IsInvalid, 3000);

                Assert.IsTrue(tokenized, "The line skipped by ForceTokenization while compiling was never tokenized.");
            }
            finally
            {
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Retokenize_A_Line_Edited_While_It_Is_Being_Tokenized([Values] bool invalidateAsRange)
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "var y = 2;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();
            HoldingGrammar grammar = new HoldingGrammar(source);

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);
                WaitUntilTokenized(modelLines);

                // a key: the tokenizer thread starts on its text
                grammar.HoldNextTokenize();
                modelLines.SetLine(0, "var x = 12;");
                tmModel.InvalidateLine(0);
                Assert.IsTrue(grammar.WaitUntilHeld(), "The tokenizer thread did not tokenize the line.");

                // the next key lands before that run stores its tokens
                modelLines.SetLine(0, "var x = 12; // done");
                if (invalidateAsRange)
                    tmModel.InvalidateLineRange(0, 0);
                else
                    tmModel.InvalidateLine(0);
                grammar.Release();

                AssertSettlesOn(tmModel, 0, ExpectedTokens(source, "var x = 12; // done"));
            }
            finally
            {
                grammar.Release();
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Retokenize_A_Later_Line_Of_A_Range_Edited_While_It_Is_Being_Tokenized()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "var y = 2;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();
            HoldingGrammar grammar = new HoldingGrammar(source);

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);
                WaitUntilTokenized(modelLines);

                // a key on the second line: the tokenizer thread starts on its text
                grammar.HoldNextTokenize();
                modelLines.SetLine(1, "var y = 23;");
                tmModel.InvalidateLine(1);
                Assert.IsTrue(grammar.WaitUntilHeld(), "The tokenizer thread did not tokenize the line.");

                // the next edit lands before that run stores, and invalidates a range the line is not first in
                modelLines.SetLine(1, "var y = 23; // done");
                tmModel.InvalidateLineRange(0, 1);
                grammar.Release();

                AssertSettlesOn(tmModel, 1, ExpectedTokens(source, "var y = 23; // done"));
            }
            finally
            {
                grammar.Release();
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Retokenize_A_Line_Edited_Just_After_Its_Text_Was_Read()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "var y = 2;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(source);
                WaitUntilTokenized(modelLines);

                modelLines.AfterNextTextRead = () =>
                {
                    modelLines.SetLine(0, "var x = 12; // done");
                    tmModel.InvalidateLine(0);
                };
                modelLines.SetLine(0, "var x = 12;");
                tmModel.InvalidateLine(0);

                AssertSettlesOn(tmModel, 0, ExpectedTokens(source, "var x = 12; // done"));
            }
            finally
            {
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Show_The_Tokens_Of_An_Edited_Line_Until_Its_Retokenization_Ends()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "var y = 2;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();
            HoldingGrammar grammar = new HoldingGrammar(source);

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);
                WaitUntilTokenized(modelLines);

                grammar.HoldNextTokenize();
                modelLines.SetLine(0, "var x = 12;");
                tmModel.InvalidateLine(0);
                Assert.IsTrue(grammar.WaitUntilHeld(), "The tokenizer thread did not tokenize the line.");

                modelLines.SetLine(0, "var x = 12; // done");
                tmModel.InvalidateLine(0);

                // the second run is held once the first one has stored its result
                grammar.HoldNextTokenize();
                grammar.Release();
                Assert.IsTrue(grammar.WaitUntilHeld(), "The line was not tokenized again after the edit.");

                Assert.AreEqual(ExpectedTokens(source, "var x = 12;"), Describe(tmModel.GetLineTokens(0)));
                Assert.IsTrue(tmModel.IsLineInvalid(0), "The line must stay invalid until its new text is tokenized.");

                grammar.Release();

                AssertSettlesOn(tmModel, 0, ExpectedTokens(source, "var x = 12; // done"));
            }
            finally
            {
                grammar.Release();
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Recover_The_Next_Line_From_The_End_State_Of_An_Edited_Line()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "var y = 2;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();
            HoldingGrammar grammar = new HoldingGrammar(source);

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);
                WaitUntilTokenized(modelLines);

                // this run ends inside a comment, which the next key removes
                grammar.HoldNextTokenize();
                modelLines.SetLine(0, "/* older");
                tmModel.InvalidateLine(0);
                Assert.IsTrue(grammar.WaitUntilHeld(), "The tokenizer thread did not tokenize the line.");

                modelLines.SetLine(0, "var x = 12;");
                tmModel.InvalidateLine(0);

                // the re-run of line 0 is held once the outdated run has passed its end state on
                grammar.HoldNextTokenize();
                grammar.Release();
                Assert.IsTrue(grammar.WaitUntilHeld(), "The line was not tokenized again after the edit.");

                Tokenizer tokenizer = new Tokenizer(source);
                TMState commentState = tokenizer.Tokenize("/* older" + Environment.NewLine, tokenizer.GetInitialState(), TimeSpan.FromSeconds(3)).EndState;
                Assert.AreEqual(commentState, modelLines.Get(1).State);

                grammar.Release();

                AssertSettlesOn(tmModel, 0, ExpectedTokens(source, "var x = 12;"));
                AssertSettlesOn(tmModel, 1, ExpectedTokens(source, "var y = 2;"));
            }
            finally
            {
                grammar.Release();
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Not_Let_A_Run_On_Older_Text_Replace_The_Result_For_Newer_Text()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var x = 1;", "var y = 2;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();
            HoldingGrammar grammar = new HoldingGrammar(source);

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);
                WaitUntilTokenized(modelLines);

                grammar.HoldNextTokenize();
                modelLines.SetLine(0, "/* older");
                tmModel.InvalidateLine(0);
                Assert.IsTrue(grammar.WaitUntilHeld(), "The tokenizer thread did not tokenize the line.");

                // the newer text is tokenized on this thread while the older run is held
                modelLines.SetLine(0, "var x = 12;");
                tmModel.InvalidateLine(0);
                tmModel.ForceTokenization(0);

                string expected = ExpectedTokens(source, "var x = 12;");
                Assert.AreEqual(expected, Describe(tmModel.GetLineTokens(0)));
                Assert.IsFalse(tmModel.IsLineInvalid(0));

                // only the tokenizer thread clears line 1, and it does so after the held run ends
                tmModel.InvalidateLine(1);
                grammar.Release();
                Assert.IsTrue(
                    SpinWait.SpinUntil(() => !tmModel.IsLineInvalid(1), 3000),
                    "The held run did not end.");

                Assert.AreEqual(expected, Describe(tmModel.GetLineTokens(0)));
                Assert.IsFalse(tmModel.IsLineInvalid(0));
                Assert.AreEqual(ExpectedTokens(source, "var y = 2;"), Describe(tmModel.GetLineTokens(1)));
            }
            finally
            {
                grammar.Release();
                tmModel.Dispose();
            }
        }

        [Test]
        public void TMModel_Should_Not_Store_Tokens_Of_Another_Line_When_A_Line_Is_Inserted_During_A_Wait_For_Its_Lock()
        {
            ModelLinesMock modelLines = new ModelLinesMock(new string[] { "var a;", "var b;", "// c", "var d;", "var e;" });
            TMModel tmModel = new TMModel(modelLines);
            IGrammar source = LoadJavaScriptGrammar();
            HoldingGrammar grammar = new HoldingGrammar(source);
            ManualResetEventSlim atLookup = new ManualResetEventSlim();
            ManualResetEventSlim lockTaken = new ManualResetEventSlim();
            object modelLock = typeof(TMModel).GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tmModel);
            int compilingReads = 0;

            tmModel.AddModelTokensChangedListener(new ModelTokensChangedListenerMock(modelLines.GetNumberOfLines()));

            try
            {
                tmModel.SetGrammar(grammar);
                Assert.IsTrue(
                    SpinWait.SpinUntil(() => Enumerable.Range(0, 5).All(i => !tmModel.IsLineInvalid(i)), 3000),
                    "The lines were not tokenized initially.");

                // read once by the thread's loop, then just before the line is looked up
                grammar.OnIsCompilingRead = () =>
                {
                    if (Interlocked.Increment(ref compilingReads) == 2)
                    {
                        atLookup.Set();
                        lockTaken.Wait(3000);
                    }
                };

                tmModel.InvalidateLine(3);
                Assert.IsTrue(atLookup.Wait(3000), "The tokenizer thread did not reach the line.");

                lock (modelLock)
                {
                    lockTaken.Set();
                    Thread.Sleep(100);
                    modelLines.InsertLine(1, "var n;");
                }
                tmModel.InvalidateLineRange(0, 1);

                // only a run started after this call can clear the flag, and the thread runs one at a time
                tmModel.InvalidateLine(5);
                Assert.IsTrue(
                    SpinWait.SpinUntil(() => !tmModel.IsLineInvalid(5), 3000),
                    "The tokenizer thread did not reach the last line.");

                Assert.AreEqual(ExpectedTokens(source, "var d;"), Describe(tmModel.GetLineTokens(4)));
            }
            finally
            {
                grammar.OnIsCompilingRead = null;
                lockTaken.Set();
                tmModel.Dispose();
            }
        }

        private static IGrammar LoadJavaScriptGrammar()
        {
            RegistryOptions options = new RegistryOptions(ThemeName.DarkPlus);
            Registry.Registry registry = new Registry.Registry(options);
            return registry.LoadGrammar("source.js");
        }

        private static void WaitUntilTokenized(ModelLinesMock modelLines)
        {
            Assert.IsTrue(
                SpinWait.SpinUntil(() => !modelLines.Get(0).IsInvalid && !modelLines.Get(1).IsInvalid, 3000),
                "The lines were not tokenized initially.");
        }

        private static void AssertSettlesOn(TMModel tmModel, int lineIndex, string expected)
        {
            SpinWait.SpinUntil(
                () => !tmModel.IsLineInvalid(lineIndex) && Describe(tmModel.GetLineTokens(lineIndex)) == expected,
                3000);

            Assert.AreEqual(expected, Describe(tmModel.GetLineTokens(lineIndex)));
            Assert.IsFalse(tmModel.IsLineInvalid(lineIndex), "The line was left invalid.");
        }

        // The tokens of a line of text, from a tokenizer of their own, starting from the initial state
        private static string ExpectedTokens(IGrammar grammar, string text)
        {
            Tokenizer tokenizer = new Tokenizer(grammar);
            LineTokens result = tokenizer.Tokenize(text + Environment.NewLine, tokenizer.GetInitialState(), TimeSpan.FromSeconds(3));
            return Describe(result.Tokens);
        }

        private static string Describe(List<TMToken> tokens)
        {
            return string.Join(" | ", tokens.Select(t => t.StartIndex + ":" + string.Join(",", t.Scopes)));
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

        sealed class CompilingGrammar : IGrammar
        {
            private readonly IGrammar _grammar;
            private volatile bool _isCompiling;
            private int _compilingChecks;

            internal CompilingGrammar(IGrammar grammar)
            {
                _grammar = grammar;
            }

            public bool IsCompiling
            {
                get
                {
                    bool isCompiling = _isCompiling;
                    if (isCompiling)
                        Interlocked.Increment(ref _compilingChecks);
                    return isCompiling;
                }
                set { _isCompiling = value; }
            }

            // Number of times IsCompiling was read while true
            internal int CompilingChecks
            {
                get { return Volatile.Read(ref _compilingChecks); }
            }

            public string GetName() => _grammar.GetName();
            public string GetScopeName() => _grammar.GetScopeName();
            public ICollection<string> GetFileTypes() => _grammar.GetFileTypes();
            public ITokenizeLineResult TokenizeLine(LineText lineText) => _grammar.TokenizeLine(lineText);
            public ITokenizeLineResult TokenizeLine(LineText lineText, IStateStack prevState, TimeSpan timeLimit) => _grammar.TokenizeLine(lineText, prevState, timeLimit);
            public ITokenizeLineResult2 TokenizeLine2(LineText lineText) => _grammar.TokenizeLine2(lineText);
            public ITokenizeLineResult2 TokenizeLine2(LineText lineText, IStateStack prevState, TimeSpan timeLimit) => _grammar.TokenizeLine2(lineText, prevState, timeLimit);
        }

        // Holds a tokenize call after the grammar has run, before the model can store the result
        sealed class HoldingGrammar : IGrammar
        {
            private readonly IGrammar _grammar;
            private readonly SemaphoreSlim _held = new SemaphoreSlim(0);
            private readonly SemaphoreSlim _released = new SemaphoreSlim(0);
            private int _holdNext;

            // Runs on the reading thread, on every read of IsCompiling
            internal Action OnIsCompilingRead;

            internal HoldingGrammar(IGrammar grammar)
            {
                _grammar = grammar;
            }

            internal void HoldNextTokenize()
            {
                Volatile.Write(ref _holdNext, 1);
            }

            internal bool WaitUntilHeld()
            {
                return _held.Wait(3000);
            }

            internal void Release()
            {
                _released.Release();
            }

            public ITokenizeLineResult TokenizeLine(LineText lineText, IStateStack prevState, TimeSpan timeLimit)
            {
                ITokenizeLineResult result = _grammar.TokenizeLine(lineText, prevState, timeLimit);
                if (Interlocked.Exchange(ref _holdNext, 0) == 1)
                {
                    _held.Release();
                    _released.Wait(10000);
                }
                return result;
            }

            public bool IsCompiling
            {
                get
                {
                    Volatile.Read(ref OnIsCompilingRead)?.Invoke();
                    return _grammar.IsCompiling;
                }
            }

            public string GetName() => _grammar.GetName();
            public string GetScopeName() => _grammar.GetScopeName();
            public ICollection<string> GetFileTypes() => _grammar.GetFileTypes();
            public ITokenizeLineResult TokenizeLine(LineText lineText) => _grammar.TokenizeLine(lineText);
            public ITokenizeLineResult2 TokenizeLine2(LineText lineText) => _grammar.TokenizeLine2(lineText);
            public ITokenizeLineResult2 TokenizeLine2(LineText lineText, IStateStack prevState, TimeSpan timeLimit) => _grammar.TokenizeLine2(lineText, prevState, timeLimit);
        }

        class ModelLinesMock : AbstractLineList
        {
            string[] _lines;

            // Runs once, on the thread that reads a line's text, after the read
            internal Action AfterNextTextRead;

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
                LineText text = _lines[lineIndex] + Environment.NewLine;
                Interlocked.Exchange(ref AfterNextTextRead, null)?.Invoke();
                return text;
            }

            internal void SetLine(int lineIndex, string text)
            {
                _lines[lineIndex] = text;
            }

            internal void InsertLine(int lineIndex, string text)
            {
                AddLine(lineIndex);
                _lines = _lines.Take(lineIndex).Append(text).Concat(_lines.Skip(lineIndex)).ToArray();
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