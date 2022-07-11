using System;
using System.Collections.Generic;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Matcher;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class Grammar : IGrammar, IRuleFactoryHelper
    {
        private string _rootScopeName;
        private int? _rootId;
        private int _lastRuleId;
        private volatile bool _isCompiling;
        private Dictionary<int?, Rule> _ruleId2desc;
        private Dictionary<string, IRawGrammar> _includedGrammars;
        private IGrammarRepository _grammarRepository;
        private IRawGrammar _rawGrammar;
        private List<Injection> _injections;
        private ScopeMetadataProvider _scopeMetadataProvider;
        private List<TokenTypeMatcher> _tokenTypeMatchers;
        private BalancedBracketSelectors _balancedBracketSelectors;

        public Grammar(
            string scopeName,
            IRawGrammar grammar,
            int initialLanguage,
            Dictionary<string, int> embeddedLanguages,
            Dictionary<string, int> tokenTypes,
            BalancedBracketSelectors balancedBracketSelectors,
            IGrammarRepository grammarRepository,
            IThemeProvider themeProvider)
        {
            _rootScopeName = scopeName;
            _scopeMetadataProvider = new ScopeMetadataProvider(initialLanguage, themeProvider, embeddedLanguages);
            _balancedBracketSelectors = balancedBracketSelectors;
            _rootId = null;
            _lastRuleId = 0;
            _includedGrammars = new Dictionary<string, IRawGrammar>();
            _grammarRepository = grammarRepository;
            _rawGrammar = InitGrammar(grammar, null);
            _ruleId2desc = new Dictionary<int?, Rule>();
            _injections = null;
            _tokenTypeMatchers = GenerateTokenTypeMatchers(tokenTypes);
        }

        public void OnDidChangeTheme()
        {
            this._scopeMetadataProvider.OnDidChangeTheme();
        }

        public ScopeMetadata GetMetadataForScope(string scope)
        {
            return this._scopeMetadataProvider.GetMetadataForScope(scope);
        }

        public bool IsCompiling => _isCompiling;

        public List<Injection> GetInjections()
        {
            if (this._injections == null)
            {
                this._injections = new List<Injection>();
                // add injections from the current grammar
                Dictionary<string, IRawRule> rawInjections = this._rawGrammar.GetInjections();
                if (rawInjections != null)
                {
                    foreach (string expression in rawInjections.Keys)
                    {
                        IRawRule rule = rawInjections[expression];
                        CollectInjections(this._injections, expression, rule, this, this._rawGrammar);
                    }
                }

                // add injection grammars contributed for the current scope
                if (this._grammarRepository != null)
                {
                    ICollection<string> injectionScopeNames = this._grammarRepository
                            .Injections(this._rawGrammar.GetScopeName());
                    if (injectionScopeNames != null)
                    {
                        foreach (string injectionScopeName in injectionScopeNames)
                        {
                            IRawGrammar injectionGrammar = this.GetExternalGrammar(injectionScopeName);
                            if (injectionGrammar != null)
                            {
                                string selector = injectionGrammar.GetInjectionSelector();
                                if (selector != null)
                                {
                                    CollectInjections(this._injections, selector, (IRawRule)injectionGrammar, this,
                                            injectionGrammar);
                                }
                            }
                        }
                    }
                }

                // sort by priority
                _injections.Sort((i1, i2) =>
                {
                    return i1.Priority - i2.Priority;
                });
            }

            return this._injections;
        }

        private void CollectInjections(List<Injection> result, string selector, IRawRule rule,
                IRuleFactoryHelper ruleFactoryHelper, IRawGrammar grammar)
        {
            var matchers = Matcher.Matcher.CreateMatchers(selector);
            int? ruleId = RuleFactory.GetCompiledRuleId(rule, ruleFactoryHelper, grammar.GetRepository());

            foreach (MatcherWithPriority<List<string>> matcher in matchers)
            {
                result.Add(new Injection(matcher.Matcher, ruleId, grammar, matcher.Priority));
            }
        }

        public Rule RegisterRule(Func<int, Rule> factory)
        {
            int id = (++this._lastRuleId);
            Rule result = factory(id);
            this._ruleId2desc[id] = result;
            return result;
        }

        public Rule GetRule(int? patternId)
        {
            Rule result;
            this._ruleId2desc.TryGetValue(patternId, out result);
            return result;
        }

        public IRawGrammar GetExternalGrammar(string scopeName)
        {
            return GetExternalGrammar(scopeName, null);
        }

        public IRawGrammar GetExternalGrammar(string scopeName, IRawRepository repository)
        {
            if (this._includedGrammars.ContainsKey(scopeName))
            {
                return this._includedGrammars[scopeName];
            }
            else if (this._grammarRepository != null)
            {
                IRawGrammar rawIncludedGrammar = this._grammarRepository.Lookup(scopeName);
                if (rawIncludedGrammar != null)
                {
                    this._includedGrammars[scopeName] =
                            InitGrammar(rawIncludedGrammar, repository != null ? repository.GetBase() : null);
                    return this._includedGrammars[scopeName];
                }
            }
            return null;
        }

        private IRawGrammar InitGrammar(IRawGrammar grammar, IRawRule ruleBase)
        {
            grammar = grammar.Clone();
            if (grammar.GetRepository() == null)
            {
                ((Raw)grammar).SetRepository(new Raw());
            }
            Raw self = new Raw();
            self.SetPatterns(grammar.GetPatterns());
            self.SetName(grammar.GetScopeName());
            grammar.GetRepository().SetSelf(self);
            if (ruleBase != null)
            {
                grammar.GetRepository().SetBase(ruleBase);
            }
            else
            {
                grammar.GetRepository().SetBase(grammar.GetRepository().GetSelf());
            }
            return grammar;
        }

        private IRawGrammar Clone(IRawGrammar grammar)
        {
            return (IRawGrammar)((Raw)grammar).Clone();
        }

        public ITokenizeLineResult TokenizeLine(string lineText)
        {
            return TokenizeLine(lineText, null);
        }

        public ITokenizeLineResult TokenizeLine(string lineText, StackElement prevState)
        {
            return (ITokenizeLineResult)Tokenize(lineText, prevState, false);
        }

        public ITokenizeLineResult2 TokenizeLine2(string lineText)
        {
            return TokenizeLine2(lineText, null);
        }

        public ITokenizeLineResult2 TokenizeLine2(string lineText, StackElement prevState)
        {
            return (ITokenizeLineResult2)Tokenize(lineText, prevState, true);
        }

        private object Tokenize(string lineText, StackElement prevState, bool emitBinaryTokens)
        {
            if (this._rootId == null)
            {
                GenerateRootId();
            }

            bool isFirstLine;
            if (prevState == null || prevState.Equals(StackElement.NULL))
            {
                isFirstLine = true;
                ScopeMetadata rawDefaultMetadata = this._scopeMetadataProvider.GetDefaultMetadata();
                ThemeTrieElementRule defaultTheme = rawDefaultMetadata.ThemeData[0];
                int defaultMetadata = StackElementMetadata.Set(0, rawDefaultMetadata.LanguageId,
                        rawDefaultMetadata.TokenType, null, defaultTheme.fontStyle, defaultTheme.foreground,
                        defaultTheme.background);

                string rootScopeName = this.GetRule(this._rootId.Value)?.GetName(null, null);
                if (rootScopeName == null)
                    return null;
                ScopeMetadata rawRootMetadata = this._scopeMetadataProvider.GetMetadataForScope(rootScopeName);
                int rootMetadata = ScopeListElement.MergeMetadata(defaultMetadata, null, rawRootMetadata);

                ScopeListElement scopeList = new ScopeListElement(null, rootScopeName, rootMetadata);

                prevState = new StackElement(null, this._rootId.Value, -1, null, scopeList, scopeList);
            }
            else
            {
                isFirstLine = false;
                prevState.Reset();
            }

            if (string.IsNullOrEmpty(lineText) || lineText[lineText.Length - 1] != '\n')
            {
                // Only add \n if the passed lineText didn't have it.
                lineText += '\n';
            }
            int lineLength = lineText.Length;
            LineTokens lineTokens = new LineTokens(emitBinaryTokens, lineText, _tokenTypeMatchers, _balancedBracketSelectors);
            StackElement nextState = LineTokenizer.TokenizeString(this, lineText, isFirstLine, 0, prevState,
                lineTokens, true);

            if (emitBinaryTokens)
            {
                return new TokenizeLineResult2(lineTokens.GetBinaryResult(nextState, lineLength), nextState);
            }
            return new TokenizeLineResult(lineTokens.GetResult(nextState, lineLength), nextState);
        }

        private void GenerateRootId()
        {
            _isCompiling = true;
            try
            {
                this._rootId = RuleFactory.GetCompiledRuleId(this._rawGrammar.GetRepository().GetSelf(), this,
                        this._rawGrammar.GetRepository());
            }
            finally
            {
                _isCompiling = false;
            }
        }

        private List<TokenTypeMatcher> GenerateTokenTypeMatchers(Dictionary<string, int> tokenTypes)
        {
            var result = new List<TokenTypeMatcher>();

            if (tokenTypes == null)
                return result;

            foreach (var selector in tokenTypes.Keys)
            {
                foreach (var matcher in Matcher.Matcher.CreateMatchers(selector))
                {
                    result.Add(new TokenTypeMatcher(tokenTypes[selector], matcher.Matcher));
                }
            }

            return result;
        }

        public string GetName()
        {
            return _rawGrammar.GetName();
        }

        public string GetScopeName()
        {
            return _rawGrammar.GetScopeName();
        }

        public ICollection<string> GetFileTypes()
        {
            return _rawGrammar.GetFileTypes();
        }
    }
}