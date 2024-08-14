using System;
using System.Collections.Generic;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Types;
using TextMateSharp.Internal.Utils;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class SyncRegistry : IGrammarRepository, IThemeProvider
    {
        private Dictionary<string, IGrammar> _grammars;
        private Dictionary<string, IRawGrammar> _rawGrammars;
        private Dictionary<string, ICollection<string>> _injectionGrammars;
        private Theme _theme;

        public SyncRegistry(Theme theme)
        {
            _theme = theme;
            _grammars = new Dictionary<string, IGrammar>();
            _rawGrammars = new Dictionary<string, IRawGrammar>();
            _injectionGrammars = new Dictionary<string, ICollection<string>>();
        }

        public Theme GetTheme()
        {
            return _theme;
        }

        public void SetTheme(Theme theme)
        {
            this._theme = theme;

            foreach (IGrammar grammar in _grammars.Values)
                ((Grammar)grammar).OnDidChangeTheme();

        }

        public ICollection<string> GetColorMap()
        {
            return this._theme.GetColorMap();
        }

        public ICollection<string> AddGrammar(IRawGrammar grammar, ICollection<string> injectionScopeNames)
        {
            this._rawGrammars.Add(grammar.GetScopeName(), grammar);
            ICollection<string> includedScopes = new List<string>();
            CollectIncludedScopes(includedScopes, grammar);

            if (injectionScopeNames != null)
            {
                this._injectionGrammars.Add(grammar.GetScopeName(), injectionScopeNames);
                foreach (string scopeName in injectionScopeNames)
                {
                    AddIncludedScope(scopeName, includedScopes);
                }
            }
            return includedScopes;
        }

        public IRawGrammar Lookup(string scopeName)
        {
            IRawGrammar result;
            this._rawGrammars.TryGetValue(scopeName, out result);
            return result;
        }

        public ICollection<string> Injections(string targetScope)
        {
            ICollection<string> result;
            this._injectionGrammars.TryGetValue(targetScope, out result);
            return result;
        }

        public ThemeTrieElementRule GetDefaults()
        {
            return this._theme.GetDefaults();
        }

        public List<ThemeTrieElementRule> ThemeMatch(IList<string> scopeNames)
        {
            return this._theme.Match(scopeNames);
        }

        public IGrammar GrammarForScopeName(
            string scopeName,
            int initialLanguage,
            Dictionary<string, int> embeddedLanguages,
            Dictionary<string, int> tokenTypes,
            BalancedBracketSelectors balancedBracketSelectors)
        {
            if (!_grammars.TryGetValue(scopeName, out IGrammar value))
            {
                IRawGrammar rawGrammar = Lookup(scopeName);
                if (rawGrammar == null)
                {
                    return null;
                }

                value = new Grammar(
                        scopeName,
                        rawGrammar,
                        initialLanguage,
                        embeddedLanguages,
                        tokenTypes,
                        balancedBracketSelectors,
                        this,
                        this);
                this._grammars.Add(scopeName, value);
            }
            return value;
        }

        private static void CollectIncludedScopes(ICollection<string> result, IRawGrammar grammar)
        {
            ICollection<IRawRule> patterns = grammar.GetPatterns();
            if (patterns != null /* && Array.isArray(grammar.patterns) */)
            {
                ExtractIncludedScopesInPatterns(result, patterns);
            }

            IRawRepository repository = grammar.GetRepository();
            if (repository != null)
            {
                ExtractIncludedScopesInRepository(result, repository);
            }

            // remove references to own scope (avoid recursion)
            result.Remove(grammar.GetScopeName());
        }

        private static void ExtractIncludedScopesInPatterns(ICollection<string> result, ICollection<IRawRule> patterns)
        {
            foreach (IRawRule pattern in patterns)
            {
                ICollection<IRawRule> p = pattern.GetPatterns();
                if (p != null)
                {
                    ExtractIncludedScopesInPatterns(result, p);
                }

                string include = pattern.GetInclude();
                if (include == null)
                {
                    continue;
                }

                if (include.Equals("$base") || include.Equals("$self"))
                {
                    // Special includes that can be resolved locally in this grammar
                    continue;
                }

                if (include[0] == '#')
                {
                    // Local include from this grammar
                    continue;
                }

                int sharpIndex = include.IndexOf('#');
                if (sharpIndex >= 0)
                {
                    AddIncludedScope(include.SubstringAtIndexes(0, sharpIndex), result);
                }
                else
                {
                    AddIncludedScope(include, result);
                }
            }
        }

        private static void AddIncludedScope(string scopeName, ICollection<string> includedScopes)
        {
            if (!includedScopes.Contains(scopeName))
            {
                includedScopes.Add(scopeName);
            }
        }

        private static void ExtractIncludedScopesInRepository(
            ICollection<string> result,
            IRawRepository repository)
        {
            if (!(repository is Raw))
            {
                return;
            }

            Raw rawRepository = (Raw)repository;
            foreach (string key in rawRepository.Keys)
            {
                IRawRule rule = (IRawRule)rawRepository[key];

                ICollection<IRawRule> patterns = rule.GetPatterns();
                IRawRepository repositoryRule = rule.GetRepository();

                if (patterns != null)
                {
                    ExtractIncludedScopesInPatterns(result, patterns);
                }
                if (repositoryRule != null)
                {
                    ExtractIncludedScopesInRepository(result, repositoryRule);
                }
            }
        }
    }
}
