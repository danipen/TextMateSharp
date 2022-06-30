using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Types;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
    public class RuleFactory
    {
        public static CaptureRule CreateCaptureRule(IRuleFactoryHelper helper, string name, string contentName,
                int? retokenizeCapturedWithRuleId)
        {
            return (CaptureRule)helper.RegisterRule(id => new CaptureRule(id, name, contentName, retokenizeCapturedWithRuleId));
        }

        public static int? GetCompiledRuleId(IRawRule desc, IRuleFactoryHelper helper,
                IRawRepository repository)
        {
            if (desc.GetId() == null)
            {

                helper.RegisterRule(id =>
                {
                    desc.SetId(id);

                    if (desc.GetMatch() != null)
                    {
                        return new MatchRule(desc.GetId(), desc.GetName(), desc.GetMatch(),
                                RuleFactory.CompileCaptures(desc.GetCaptures(), helper, repository));
                    }

                    if (desc.GetBegin() == null)
                    {
                        IRawRepository r = repository;
                        if (desc.GetRepository() != null)
                        {
                            r = repository.Merge(desc.GetRepository());
                        }
                        return new IncludeOnlyRule(desc.GetId(), desc.GetName(), desc.GetContentName(),
                                RuleFactory.CompilePatterns(desc.GetPatterns(), helper, r));
                    }

                    string ruleWhile = desc.GetWhile();
                    if (ruleWhile != null)
                    {
                        return new BeginWhileRule(
                                desc.GetId(), desc.GetName(), desc.GetContentName(), desc.GetBegin(),
                                RuleFactory.CompileCaptures(
                                        desc.GetBeginCaptures() != null ? desc.GetBeginCaptures() : desc.GetCaptures(),
                                        helper, repository),
                                ruleWhile,
                                RuleFactory.CompileCaptures(
                                        desc.GetWhileCaptures() != null ? desc.GetWhileCaptures() : desc.GetCaptures(),
                                        helper, repository),
                                RuleFactory.CompilePatterns(desc.GetPatterns(), helper, repository));
                    }

                    return new BeginEndRule(desc.GetId(), desc.GetName(), desc.GetContentName(), desc.GetBegin(),
                            RuleFactory.CompileCaptures(
                                    desc.GetBeginCaptures() != null ? desc.GetBeginCaptures() : desc.GetCaptures(),
                                    helper, repository),
                            desc.GetEnd(),
                            RuleFactory.CompileCaptures(
                                    desc.GetEndCaptures() != null ? desc.GetEndCaptures() : desc.GetCaptures(), helper,
                                    repository),
                            desc.IsApplyEndPatternLast(),
                            RuleFactory.CompilePatterns(desc.GetPatterns(), helper, repository));
                });
            }

            return desc.GetId();
        }

        private static List<CaptureRule> CompileCaptures(IRawCaptures captures, IRuleFactoryHelper helper,
                IRawRepository repository)
        {
            List<CaptureRule> r = new List<CaptureRule>();
            int numericCaptureId;
            int maximumCaptureId;
            int i;

            if (captures != null)
            {
                // Find the maximum capture id
                maximumCaptureId = 0;
                foreach (string captureId in captures)
                {
                    numericCaptureId = ParseInt(captureId);
                    if (numericCaptureId > maximumCaptureId)
                    {
                        maximumCaptureId = numericCaptureId;
                    }
                }

                // Initialize result
                for (i = 0; i <= maximumCaptureId; i++)
                {
                    r.Add(null);
                }

                // Fill out result
                foreach (string captureId in captures)
                {
                    numericCaptureId = ParseInt(captureId);
                    int? retokenizeCapturedWithRuleId = null;
                    IRawRule rule = captures.GetCapture(captureId);
                    if (rule.GetPatterns() != null)
                    {
                        retokenizeCapturedWithRuleId = RuleFactory.GetCompiledRuleId(captures.GetCapture(captureId), helper,
                                repository);
                    }
                    r[numericCaptureId] = RuleFactory.CreateCaptureRule(
                        helper, rule.GetName(), rule.GetContentName(),
                        retokenizeCapturedWithRuleId);
                }
            }

            return r;
        }

        private static int ParseInt(string str)
        {
            int result = 0;
            int.TryParse(str, out result);
            return result;
        }

        private static CompilePatternsResult CompilePatterns(ICollection<IRawRule> patterns, IRuleFactoryHelper helper,
            IRawRepository repository)
        {
            List<int?> r = new List<int?>();
            int? patternId;
            IRawGrammar externalGrammar;
            Rule rule;
            bool skipRule;

            if (patterns != null)
            {
                foreach (IRawRule pattern in patterns)
                {
                    patternId = null;

                    if (pattern.GetInclude() != null)
                    {
                        if (pattern.GetInclude()[0] == '#')
                        {
                            // Local include found in `repository`
                            IRawRule localIncludedRule = repository.GetProp(pattern.GetInclude().Substring(1));
                            if (localIncludedRule != null)
                            {
                                patternId = RuleFactory.GetCompiledRuleId(localIncludedRule, helper, repository);
                            }
                            else
                            {
                                // console.warn('CANNOT find rule for scopeName: ' +
                                // pattern.include + ', I am: ',
                                // repository['$base'].name);
                            }
                        }
                        else if (pattern.GetInclude().Equals("$base") || pattern.GetInclude().Equals("$self"))
                        {
                            // Special include also found in `repository`
                            patternId = RuleFactory.GetCompiledRuleId(repository.GetProp(pattern.GetInclude()), helper,
                                    repository);
                        }
                        else
                        {
                            string externalGrammarName = null, externalGrammarInclude = null;
                            int sharpIndex = pattern.GetInclude().IndexOf('#');
                            if (sharpIndex >= 0)
                            {
                                externalGrammarName = pattern.GetInclude().SubstringAtIndexes(0, sharpIndex);
                                externalGrammarInclude = pattern.GetInclude().Substring(sharpIndex + 1);
                            }
                            else
                            {
                                externalGrammarName = pattern.GetInclude();
                            }
                            // External include
                            externalGrammar = helper.GetExternalGrammar(externalGrammarName, repository);

                            if (externalGrammar != null)
                            {
                                if (externalGrammarInclude != null)
                                {
                                    IRawRule externalIncludedRule = externalGrammar.GetRepository()
                                            .GetProp(externalGrammarInclude);
                                    if (externalIncludedRule != null)
                                    {
                                        patternId = RuleFactory.GetCompiledRuleId(externalIncludedRule, helper,
                                                externalGrammar.GetRepository());
                                    }
                                    else
                                    {
                                        // console.warn('CANNOT find rule for
                                        // scopeName: ' + pattern.include + ', I am:
                                        // ', repository['$base'].name);
                                    }
                                }
                                else
                                {
                                    patternId = RuleFactory.GetCompiledRuleId(externalGrammar.GetRepository().GetSelf(),
                                            helper, externalGrammar.GetRepository());
                                }
                            }
                            else
                            {
                                // console.warn('CANNOT find grammar for scopeName:
                                // ' + pattern.include + ', I am: ',
                                // repository['$base'].name);
                            }

                        }
                    }
                    else
                    {
                        patternId = RuleFactory.GetCompiledRuleId(pattern, helper, repository);
                    }

                    if (patternId != null)
                    {
                        rule = helper.GetRule(patternId);

                        skipRule = false;

                        if (rule is IncludeOnlyRule)
                        {
                            IncludeOnlyRule ior = (IncludeOnlyRule)rule;
                            if (ior.HasMissingPatterns && ior.Patterns.Length == 0)
                            {
                                skipRule = true;
                            }
                        }
                        else if (rule is BeginEndRule)
                        {
                            BeginEndRule br = (BeginEndRule)rule;
                            if (br.HasMissingPatterns && br.Patterns.Length == 0)
                            {
                                skipRule = true;
                            }
                        }
                        else if (rule is BeginWhileRule)
                        {
                            BeginWhileRule br = (BeginWhileRule)rule;
                            if (br.HasMissingPatterns && br.Patterns.Length == 0)
                            {
                                skipRule = true;
                            }
                        }

                        if (skipRule)
                        {
                            // console.log('REMOVING RULE ENTIRELY DUE TO EMPTY
                            // PATTERNS THAT ARE MISSING');
                            continue;
                        }

                        r.Add(patternId);
                    }
                }
            }

            return new CompilePatternsResult(r, ((patterns != null ? patterns.Count : 0) != r.Count));
        }
            }
}