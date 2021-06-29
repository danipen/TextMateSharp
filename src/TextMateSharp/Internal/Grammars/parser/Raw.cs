using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TextMateSharp.Internal.Types;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Grammars.Parser
{
    public class Raw : Dictionary<string, object>, IRawRepository, IRawRule, IRawGrammar, IRawCaptures
    {
        private static string FIRST_LINE_MATCH = "firstLineMatch";
        private static string FILE_TYPES = "fileTypes";
        private static string SCOPE_NAME = "scopeName";
        private static string APPLY_END_PATTERN_LAST = "applyEndPatternLast";
        private static string REPOSITORY = "repository";
        private static string INJECTION_SELECTOR = "injectionSelector";
        private static string INJECTIONS = "injections";
        private static string PATTERNS = "patterns";
        private static string WHILE_CAPTURES = "whileCaptures";
        private static string END_CAPTURES = "endCaptures";
        private static string INCLUDE = "include";
        private static string WHILE = "while";
        private static string END = "end";
        private static string BEGIN = "begin";
        private static string CAPTURES = "captures";
        private static string MATCH = "match";
        private static string BEGIN_CAPTURES = "beginCaptures";
        private static string CONTENT_NAME = "contentName";
        private static string NAME = "name";
        private static string ID = "id";
        private static string DOLLAR_SELF = "$self";
        private static string DOLLAR_BASE = "$base";
        private List<string> fileTypes;

        public IRawRule GetProp(string name)
        {
            return TryGetObject<IRawRule>(name);
        }

        public IRawRule GetBase()
        {
            return TryGetObject<IRawRule>(DOLLAR_BASE);
        }

        public void SetBase(IRawRule ruleBase)
        {
            this[DOLLAR_BASE] = ruleBase;
        }

        public IRawRule GetSelf()
        {
            return TryGetObject<IRawRule>(DOLLAR_SELF);
        }

        public void SetSelf(IRawRule self)
        {
            this[DOLLAR_SELF] = self;
        }

        public int? GetId()
        {
            return TryGetObject<int?>(ID);
        }

        public void SetId(int id)
        {
            this[ID] = id;
        }

        public string GetName()
        {
            return TryGetObject<string>(NAME);
        }

        public void SetName(string name)
        {
            this[NAME] = name;
        }

        public string GetContentName()
        {
            return TryGetObject<string>(CONTENT_NAME);
        }

        public void SetContentName(string name)
        {
            this[CONTENT_NAME] = name;
        }

        public string GetMatch()
        {
            return TryGetObject<string>(MATCH);
        }

        public void SetMatch(string match)
        {
            this[MATCH] = match;
        }

        public IRawCaptures GetCaptures()
        {
            UpdateCaptures(CAPTURES);
            return TryGetObject<IRawCaptures>(CAPTURES);
        }

        private void UpdateCaptures(string name)
        {
            object captures = TryGetObject<object>(name);
            if (captures is IList)
            {
                Raw rawCaptures = new Raw();
                int i = 0;
                foreach (object capture in (IList)captures)
                {
                    i++;
                    rawCaptures[i + ""] = capture;
                }
                this[name] = rawCaptures;
            }
        }

        public void SetCaptures(IRawCaptures captures)
        {
            this[CAPTURES] = captures;
        }

        public string GetBegin()
        {
            return TryGetObject<string>(BEGIN);
        }

        public void SetBegin(string begin)
        {
            this[BEGIN] = begin;
        }

        public string GetWhile()
        {
            return TryGetObject<string>(WHILE);
        }

        public string GetInclude()
        {
            return TryGetObject<string>(INCLUDE);
        }

        public void SetInclude(string include)
        {
            this[INCLUDE] = include;
        }

        public IRawCaptures GetBeginCaptures()
        {
            UpdateCaptures(BEGIN_CAPTURES);
            return TryGetObject<IRawCaptures>(BEGIN_CAPTURES);
        }

        public void SetBeginCaptures(IRawCaptures beginCaptures)
        {
            this[BEGIN_CAPTURES] = beginCaptures;
        }

        public string GetEnd()
        {
            return TryGetObject<string>(END);
        }

        public void SetEnd(string end)
        {
            this[END] = end;
        }

        public IRawCaptures GetEndCaptures()
        {
            UpdateCaptures(END_CAPTURES);
            return TryGetObject<IRawCaptures>(END_CAPTURES);
        }

        public void SetEndCaptures(IRawCaptures endCaptures)
        {
            this[END_CAPTURES] = endCaptures;
        }

        public IRawCaptures GetWhileCaptures()
        {
            UpdateCaptures(WHILE_CAPTURES);
            return TryGetObject<IRawCaptures>(WHILE_CAPTURES);
        }

        public ICollection<IRawRule> GetPatterns()
        {
            ICollection result = TryGetObject<ICollection>(PATTERNS);

            if (result == null)
                return null;

            return result.Cast<IRawRule>().ToList();
        }

        public void SetPatterns(ICollection<IRawRule> patterns)
        {
            this[PATTERNS] = patterns;
        }

        public Dictionary<string, IRawRule> GetInjections()
        {
            Raw result = TryGetObject<Raw>(INJECTIONS);

            if (result == null)
                return null;

            return ConvertToDictionary<IRawRule>(result);
        }

        public string GetInjectionSelector()
        {
            return (string)this[INJECTION_SELECTOR];
        }

        public IRawRepository GetRepository()
        {
            return TryGetObject<IRawRepository>(REPOSITORY);
        }

        public void SetRepository(IRawRepository repository)
        {
            this[REPOSITORY] = repository;
        }

        public bool IsApplyEndPatternLast()
        {
            object applyEndPatternLast = TryGetObject<object>(APPLY_END_PATTERN_LAST);
            if (applyEndPatternLast == null)
            {
                return false;
            }
            if (applyEndPatternLast is bool)
            {
                return (bool)applyEndPatternLast;
            }
            if (applyEndPatternLast is int)
            {
                return ((int)applyEndPatternLast) == 1;
            }
            return false;
        }

        public void SetApplyEndPatternLast(bool applyEndPatternLast)
        {
            this[APPLY_END_PATTERN_LAST] = applyEndPatternLast;
        }

        public string GetScopeName()
        {
            return TryGetObject<string>(SCOPE_NAME);
        }

        public ICollection<string> GetFileTypes()
        {
            if (fileTypes == null)
            {
                List<string> list = new List<string>();
                ICollection unparsedFileTypes = TryGetObject<ICollection>(FILE_TYPES);
                if (unparsedFileTypes != null)
                {
                    foreach (object o in unparsedFileTypes)
                    {
                        string str = o.ToString();
                        // #202
                        if (str.StartsWith("."))
                        {
                            str = str.Substring(1);
                        }
                        list.Add(str);
                    }
                }
                fileTypes = list;
            }
            return fileTypes;
        }

        public string GetFirstLineMatch()
        {
            return TryGetObject<string>(FIRST_LINE_MATCH);
        }

        public IRawRule GetCapture(string captureId)
        {
            return GetProp(captureId);
        }

        public object Clone()
        {
            return CloneUtils.Clone(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return Keys.GetEnumerator();
        }

        Dictionary<string, T> ConvertToDictionary<T>(Raw raw)
        {
            Dictionary<string, T> result = new Dictionary<string, T>();

            foreach (string key in raw.Keys)
                result.Add(key, (T)raw[key]);

            return result;
        }

        T TryGetObject<T>(string key)
        {
            object result;
            if (!TryGetValue(key, out result))
            {
                return default(T);
            }

            return (T)result;
        }
    }
}