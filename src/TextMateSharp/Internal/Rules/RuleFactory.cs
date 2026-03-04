using System.Collections.Generic;

using TextMateSharp.Internal.Types;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Rules
{
    public class RuleFactory
    {
        public static CaptureRule CreateCaptureRule(IRuleFactoryHelper helper, string name, string contentName,
                RuleId retokenizeCapturedWithRuleId)
        {
            return (CaptureRule)helper.RegisterRule(id => new CaptureRule(id, name, contentName, retokenizeCapturedWithRuleId));
        }

        public static RuleId GetCompiledRuleId(IRawRule desc, IRuleFactoryHelper helper,
                IRawRepository repository)
        {
            if (desc == null)
                return null;

            if (desc.GetId() == null)
            {

                helper.RegisterRule(id =>
                {
                    desc.SetId(id);

                    string match = desc.GetMatch();
                    if (match != null)
                    {
                        return new MatchRule(desc.GetId(), desc.GetName(), match,
                                RuleFactory.CompileCaptures(desc.GetCaptures(), helper, repository));
                    }

                    string begin = desc.GetBegin();
                    if (begin == null)
                    {
                        IRawRepository r = repository;
                        IRawRepository descRepository = desc.GetRepository();
                        if (descRepository != null)
                        {
                            r = repository.Merge(descRepository);
                        }
                        return new IncludeOnlyRule(desc.GetId(), desc.GetName(), desc.GetContentName(),
                                RuleFactory.CompilePatterns(desc.GetPatterns(), helper, r));
                    }

                    string ruleWhile = desc.GetWhile();
                    IRawCaptures captures = desc.GetCaptures();
                    IRawCaptures beginCaptures = desc.GetBeginCaptures() ?? captures;
                    IRawCaptures whileCaptures = desc.GetWhileCaptures() ?? captures;
                    IRawCaptures endCaptures = desc.GetEndCaptures() ?? captures;
                    ICollection<IRawRule> patterns = desc.GetPatterns();
                    if (ruleWhile != null)
                    {
                        return new BeginWhileRule(
                                desc.GetId(), desc.GetName(), desc.GetContentName(), begin,
                                RuleFactory.CompileCaptures(beginCaptures, helper, repository),
                                ruleWhile,
                                RuleFactory.CompileCaptures(whileCaptures, helper, repository),
                                RuleFactory.CompilePatterns(patterns, helper, repository));
                    }

                    return new BeginEndRule(desc.GetId(), desc.GetName(), desc.GetContentName(), begin,
                            RuleFactory.CompileCaptures(beginCaptures, helper, repository),
                            desc.GetEnd(),
                            RuleFactory.CompileCaptures(endCaptures, helper, repository),
                            desc.IsApplyEndPatternLast(),
                            RuleFactory.CompilePatterns(patterns, helper, repository));
                });
            }

            return desc.GetId();
        }

        private static List<CaptureRule> CompileCaptures(IRawCaptures captures, IRuleFactoryHelper helper,
                IRawRepository repository)
        {
            if (captures == null)
            {
                return new List<CaptureRule>();
            }

            int numericCaptureId;
            int maximumCaptureId = 0;

            // Find the maximum capture id
            foreach (string captureId in captures)
            {
                numericCaptureId = ParseInt(captureId);
                if (numericCaptureId > maximumCaptureId)
                {
                    maximumCaptureId = numericCaptureId;
                }
            }

            // Initialize result
            List<CaptureRule> r = new List<CaptureRule>(maximumCaptureId + 1);
            for (int i = 0; i <= maximumCaptureId; i++)
            {
                r.Add(null);
            }

            // Fill out result
            foreach (string captureId in captures)
            {
                numericCaptureId = ParseInt(captureId);
                RuleId retokenizeCapturedWithRuleId = null;
                IRawRule rule = captures.GetCapture(captureId);
                if (rule.GetPatterns() != null)
                {
                    retokenizeCapturedWithRuleId = RuleFactory.GetCompiledRuleId(rule, helper, repository);
                }
                r[numericCaptureId] = RuleFactory.CreateCaptureRule(
                    helper, rule.GetName(), rule.GetContentName(),
                    retokenizeCapturedWithRuleId);
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
            int patternCount = patterns != null ? patterns.Count : 0;
            List<RuleId> r = new List<RuleId>(patternCount);
            RuleId patternId;
            IRawGrammar externalGrammar;
            Rule rule;
            bool skipRule;

            if (patterns != null)
            {
                foreach (IRawRule pattern in patterns)
                {
                    patternId = null;
                    string include = pattern.GetInclude();

                    if (include != null)
                    {
                        if (include[0] == '#')
                        {
                            // Local include found in `repository`
                            IRawRule localIncludedRule = repository.GetProp(include.Substring(1));
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
                        else if (include.Equals("$base") || include.Equals("$self"))
                        {
                            // Special include also found in `repository`
                            patternId = RuleFactory.GetCompiledRuleId(repository.GetProp(include), helper,
                                    repository);
                        }
                        else
                        {
                            string externalGrammarName = null, externalGrammarInclude = null;
                            int sharpIndex = include.IndexOf('#');
                            if (sharpIndex >= 0)
                            {
                                externalGrammarName = include.SubstringAtIndexes(0, sharpIndex);
                                externalGrammarInclude = include.Substring(sharpIndex + 1);
                            }
                            else
                            {
                                externalGrammarName = include;
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

                        if (rule is IncludeOnlyRule ior)
                        {
                            if (ior.HasMissingPatterns && ior.Patterns.Count == 0)
                            {
                                skipRule = true;
                            }
                        }
                        else if (rule is BeginEndRule br)
                        {
                            if (br.HasMissingPatterns && br.Patterns.Count == 0)
                            {
                                skipRule = true;
                            }
                        }
                        else if (rule is BeginWhileRule bwRule)
                        {
                            if (bwRule.HasMissingPatterns && bwRule.Patterns.Count == 0)
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

            return new CompilePatternsResult(r, (patternCount != r.Count));
        }
    }
}