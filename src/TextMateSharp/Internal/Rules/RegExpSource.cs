using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
    public class RegExpSource
    {

        private static Regex HAS_BACK_REFERENCES = new Regex("\\\\(\\d+)");
        private static Regex BACK_REFERENCING_END = new Regex("\\\\(\\d+)");

        private int? _ruleId;
        private bool _hasAnchor;
        private bool _hasBackReferences;
        private RegExpSourceAnchorCache _anchorCache;
        private string _source;

        public RegExpSource(string regExpSource, int? ruleId) :
            this(regExpSource, ruleId, true)
        {
        }

        public RegExpSource(string regExpSource, int? ruleId, bool handleAnchors)
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

        public RegExpSource Clone()
        {
            return new RegExpSource(this._source, this._ruleId, true);
        }

        public void SetSource(string newSource)
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

        public string ResolveBackReferences(string lineText, IOnigCaptureIndex[] captureIndices)
        {
            List<string> capturedValues = new List<string>();

            try
            {
                foreach (IOnigCaptureIndex captureIndex in captureIndices)
                {
                    capturedValues.Add(lineText.SubstringAtIndexes(
                        captureIndex.Start,
                        captureIndex.End));
                }

                return BACK_REFERENCING_END.Replace(this._source, m =>
                {
                    try
                    {
                        string value = m.Value;
                        int index = int.Parse(m.Value.SubstringAtIndexes(1, value.Length));
                        return EscapeRegExpCharacters(capturedValues.Count > index ? capturedValues[index] : "");
                    }
                    catch (Exception)
                    {
                        return "";
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return lineText;
        }

        private string EscapeRegExpCharacters(string value)
        {
            int valueLen = value.Length;
            var sb = new StringBuilder(valueLen);
            for (int i = 0; i < valueLen; i++)
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
                    sb.Append('\\');
                    break;
                }
                sb.Append(ch);
            }
            return sb.ToString();
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

        public string ResolveAnchors(bool allowA, bool allowG)
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

        public bool HasAnchor()
        {
            return this._hasAnchor;
        }

        public string GetSource()
        {
            return this._source;
        }

        public int? GetRuleId()
        {
            return this._ruleId;
        }

        public bool HasBackReferences()
        {
            return this._hasBackReferences;
        }

    }
}