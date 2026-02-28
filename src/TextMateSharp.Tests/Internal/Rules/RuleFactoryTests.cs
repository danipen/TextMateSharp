using NUnit.Framework;
using System;
using System.Collections.Generic;
using TextMateSharp.Internal.Grammars.Parser;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Types;

namespace TextMateSharp.Tests.Internal.Rules
{
    [TestFixture]
    public class RuleFactoryTests
    {
        [Test]
        public void GetCompiledRuleId_NullDesc_ReturnsNull()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(null, helper, repository);

            // assert
            Assert.IsNull(id);
        }

        [Test]
        public void GetCompiledRuleId_ReusesExistingRuleId()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw rule = new Raw();
            rule.SetName("match.rule");
            rule["match"] = "abc";

            // act
            RuleId first = RuleFactory.GetCompiledRuleId(rule, helper, repository);
            int countAfterFirst = helper.RuleCount;
            RuleId second = RuleFactory.GetCompiledRuleId(rule, helper, repository);

            // assert
            Assert.AreEqual(first, second);
            Assert.AreEqual(countAfterFirst, helper.RuleCount);
        }

        [Test]
        public void GetCompiledRuleId_MatchRule_CompilesCapturesWithRetokenizeRule()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw nestedPattern = new Raw();
            nestedPattern["match"] = "def";

            Raw captureRuleWithPatterns = new Raw();
            captureRuleWithPatterns.SetName("capture.one");
            captureRuleWithPatterns["contentName"] = "capture.one.content";
            captureRuleWithPatterns.SetPatterns(new List<IRawRule> { nestedPattern });

            Raw captureRuleWithoutPatterns = new Raw();
            captureRuleWithoutPatterns.SetName("capture.three");
            Raw captures = new Raw
            {
                ["1"] = captureRuleWithPatterns,
                ["3"] = captureRuleWithoutPatterns
            };

            Raw rule = new Raw();
            rule.SetName("match.rule");
            rule["match"] = "abc";
            rule["captures"] = captures;

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(rule, helper, repository);
            MatchRule compiledRule = helper.GetRule(id) as MatchRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(4, compiledRule.Captures.Count);
            Assert.IsNull(compiledRule.Captures[2]);
            Assert.IsNotNull(compiledRule.Captures[1].RetokenizeCapturedWithRuleId);
            Assert.IsNull(compiledRule.Captures[3].RetokenizeCapturedWithRuleId);
        }

        [Test]
        public void GetCompiledRuleId_IncludeOnlyRule_MergesRepository()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw localRule = new Raw();
            localRule.SetName("local.rule");
            localRule["match"] = "\\d+";

            Raw descRepository = new Raw();
            descRepository["local"] = localRule;

            Raw includeRule = new Raw();
            includeRule.SetInclude("#local");

            Raw includeOnlyRule = new Raw();
            includeOnlyRule.SetRepository(descRepository);
            includeOnlyRule.SetPatterns(new List<IRawRule> { includeRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(includeOnlyRule, helper, repository);
            IncludeOnlyRule compiledRule = helper.GetRule(id) as IncludeOnlyRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(1, compiledRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(compiledRule.Patterns[0]));
        }

        [Test]
        public void GetCompiledRuleId_BeginWhileRule_UsesCaptureFallbacks()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw captureRule = new Raw();
            captureRule.SetName("cap");

            Raw captures = new Raw
            {
                ["1"] = captureRule
            };

            Raw rule = new Raw();
            rule.SetName("begin.while.rule");
            rule["begin"] = "\\{";
            rule["while"] = "\\}";
            rule["captures"] = captures;
            rule.SetPatterns(new List<IRawRule>());

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(rule, helper, repository);
            BeginWhileRule compiledRule = helper.GetRule(id) as BeginWhileRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(2, compiledRule.BeginCaptures.Count);
            Assert.AreEqual(2, compiledRule.WhileCaptures.Count);
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_ResolvesLocalInclude()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw includedRule = new Raw();
            includedRule.SetName("included.rule");
            includedRule["match"] = "\\w+";
            repository["included"] = includedRule;

            Raw includeRule = new Raw();
            includeRule.SetInclude("#included");

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("block.rule");
            beginEndRule["begin"] = "\\{";
            beginEndRule["end"] = "\\}";
            beginEndRule.SetPatterns(new List<IRawRule> { includeRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(1, compiledRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(compiledRule.Patterns[0]));
            Assert.IsFalse(compiledRule.HasMissingPatterns);
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_ResolvesSelfInclude()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw selfRule = new Raw();
            selfRule.SetName("self.rule");
            selfRule["match"] = "self";
            repository.SetSelf(selfRule);

            Raw includeRule = new Raw();
            includeRule.SetInclude("$self");

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("self.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { includeRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(1, compiledRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(compiledRule.Patterns[0]));
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_SkipsMissingPatterns()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw missingInclude = new Raw();
            missingInclude.SetInclude("#missing");

            Raw includeOnlyRule = new Raw();
            includeOnlyRule.SetPatterns(new List<IRawRule> { missingInclude });

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("outer.block");
            beginEndRule["begin"] = "\\[";
            beginEndRule["end"] = "\\]";
            beginEndRule.SetPatterns(new List<IRawRule> { includeOnlyRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.IsTrue(compiledRule.HasMissingPatterns);
            Assert.AreEqual(0, compiledRule.Patterns.Count);
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_SkipsMissingNestedBeginEndRule()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw missingInclude = new Raw();
            missingInclude.SetInclude("#missing");

            Raw nestedBeginEndRule = new Raw();
            nestedBeginEndRule.SetName("nested.block");
            nestedBeginEndRule["begin"] = "\\{";
            nestedBeginEndRule["end"] = "\\}";
            nestedBeginEndRule.SetPatterns(new List<IRawRule> { missingInclude });

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("outer.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { nestedBeginEndRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.IsTrue(compiledRule.HasMissingPatterns);
            Assert.AreEqual(0, compiledRule.Patterns.Count);
        }

        [Test]
        public void GetCompiledRuleId_BeginWhileRule_SkipsMissingPatterns()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw missingInclude = new Raw();
            missingInclude.SetInclude("#missing");

            Raw beginWhileRule = new Raw();
            beginWhileRule.SetName("outer.while");
            beginWhileRule["begin"] = "\\(";
            beginWhileRule["while"] = "\\)";
            beginWhileRule.SetPatterns(new List<IRawRule> { missingInclude });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginWhileRule, helper, repository);
            BeginWhileRule compiledRule = helper.GetRule(id) as BeginWhileRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.IsTrue(compiledRule.HasMissingPatterns);
            Assert.AreEqual(0, compiledRule.Patterns.Count);
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_SkipsMissingNestedBeginWhileRule()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw missingInclude = new Raw();
            missingInclude.SetInclude("#missing");

            Raw nestedBeginWhileRule = new Raw();
            nestedBeginWhileRule.SetName("nested.while");
            nestedBeginWhileRule["begin"] = "\\{";
            nestedBeginWhileRule["while"] = "\\}";
            nestedBeginWhileRule.SetPatterns(new List<IRawRule> { missingInclude });

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("outer.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { nestedBeginWhileRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.IsTrue(compiledRule.HasMissingPatterns);
            Assert.AreEqual(0, compiledRule.Patterns.Count);
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_ResolvesExternalInclude()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw externalRepository = new Raw();

            Raw externalInnerRule = new Raw();
            externalInnerRule.SetName("external.inner");
            externalInnerRule["match"] = "xyz";
            externalRepository["inner"] = externalInnerRule;

            Raw externalGrammar = new Raw();
            externalGrammar.SetRepository(externalRepository);
            helper.AddExternalGrammar("external.scope", externalGrammar);

            Raw includeRule = new Raw();
            includeRule.SetInclude("external.scope#inner");

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("external.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { includeRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(1, compiledRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(compiledRule.Patterns[0]));
        }

        [Test]
        public void GetCompiledRuleId_ExternalIncludeWithoutFragment_UsesExternalSelfRule()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw externalRepository = new Raw();
            Raw externalSelfRule = new Raw();
            externalSelfRule.SetName("external.self");
            externalSelfRule["match"] = "self";
            externalRepository.SetSelf(externalSelfRule);

            Raw externalGrammar = new Raw();
            externalGrammar.SetRepository(externalRepository);
            helper.AddExternalGrammar("external.scope", externalGrammar);

            Raw includeRule = new Raw();
            includeRule.SetInclude("external.scope");

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("external.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { includeRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(1, compiledRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(compiledRule.Patterns[0]));
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_SkipsMissingNestedBeginEndRule_WithMixedPatterns()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw validPattern = new Raw();
            validPattern.SetName("valid.rule");
            validPattern["match"] = "valid";

            Raw missingInclude = new Raw();
            missingInclude.SetInclude("#missing");

            Raw nestedBeginEndRule = new Raw();
            nestedBeginEndRule.SetName("nested.block");
            nestedBeginEndRule["begin"] = "\\{";
            nestedBeginEndRule["end"] = "\\}";
            nestedBeginEndRule.SetPatterns(new List<IRawRule> { validPattern, missingInclude });

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("outer.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { nestedBeginEndRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.IsFalse(compiledRule.HasMissingPatterns);
            Assert.AreEqual(1, compiledRule.Patterns.Count);

            BeginEndRule nestedRule = helper.GetRule(compiledRule.Patterns[0]) as BeginEndRule;
            Assert.IsNotNull(nestedRule);
            Assert.IsTrue(nestedRule.HasMissingPatterns);
            Assert.AreEqual(1, nestedRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(nestedRule.Patterns[0]));
        }

        [Test]
        public void GetCompiledRuleId_BeginEndRule_ResolvesBaseInclude()
        {
            // arrange
            MockRuleFactoryHelper helper = new MockRuleFactoryHelper();
            Raw repository = new Raw();

            Raw baseRule = new Raw();
            baseRule.SetName("base.rule");
            baseRule["match"] = "base";
            repository.SetBase(baseRule);

            Raw includeRule = new Raw();
            includeRule.SetInclude("$base");

            Raw beginEndRule = new Raw();
            beginEndRule.SetName("base.block");
            beginEndRule["begin"] = "\\(";
            beginEndRule["end"] = "\\)";
            beginEndRule.SetPatterns(new List<IRawRule> { includeRule });

            // act
            RuleId id = RuleFactory.GetCompiledRuleId(beginEndRule, helper, repository);
            BeginEndRule compiledRule = helper.GetRule(id) as BeginEndRule;

            // assert
            Assert.IsNotNull(compiledRule);
            Assert.AreEqual(1, compiledRule.Patterns.Count);
            Assert.IsInstanceOf<MatchRule>(helper.GetRule(compiledRule.Patterns[0]));
            Assert.IsFalse(compiledRule.HasMissingPatterns);
        }

        private sealed class MockRuleFactoryHelper : IRuleFactoryHelper
        {
            private int _lastRuleId;
            private readonly Dictionary<RuleId, Rule> _rules = new Dictionary<RuleId, Rule>();
            private readonly Dictionary<string, IRawGrammar> _externalGrammars = new Dictionary<string, IRawGrammar>();

            public int RuleCount => _rules.Count;

            public Rule RegisterRule(Func<RuleId, Rule> factory)
            {
                RuleId id = RuleId.Of(++_lastRuleId);
                Rule rule = factory(id);
                _rules[id] = rule;
                return rule;
            }

            public Rule GetRule(RuleId patternId)
            {
                _rules.TryGetValue(patternId, out Rule rule);
                return rule;
            }

            public IRawGrammar GetExternalGrammar(string scopeName, IRawRepository repository)
            {
                _externalGrammars.TryGetValue(scopeName, out IRawGrammar grammar);
                return grammar;
            }

            public void AddExternalGrammar(string scopeName, IRawGrammar grammar)
            {
                _externalGrammars[scopeName] = grammar;
            }
        }
    }
}