using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TextMateSharp.Grammars;
using TextMateSharp.Model;

namespace TextMateSharp.Tests.Model
{
    [TestFixture]
    internal class TokenizerTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

        [Test]
        public void Tokenizer_Should_Not_Decode_While_Another_Thread_Decodes_On_The_Same_DecodeMap()
        {
            // arrange
            SignallingGrammar grammar = new SignallingGrammar(LoadGrammar());
            Tokenizer tokenizer = new Tokenizer(grammar);
            HoldingComparer holdingComparer = HoldingComparer.InstallIn(tokenizer);

            // act
            Task<LineTokens> firstRun = Task.Run(() => Tokenize(tokenizer, "int a;"));
            Assert.IsTrue(holdingComparer.FirstCallerHeld.Wait(TestTimeout), "the first run never reached the map");

            // An empty line's only token has no scope to look up: its decode reads and writes PrevToken and calls
            // GetToken without reaching the comparer, so only a lock over the whole decode holds it back
            Task<LineTokens> secondRun = Task.Run(() => Tokenize(tokenizer, ""));
            Assert.IsTrue(grammar.SecondCallReturned.Wait(TestTimeout), "the second run never reached its decode");

            // A correct tokenizer holds the second run back, so this wait always runs to its end there
            bool secondRunDecoded = secondRun.Wait(TimeSpan.FromMilliseconds(200));
            holdingComparer.ReleaseFirstCaller.Set();

            Assert.IsTrue(Task.WaitAll(new Task[] { firstRun, secondRun }, TestTimeout), "the runs never finished");

            // assert
            Assert.IsFalse(secondRunDecoded, "the second run decoded while the first was inside the DecodeMap");

            Tokenizer sequential = new Tokenizer(LoadGrammar());
            AssertSameTokens(Tokenize(sequential, "int a;"), firstRun.Result);
            AssertSameTokens(Tokenize(sequential, ""), secondRun.Result);
        }

        [Test]
        public void Tokenizer_Should_Not_Read_A_Tokens_Scopes_While_Holding_The_DecodeMap()
        {
            // arrange
            ScopesProbeGrammar grammar = new ScopesProbeGrammar();
            Tokenizer tokenizer = new Tokenizer(grammar);
            grammar.DecodeMap = HoldingComparer.DecodeMapOf(tokenizer);

            // act
            LineTokens result = Tokenize(tokenizer, "a");

            // assert
            Assert.AreEqual(1, result.Tokens.Count);
            Assert.IsTrue(grammar.ScopesRead, "the token's scopes were never read");

            // IToken is public, so its getter may take a lock a host holds while it calls the tokenizer
            Assert.IsFalse(grammar.ScopesReadInsideTheLock, "the token's scopes were read while the DecodeMap was locked");
        }

        private static LineTokens Tokenize(Tokenizer tokenizer, string line)
        {
            return tokenizer.Tokenize(line + "\n", null, 0, 10000, TestTimeout);
        }

        private static void AssertSameTokens(LineTokens expected, LineTokens actual)
        {
            Assert.AreEqual(expected.Tokens.Count, actual.Tokens.Count);
            for (int i = 0; i < expected.Tokens.Count; i++)
            {
                Assert.AreEqual(expected.Tokens[i].StartIndex, actual.Tokens[i].StartIndex);
                CollectionAssert.AreEqual(expected.Tokens[i].Scopes, actual.Tokens[i].Scopes);
            }
        }

        private static IGrammar LoadGrammar()
        {
            Registry.Registry registry = new Registry.Registry(new RegistryOptions(ThemeName.DarkPlus));
            return registry.LoadGrammar("source.cs");
        }

        // The only code a decode calls that a test can replace: the first caller to hash a scope stops here,
        // inside the decode, until the test releases it.
        private sealed class HoldingComparer : IEqualityComparer<string>
        {
            internal readonly ManualResetEventSlim FirstCallerHeld = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim ReleaseFirstCaller = new ManualResetEventSlim();
            private int _held;

            internal static object DecodeMapOf(Tokenizer tokenizer)
            {
                return typeof(Tokenizer).GetField("_decodeMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tokenizer);
            }

            internal static HoldingComparer InstallIn(Tokenizer tokenizer)
            {
                object decodeMap = DecodeMapOf(tokenizer);

                HoldingComparer comparer = new HoldingComparer();
                typeof(DecodeMap).GetField("_scopeToTokenIds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(
                    decodeMap, new Dictionary<string, int[]>(comparer));
                return comparer;
            }

            public bool Equals(string x, string y)
            {
                return string.Equals(x, y);
            }

            public int GetHashCode(string obj)
            {
                if (Interlocked.Exchange(ref _held, 1) == 0)
                {
                    FirstCallerHeld.Set();
                    ReleaseFirstCaller.Wait(TestTimeout);
                }

                return obj.GetHashCode();
            }
        }

        // Signals when the grammar has returned for the second time: that run is then past the grammar's lock and
        // about to decode
        private sealed class SignallingGrammar : IGrammar
        {
            internal readonly ManualResetEventSlim SecondCallReturned = new ManualResetEventSlim();
            private readonly IGrammar _grammar;
            private int _calls;

            internal SignallingGrammar(IGrammar grammar)
            {
                _grammar = grammar;
            }

            public bool IsCompiling => _grammar.IsCompiling;
            public string GetName() => _grammar.GetName();
            public string GetScopeName() => _grammar.GetScopeName();
            public ICollection<string> GetFileTypes() => _grammar.GetFileTypes();
            public ITokenizeLineResult TokenizeLine(LineText lineText) => _grammar.TokenizeLine(lineText);
            public ITokenizeLineResult2 TokenizeLine2(LineText lineText) => _grammar.TokenizeLine2(lineText);

            public ITokenizeLineResult2 TokenizeLine2(LineText lineText, IStateStack prevState, TimeSpan timeLimit)
            {
                return _grammar.TokenizeLine2(lineText, prevState, timeLimit);
            }

            public ITokenizeLineResult TokenizeLine(LineText lineText, IStateStack prevState, TimeSpan timeLimit)
            {
                ITokenizeLineResult result = _grammar.TokenizeLine(lineText, prevState, timeLimit);
                if (Interlocked.Increment(ref _calls) == 2)
                    SecondCallReturned.Set();
                return result;
            }
        }

        // Returns one token whose Scopes getter records whether the calling thread holds the DecodeMap's lock
        private sealed class ScopesProbeGrammar : IGrammar, ITokenizeLineResult, IToken
        {
            internal object DecodeMap;
            internal bool ScopesRead;
            internal bool ScopesReadInsideTheLock;

            public bool IsCompiling => false;
            public string GetName() => "probe";
            public string GetScopeName() => "source.probe";
            public ICollection<string> GetFileTypes() => new List<string>();
            public ITokenizeLineResult TokenizeLine(LineText lineText) => this;
            public ITokenizeLineResult TokenizeLine(LineText lineText, IStateStack prevState, TimeSpan timeLimit) => this;
            public ITokenizeLineResult2 TokenizeLine2(LineText lineText) => throw new NotSupportedException();

            public ITokenizeLineResult2 TokenizeLine2(LineText lineText, IStateStack prevState, TimeSpan timeLimit)
            {
                throw new NotSupportedException();
            }

            public IToken[] Tokens => new IToken[] { this };
            public IStateStack RuleStack => null;

            public int StartIndex { get; set; }
            public int EndIndex => 1;
            public int Length => 1;

            public List<string> Scopes
            {
                get
                {
                    ScopesRead = true;
                    ScopesReadInsideTheLock |= Monitor.IsEntered(DecodeMap);
                    return new List<string> { "source.probe" };
                }
            }
        }
    }
}
