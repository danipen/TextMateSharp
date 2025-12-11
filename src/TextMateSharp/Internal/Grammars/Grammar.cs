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
        private RuleId _rootId;
        private int _lastRuleId;
        private volatile bool _isCompiling;
        private Dictionary<RuleId, Rule> _ruleId2desc;
        private Dictionary<string, IRawGrammar> _includedGrammars;
        private IGrammarRepository _grammarRepository;
        private IRawGrammar _rawGrammar;
        private List<Injection> _injections;
        private BasicScopeAttributesProvider _basicScopeAttributesProvider;
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
            _basicScopeAttributesProvider = new BasicScopeAttributesProvider(initialLanguage, themeProvider, embeddedLanguages);
            _balancedBracketSelectors = balancedBracketSelectors;
            _rootId = null;
            _lastRuleId = 0;
            _includedGrammars = new Dictionary<string, IRawGrammar>();
            _grammarRepository = grammarRepository;
            _rawGrammar = InitGrammar(grammar, null);
            _ruleId2desc = new Dictionary<RuleId, Rule>();
            _injections = null;
            _tokenTypeMatchers = GenerateTokenTypeMatchers(tokenTypes);
        }

        public void OnDidChangeTheme()
        {
            this._basicScopeAttributesProvider.OnDidChangeTheme();
        }

        public BasicScopeAttributes GetMetadataForScope(string scope)
        {
            return this._basicScopeAttributesProvider.GetBasicScopeAttributes(scope);
        }

        public bool IsCompiling => _isCompiling;

        public List<Injection> GetInjections()
        {
            if (this._injections == null)
            {
                this._injections = new List<Injection>();

                var grammarRepository = new GrammarRepository(this);
                var scopeName = this._rootScopeName;
                var grammar = grammarRepository.Lookup(scopeName);

                if (grammar != null)
                {
                    // add injections from the current grammar
                    Dictionary<string, IRawRule> rawInjections = grammar.GetInjections();
                    if (rawInjections != null)
                    {
                        foreach (string expression in rawInjections.Keys)
                        {
                            IRawRule rule = rawInjections[expression];
                            CollectInjections(this._injections, expression, rule, this, grammar);
                        }
                    }
                }

                // add injection grammars contributed for the current scope
                var injectionScopeNames = this._grammarRepository.Injections(scopeName);

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
                                CollectInjections(
                                    this._injections,
                                    selector,
                                    (IRawRule)injectionGrammar,
                                    this,
                                    injectionGrammar);
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
            RuleId ruleId = RuleFactory.GetCompiledRuleId(rule, ruleFactoryHelper, grammar.GetRepository());

            foreach (MatcherWithPriority<List<string>> matcher in matchers)
            {
                result.Add(new Injection(
                    matcher.Matcher,
                    ruleId,
                    grammar,
                    matcher.Priority));
            }
        }

        public Rule RegisterRule(Func<RuleId, Rule> factory)
        {
            RuleId id = RuleId.Of(++this._lastRuleId);
            Rule result = factory(id);
            this._ruleId2desc[id] = result;
            return result;
        }

        public Rule GetRule(RuleId patternId)
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
            if (_includedGrammars.TryGetValue(scopeName, out IRawGrammar value))
            {
                return value;
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

        public ITokenizeLineResult TokenizeLine(LineText lineText)
        {
            return TokenizeLine(lineText, null, TimeSpan.MaxValue);
        }

        public ITokenizeLineResult TokenizeLine(LineText lineText, IStateStack prevState, TimeSpan timeLimit)
        {
            return (ITokenizeLineResult)Tokenize(lineText.Memory, (StateStack)prevState, false, timeLimit);
        }

        public ITokenizeLineResult2 TokenizeLine2(LineText lineText)
        {
            return TokenizeLine2(lineText, null, TimeSpan.MaxValue);
        }

        public ITokenizeLineResult2 TokenizeLine2(LineText lineText, IStateStack prevState, TimeSpan timeLimit)
        {
            return (ITokenizeLineResult2)Tokenize(lineText.Memory, (StateStack)prevState, true, timeLimit);
        }

        private object Tokenize(ReadOnlyMemory<char> lineText, StateStack prevState, bool emitBinaryTokens, TimeSpan timeLimit)
        {
            if (this._rootId == null)
            {
                GenerateRootId();
            }

            bool isFirstLine;
            if (prevState == null || prevState.Equals(StateStack.NULL))
            {
                isFirstLine = true;
                BasicScopeAttributes rawDefaultMetadata = this._basicScopeAttributesProvider.GetDefaultAttributes();
                ThemeTrieElementRule defaultTheme = rawDefaultMetadata.ThemeData[0];
                int defaultMetadata = EncodedTokenAttributes.Set(0, rawDefaultMetadata.LanguageId,
                        rawDefaultMetadata.TokenType, null, defaultTheme.fontStyle, defaultTheme.foreground,
                        defaultTheme.background);

                string rootScopeName = this.GetRule(this._rootId)?.GetName(ReadOnlyMemory<char>.Empty, null);
                if (rootScopeName == null)
                    return null;
                BasicScopeAttributes rawRootMetadata = this._basicScopeAttributesProvider.GetBasicScopeAttributes(rootScopeName);
                int rootMetadata = AttributedScopeStack.MergeAttributes(defaultMetadata, null, rawRootMetadata);

                AttributedScopeStack scopeList = new AttributedScopeStack(null, rootScopeName, rootMetadata);

                prevState = new StateStack(null, this._rootId, -1, -1, false, null, scopeList, scopeList);
            }
            else
            {
                isFirstLine = false;
                prevState.Reset();
            }

            // Check if we need to append newline
            ReadOnlyMemory<char> effectiveLineText;
            if (lineText.Length == 0 || lineText.Span[lineText.Length - 1] != '\n')
            {
                // Only add \n if the passed lineText didn't have it.
                // Note: We cannot use ArrayPool here because the LineTokens/tokens may hold
                // references to this memory after this method returns. Using ArrayPool would
                // cause memory corruption when the buffer is returned and reused.
                char[] buffer = new char[lineText.Length + 1];
                lineText.Span.CopyTo(buffer);
                buffer[lineText.Length] = '\n';
                effectiveLineText = buffer.AsMemory();
            }
            else
            {
                effectiveLineText = lineText;
            }

            int lineLength = effectiveLineText.Length;
            LineTokens lineTokens = new LineTokens(emitBinaryTokens, effectiveLineText, _tokenTypeMatchers, _balancedBracketSelectors);
            TokenizeStringResult tokenizeResult = LineTokenizer.TokenizeString(this, effectiveLineText, isFirstLine, 0, prevState,
                lineTokens, true, timeLimit);

            if (emitBinaryTokens)
            {
                return new TokenizeLineResult2(lineTokens.GetBinaryResult(tokenizeResult.Stack, lineLength),
                    tokenizeResult.Stack, tokenizeResult.StoppedEarly);
            }
            return new TokenizeLineResult(lineTokens.GetResult(tokenizeResult.Stack, lineLength),
                tokenizeResult.Stack, tokenizeResult.StoppedEarly);
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
            return _rootScopeName;
        }

        public ICollection<string> GetFileTypes()
        {
            return _rawGrammar.GetFileTypes();
        }

        class GrammarRepository : IGrammarRepository
        {
            private Grammar _grammar;
            internal GrammarRepository(Grammar grammar)
            {
                _grammar = grammar;
            }

            public IRawGrammar Lookup(string scopeName)
            {
                if (scopeName.Equals(_grammar._rootScopeName))
                {
                    return _grammar._rawGrammar;
                }

                return _grammar.GetExternalGrammar(scopeName, null);
            }

            public ICollection<string> Injections(string targetScope)
            {
                return _grammar._grammarRepository.Injections(targetScope);
            }
        }
    }
}