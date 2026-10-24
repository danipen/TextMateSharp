using System;
using System.Text;
using GregsEx;

namespace TextMateSharp.Internal.Oniguruma
{
    // A capture of a match: one array of these per match. The default value is no capture.
    public readonly struct OnigCaptureIndex
    {
        public OnigCaptureIndex(int index, int start, int end)
        {
            Index = index;
            Start = start;
            End = end;
            IsDefined = true;
        }

        public int Index { get; }

        public int Start { get; }

        public int End { get; }

        public int Length => End - Start;

        public bool IsDefined { get; }

        public override string ToString() => $"{{\"index\": {Index}, \"start\": {Start}, \"end\": {End}, \"length\": {Length}}}";
    }

    // Capture arrays reused across the searches of one tokenizer: Rent hands out exact-length arrays, Reset makes them
    // all available again. Not thread-safe.
    public sealed class OnigCapturePool
    {
        private System.Collections.Generic.List<OnigCaptureIndex[]>[] _byLength = new System.Collections.Generic.List<OnigCaptureIndex[]>[16];
        private int[] _used = new int[16];
        private readonly System.Collections.Generic.List<int> _touched = new System.Collections.Generic.List<int>();

        public OnigCaptureIndex[] Rent(int length)
        {
            if (length >= _byLength.Length)
            {
                // Grammars such as C#'s have patterns with a hundred groups and more.
                int size = Math.Max(length + 1, 2 * _byLength.Length);
                Array.Resize(ref _byLength, size);
                Array.Resize(ref _used, size);
            }

            var arrays = _byLength[length] ??= new System.Collections.Generic.List<OnigCaptureIndex[]>();
            if (_used[length] == 0)
            {
                _touched.Add(length);
            }

            if (_used[length] == arrays.Count)
            {
                arrays.Add(new OnigCaptureIndex[length]);
            }

            return arrays[_used[length]++];
        }

        public void Reset()
        {
            foreach (int length in _touched)
            {
                _used[length] = 0;
            }
            _touched.Clear();
        }
    }

    public interface IOnigNextMatchResult
    {
        int GetIndex();

        OnigCaptureIndex[] GetCaptureIndices();
    }

    // Searches the regexes of a rule from one position with GregsEx's Oniguruma flavor, with the semantics of
    // Onigwrap's OnigScanner: the earliest match wins, the first regex on a tie, each regex caches its last result
    // for string-backed input, groups that did not participate are reported as (0, 0), and an invalid regex throws
    // when the search reaches it.
    public class OnigScanner
    {
        // Grammars hold patterns far longer than GregsEx's default ceiling (objective-c's platform list is 282k units):
        // Oniguruma's own parse depth limit, and room for those.
        private static readonly OnigurumaCompileOptions Options = new OnigurumaCompileOptions(
            new RegexCompileLimits(maxPatternUnits: 1 << 22, maxNestingDepth: 4096, maxInstructions: 1 << 24,
                maxClassRanges: 1 << 20, maxCaptureGroups: 32767, maxCompileWork: 1L << 32, maxPlanBytes: 1L << 30,
                maxCompileMemoryBytes: 1L << 30));

        private readonly OnigurumaScanner _scanner;
        // The first invalid regex and its source, or -1.
        private readonly int _invalidIndex = -1;
        private readonly string _invalidSource;


        public OnigScanner(string[] regexps)
        {
            var patterns = new OnigurumaPattern[regexps.Length];
            for (int i = 0; i < regexps.Length; i++)
            {
                string source = Preprocess(regexps[i]);
                if (!OnigurumaPattern.TryParse(source, RegexProfiles.OnigurumaTextMate, out var pattern, out _, Options))
                {
                    if (_invalidIndex < 0)
                    {
                        _invalidIndex = i;
                        _invalidSource = source;
                    }

                    pattern = OnigurumaPattern.Parse("(?!)", RegexProfiles.OnigurumaTextMate);
                }

                patterns[i] = pattern;
            }

            _scanner = new OnigurumaScanner(patterns);
        }

        public IOnigNextMatchResult FindNextMatchSync(string source, in int charOffset)
        {
            return FindNextMatchSync(source.AsMemory(), charOffset);
        }

        public IOnigNextMatchResult FindNextMatchSync(ReadOnlyMemory<char> source, in int charOffset)
        {
            bool found = _scanner.FindNextMatch(source, charOffset, out int index);
            if (_invalidIndex >= 0 && (!found || index > _invalidIndex || _scanner.Captures[0].Index > charOffset))
            {
                // Onigwrap searches the regexes in order and stops at a match at the offset.
                throw new ArgumentException($"Invalid regular expression: {_invalidSource}");
            }

            if (!found)
            {
                return null;
            }

            var captures = _scanner.Captures;
            var indices = new OnigCaptureIndex[captures.Length];
            for (int i = 0; i < captures.Length; i++)
            {
                RegexCapture c = captures[i];
                indices[i] = c.Participated ? new OnigCaptureIndex(i, c.Index, c.Index + c.Length) : new OnigCaptureIndex(i, 0, 0);
            }

            return new OnigNextMatchResult(index, indices);
        }

        // Finds the next match into an array rented from pool: valid until pool.Reset().
        public bool FindNextMatch(ReadOnlyMemory<char> source, int charOffset, OnigCapturePool pool, out int index, out OnigCaptureIndex[] captureIndices)
        {
            bool found = _scanner.FindNextMatch(source, charOffset, out index);
            if (_invalidIndex >= 0 && (!found || index > _invalidIndex || _scanner.Captures[0].Index > charOffset))
            {
                throw new ArgumentException($"Invalid regular expression: {_invalidSource}");
            }

            if (!found)
            {
                captureIndices = null;
                return false;
            }

            var captures = _scanner.Captures;
            captureIndices = pool.Rent(captures.Length);
            for (int i = 0; i < captures.Length; i++)
            {
                RegexCapture c = captures[i];
                captureIndices[i] = c.Participated ? new OnigCaptureIndex(i, c.Index, c.Index + c.Length) : new OnigCaptureIndex(i, 0, 0);
            }
            return true;
        }

        // Onigwrap's UnicodeCharEscape: \x followed by 2 to 8 hex digits becomes \x{...} (Oniguruma reads \xHH as an
        // encoded byte, which is not a UTF-16 character), whatever precedes the backslash; \x{7fffffff} is clamped.
        internal static string Preprocess(string pattern)
        {
            if (pattern.IndexOf("\\x", StringComparison.Ordinal) < 0)
            {
                return pattern;
            }

            var builder = new StringBuilder(pattern.Length + 8);
            int i = 0;
            while (i < pattern.Length)
            {
                if (pattern[i] == '\\' && i + 1 < pattern.Length && pattern[i + 1] == 'x')
                {
                    int digits = 0;
                    while (digits < 8 && i + 2 + digits < pattern.Length && IsHex(pattern[i + 2 + digits]))
                    {
                        digits++;
                    }

                    if (digits >= 2)
                    {
                        builder.Append("\\x{").Append(pattern, i + 2, digits).Append('}');
                        i += 2 + digits;
                        continue;
                    }
                }

                builder.Append(pattern[i]);
                i++;
            }

            return builder.ToString().Replace("\\x{7fffffff}", "\\x{7ffff}");
        }

        private static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        private sealed class OnigNextMatchResult : IOnigNextMatchResult
        {
            private readonly int _index;
            private readonly OnigCaptureIndex[] _captureIndices;

            internal OnigNextMatchResult(int index, OnigCaptureIndex[] captureIndices)
            {
                _index = index;
                _captureIndices = captureIndices;
            }

            public int GetIndex() => _index;

            public OnigCaptureIndex[] GetCaptureIndices() => _captureIndices;
        }
    }
}
