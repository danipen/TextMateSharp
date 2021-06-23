using System;
using System.Collections.Generic;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Matcher;
using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class Grammar : IGrammar, IRuleFactoryHelper
    {

        private int? rootId;
        private int lastRuleId;
        private Dictionary<int?, Rule> ruleId2desc;
        private Dictionary<string, IRawGrammar> includedGrammars;
        private IGrammarRepository grammarRepository;
        private IRawGrammar grammar;
        private List<Injection> injections;
        private ScopeMetadataProvider scopeMetadataProvider;

        public Grammar(IRawGrammar grammar, int initialLanguage, Dictionary<string, int> embeddedLanguages,
                IGrammarRepository grammarRepository, IThemeProvider themeProvider)
        {
            this.scopeMetadataProvider = new ScopeMetadataProvider(initialLanguage, themeProvider, embeddedLanguages);
            this.rootId = null;
            this.lastRuleId = 0;
            this.includedGrammars = new Dictionary<string, IRawGrammar>();
            this.grammarRepository = grammarRepository;
            this.grammar = InitGrammar(grammar, null);
            this.ruleId2desc = new Dictionary<int?, Rule>();
            this.injections = null;
        }

        public void OnDidChangeTheme()
        {
            this.scopeMetadataProvider.OnDidChangeTheme();
        }

        public ScopeMetadata GetMetadataForScope(string scope)
        {
            return this.scopeMetadataProvider.getMetadataForScope(scope);
        }

        public List<Injection> GetInjections()
        {
            if (this.injections == null)
            {
                this.injections = new List<Injection>();
                // add injections from the current grammar
                Dictionary<string, IRawRule> rawInjections = this.grammar.GetInjections();
                if (rawInjections != null)
                {
                    foreach (string expression in rawInjections.Keys)
                    {
                        IRawRule rule = rawInjections[expression];
                        CollectInjections(this.injections, expression, rule, this, this.grammar);
                    }
                }

                // add injection grammars contributed for the current scope
                if (this.grammarRepository != null)
                {
                    ICollection<string> injectionScopeNames = this.grammarRepository
                            .Injections(this.grammar.GetScopeName());
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
                                    CollectInjections(this.injections, selector, (IRawRule)injectionGrammar, this,
                                            injectionGrammar);
                                }
                            }
                        }
                    }
                }

                // sort by priority
                injections.Sort((i1, i2) =>
                {
                    return i1.priority - i2.priority;
                });
            }

            return this.injections;
        }

        private void CollectInjections(List<Injection> result, string selector, IRawRule rule,
                IRuleFactoryHelper ruleFactoryHelper, IRawGrammar grammar)
        {
            ICollection<MatcherWithPriority<List<string>>> matchers = Matcher<List<string>>.CreateMatchers(selector);
            int? ruleId = RuleFactory.GetCompiledRuleId(rule, ruleFactoryHelper, grammar.GetRepository());

            foreach (MatcherWithPriority<List<String>> matcher in matchers)
            {
                result.Add(new Injection(matcher.matcher, ruleId, grammar, matcher.priority));
            }
        }

        public Rule RegisterRule(Func<int, Rule> factory)
        {
            int id = (++this.lastRuleId);
            Rule result = factory(id);
            this.ruleId2desc[id] = result;
            return result;
        }

        public Rule GetRule(int? patternId)
        {
            Rule result;
            this.ruleId2desc.TryGetValue(patternId, out result);
            return result;
        }

        public IRawGrammar GetExternalGrammar(string scopeName)
        {
            return GetExternalGrammar(scopeName, null);
        }

        public IRawGrammar GetExternalGrammar(string scopeName, IRawRepository repository)
        {
            if (this.includedGrammars.ContainsKey(scopeName))
            {
                return this.includedGrammars[scopeName];
            }
            else if (this.grammarRepository != null)
            {
                IRawGrammar rawIncludedGrammar = this.grammarRepository.Lookup(scopeName);
                if (rawIncludedGrammar != null)
                {
                    this.includedGrammars[scopeName] =
                            InitGrammar(rawIncludedGrammar, repository != null ? repository.GetBase() : null);
                    return this.includedGrammars[scopeName];
                }
            }
            return null;
        }

        private IRawGrammar InitGrammar(IRawGrammar grammar, IRawRule ruleBase)
        {
            grammar = Clone(grammar);
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
            if (this.rootId == null)
            {
                this.rootId = RuleFactory.GetCompiledRuleId(this.grammar.GetRepository().GetSelf(), this,
                        this.grammar.GetRepository());
            }

            bool isFirstLine;
            if (prevState == null || prevState.Equals(StackElement.NULL))
            {
                isFirstLine = true;
                ScopeMetadata rawDefaultMetadata = this.scopeMetadataProvider.GetDefaultMetadata();
                ThemeTrieElementRule defaultTheme = rawDefaultMetadata.themeData[0];
                int defaultMetadata = StackElementMetadata.Set(0, rawDefaultMetadata.languageId,
                        rawDefaultMetadata.tokenType, defaultTheme.fontStyle, defaultTheme.foreground,
                        defaultTheme.background);

                string rootScopeName = this.GetRule(this.rootId.Value).GetName(null, null);
                ScopeMetadata rawRootMetadata = this.scopeMetadataProvider.getMetadataForScope(rootScopeName);
                int rootMetadata = ScopeListElement.mergeMetadata(defaultMetadata, null, rawRootMetadata);

                ScopeListElement scopeList = new ScopeListElement(null, rootScopeName, rootMetadata);

                prevState = new StackElement(null, this.rootId.Value, -1, null, scopeList, scopeList);
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
            OnigString onigLineText = GrammarHelper.CreateOnigString(lineText);
            int lineLength = lineText.Length;
            LineTokens lineTokens = new LineTokens(emitBinaryTokens, lineText);
            StackElement nextState = LineTokenizer.TokenizeString(this, onigLineText, isFirstLine, 0, prevState,
                    lineTokens);

            if (emitBinaryTokens)
            {
                return new TokenizeLineResult2(lineTokens.GetBinaryResult(nextState, lineLength), nextState);
            }
            return new TokenizeLineResult(lineTokens.GetResult(nextState, lineLength), nextState);
        }

        public string GetName()
        {
            return grammar.GetName();
        }

        public string GetScopeName()
        {
            return grammar.GetScopeName();
        }

        public ICollection<string> GetFileTypes()
        {
            return grammar.GetFileTypes();
        }
    }
}