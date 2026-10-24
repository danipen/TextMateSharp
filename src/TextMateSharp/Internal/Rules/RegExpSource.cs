using TextMateSharp.Internal.Oniguruma;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
    internal sealed class RegExpSource
    {

        private static readonly Regex HAS_BACK_REFERENCES = new Regex("\\\\(\\d+)");

        private readonly RuleId _ruleId;
        private bool _hasAnchor;
        private readonly bool _hasBackReferences;
        private RegExpSourceAnchorCache _anchorCache;
        private string _source;

        internal RegExpSource(string regExpSource, RuleId ruleId) :
            this(regExpSource, ruleId, true)
        {
        }

        internal RegExpSource(string regExpSource, RuleId ruleId, bool handleAnchors)
        {
            if (handleAnchors)
            {
                this.HandleAnchors(regExpSource);
            }
            else
            {
                this._source = regExpSource;
                this._hasAnchor = false;
            }

            if (this._hasAnchor)
            {
                this._anchorCache = this.BuildAnchorCache();
            }

            this._ruleId = ruleId;
            this._hasBackReferences = HAS_BACK_REFERENCES.Match(this._source).Success;
        }

        internal RegExpSource Clone()
        {
            return new RegExpSource(this._source, this._ruleId, true);
        }

        internal void SetSource(string newSource)
        {
            if (this._source.Equals(newSource))
            {
                return;
            }
            this._source = newSource;

            if (this._hasAnchor)
            {
                this._anchorCache = this.BuildAnchorCache();
            }
        }

        private void HandleAnchors(string regExpSource)
        {
            if (regExpSource != null)
            {
                int len = regExpSource.Length;
                char ch;
                char nextCh;
                int lastPushedPos = 0;
                StringBuilder output = new StringBuilder();

                bool hasAnchor = false;
                for (int pos = 0; pos < len; pos++)
                {
                    ch = regExpSource[pos];

                    if (ch == '\\')
                    {
                        if (pos + 1 < len)
                        {
                            nextCh = regExpSource[pos + 1];
                            if (nextCh == 'z')
                            {
                                output.Append(regExpSource.SubstringAtIndexes(lastPushedPos, pos));
                                output.Append("$(?!\\n)(?<!\\n)");
                                lastPushedPos = pos + 2;
                            }
                            else if (nextCh == 'A' || nextCh == 'G')
                            {
                                hasAnchor = true;
                            }
                            pos++;
                        }
                    }
                }

                this._hasAnchor = hasAnchor;
                if (lastPushedPos == 0)
                {
                    // No \z hit
                    this._source = regExpSource;
                }
                else
                {
                    output.Append(regExpSource.SubstringAtIndexes(lastPushedPos, len));
                    this._source = output.ToString();
                }
            }
            else
            {
                this._hasAnchor = false;
                this._source = regExpSource;
            }
        }

        internal string ResolveBackReferences(ReadOnlyMemory<char> lineText, OnigCaptureIndex[] captureIndices)
        {
            try
            {
                // The replacement of \\(\d+), without a regex or a string per capture: each backslash followed by
                // digits is replaced by that capture, escaped (by nothing if there is no such capture or the number
                // is not an int).
                string source = this._source;
                StringBuilder builder = null;
                int copied = 0;
                for (int i = source.IndexOf('\\'); i >= 0 && i < source.Length; i++)
                {
                    if (source[i] != '\\')
                    {
                        continue;
                    }

                    int digits = RegexSource.CountDigits(source, i + 1);
                    if (digits == 0)
                    {
                        continue;
                    }

                    builder ??= StringBuilderCache.Acquire(source.Length + 16);
                    builder.Append(source, copied, i - copied);
                    if (TryParseIndex(source, i + 1, digits, out int index) && index < captureIndices.Length)
                    {
                        OnigCaptureIndex captureIndex = captureIndices[index];
                        AppendEscaped(builder, lineText.Span.Slice(captureIndex.Start, captureIndex.End - captureIndex.Start));
                    }
                    copied = i + 1 + digits;
                    i = copied - 1;
                }

                if (builder == null)
                {
                    return source;
                }

                builder.Append(source, copied, source.Length - copied);
                return StringBuilderCache.GetStringAndRelease(builder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return lineText.Span.ToString();
        }

        // int.Parse of ASCII digits: false for other digits or an overflow.
        private static bool TryParseIndex(string source, int start, int length, out int value)
        {
            long result = 0;
            for (int i = start; i < start + length; i++)
            {
                char ch = source[i];
                if (ch < '0' || ch > '9')
                {
                    value = 0;
                    return false;
                }
                result = (result * 10) + (ch - '0');
                if (result > int.MaxValue)
                {
                    value = 0;
                    return false;
                }
            }
            value = (int)result;
            return true;
        }

        private static void AppendEscaped(StringBuilder builder, ReadOnlySpan<char> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                switch (ch)
                {
                    case '-':
                    case '\\':
                    case '{':
                    case '}':
                    case '*':
                    case '+':
                    case '?':
                    case '|':
                    case '^':
                    case '$':
                    case '.':
                    case ',':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case '#':
                        /* escaping white space chars is actually not necessary:
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\f':
                        case '\r':
                        case 0x0B: // vertical tab \v
                        */
                        builder.Append('\\');
                        break;
                }
                builder.Append(ch);
            }
        }

        private RegExpSourceAnchorCache BuildAnchorCache()
        {
            string source = this._source;
            var sourceLen = source.Length;

            StringBuilder A0_G0_result = new StringBuilder(sourceLen);
            StringBuilder A0_G1_result = new StringBuilder(sourceLen);
            StringBuilder A1_G0_result = new StringBuilder(sourceLen);
            StringBuilder A1_G1_result = new StringBuilder(sourceLen);

            int pos;
            int len;
            char ch;
            char nextCh;

            for (pos = 0, len = sourceLen; pos < len; pos++)
            {
                ch = source[pos];
                A0_G0_result.Append(ch);
                A0_G1_result.Append(ch);
                A1_G0_result.Append(ch);
                A1_G1_result.Append(ch);

                if (ch == '\\')
                {
                    if (pos + 1 < len)
                    {
                        nextCh = source[pos + 1];
                        if (nextCh == 'A')
                        {
                            A0_G0_result.Append('\uFFFF');
                            A0_G1_result.Append('\uFFFF');
                            A1_G0_result.Append('A');
                            A1_G1_result.Append('A');
                        }
                        else if (nextCh == 'G')
                        {
                            A0_G0_result.Append('\uFFFF');
                            A0_G1_result.Append('G');
                            A1_G0_result.Append('\uFFFF');
                            A1_G1_result.Append('G');
                        }
                        else
                        {
                            A0_G0_result.Append(nextCh);
                            A0_G1_result.Append(nextCh);
                            A1_G0_result.Append(nextCh);
                            A1_G1_result.Append(nextCh);
                        }
                        pos++;
                    }
                }
            }

            return new RegExpSourceAnchorCache(
                A0_G0_result.ToString(),
                A0_G1_result.ToString(),
                A1_G0_result.ToString(),
                A1_G1_result.ToString());
        }

        internal string ResolveAnchors(bool allowA, bool allowG)
        {
            if (!this._hasAnchor)
                return this._source;

            if (allowA)
            {
                if (allowG)
                    return this._anchorCache.A1_G1;

                return this._anchorCache.A1_G0;
            }

            if (allowG)
                return this._anchorCache.A0_G1;

            return this._anchorCache.A0_G0;
        }

        internal bool HasAnchor()
        {
            return this._hasAnchor;
        }

        internal string GetSource()
        {
            return this._source;
        }

        internal RuleId GetRuleId()
        {
            return this._ruleId;
        }

        internal bool HasBackReferences()
        {
            return this._hasBackReferences;
        }

    }
}