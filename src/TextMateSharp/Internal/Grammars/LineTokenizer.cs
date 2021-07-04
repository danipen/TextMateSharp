using System;
using System.Collections.Generic;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Matcher;
using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Grammars
{
    class LineTokenizer
    {
        class WhileStack
        {

            public StackElement stack;
            public BeginWhileRule rule;

            public WhileStack(StackElement stack, BeginWhileRule rule)
            {
                this.stack = stack;
                this.rule = rule;
            }
        }

        class WhileCheckResult
        {

            public StackElement stack;
            public int linePos;
            public int anchorPosition;
            public bool isFirstLine;

            public WhileCheckResult(StackElement stack, int linePos, int anchorPosition, bool isFirstLine)
            {
                this.stack = stack;
                this.linePos = linePos;
                this.anchorPosition = anchorPosition;
                this.isFirstLine = isFirstLine;
            }
        }

        private Grammar grammar;
        private string lineText;
        private bool isFirstLine;
        private int linePos;
        private StackElement stack;
        private LineTokens lineTokens;
        private int anchorPosition = -1;
        private bool stop;
        private int lineLength;

        public LineTokenizer(Grammar grammar, string lineText, bool isFirstLine, int linePos, StackElement stack,
                LineTokens lineTokens)
        {
            this.grammar = grammar;
            this.lineText = lineText;
            this.lineLength = lineText.Length;
            this.isFirstLine = isFirstLine;
            this.linePos = linePos;
            this.stack = stack;
            this.lineTokens = lineTokens;
        }

        public StackElement Scan()
        {
            stop = false;

            WhileCheckResult whileCheckResult = CheckWhileConditions(grammar, lineText, isFirstLine, linePos, stack,
                    lineTokens);
            stack = whileCheckResult.stack;
            linePos = whileCheckResult.linePos;
            isFirstLine = whileCheckResult.isFirstLine;
            anchorPosition = whileCheckResult.anchorPosition;

            while (!stop)
            {
                ScanNext(); // potentially modifies linePos && anchorPosition
            }

            return stack;
        }

        private void ScanNext()
        {
            IMatchResult r = MatchRuleOrInjections(grammar, lineText, isFirstLine, linePos, stack, anchorPosition);

            if (r == null)
            {
                // No match
                lineTokens.Produce(stack, lineLength);
                stop = true;
                return;
            }

            IOnigCaptureIndex[] captureIndices = r.GetCaptureIndices();
            int? matchedRuleId = r.GetMatchedRuleId();

            bool hasAdvanced = (captureIndices != null && captureIndices.Length > 0)
                    ? (captureIndices[0].GetEnd() > linePos)
                    : false;

            if (matchedRuleId == -1)
            {
                // We matched the `end` for this rule => pop it
                BeginEndRule poppedRule = (BeginEndRule)stack.GetRule(grammar);

                /*
				 * if (logger.isEnabled()) { logger.log("  popping " + poppedRule.debugName +
				 * " - " + poppedRule.debugEndRegExp); }
				 */

                lineTokens.Produce(stack, captureIndices[0].GetStart());
                stack = stack.setContentNameScopesList(stack.nameScopesList);
                HandleCaptures(grammar, lineText, isFirstLine, stack, lineTokens, poppedRule.endCaptures, captureIndices);
                lineTokens.Produce(stack, captureIndices[0].GetEnd());

                // pop
                StackElement popped = stack;
                stack = stack.Pop();

                if (!hasAdvanced && popped.GetEnterPos() == linePos)
                {
                    // Grammar pushed & popped a rule without advancing
                    System.Diagnostics.Debug.WriteLine("[1] - Grammar is in an endless loop - Grammar pushed & popped a rule without advancing");
                    // See https://github.com/Microsoft/vscode-textmate/issues/12
                    // Let's assume this was a mistake by the grammar author and the
                    // intent was to continue in this state
                    stack = popped;

                    lineTokens.Produce(stack, lineLength);
                    stop = true;
                    return;
                }
            }
            else if (captureIndices != null && captureIndices.Length > 0)
            {
                // We matched a rule!
                Rule rule = grammar.GetRule(matchedRuleId);

                lineTokens.Produce(stack, captureIndices[0].GetStart());

                StackElement beforePush = stack;
                // push it on the stack rule
                string scopeName = rule.GetName(lineText, captureIndices);
                ScopeListElement nameScopesList = stack.contentNameScopesList.Push(grammar, scopeName);
                stack = stack.Push(matchedRuleId, linePos, null, nameScopesList, nameScopesList);

                if (rule is BeginEndRule)
                {
                    BeginEndRule pushedRule = (BeginEndRule)rule;

                    HandleCaptures(grammar, lineText, isFirstLine, stack, lineTokens, pushedRule.beginCaptures,
                            captureIndices);
                    lineTokens.Produce(stack, captureIndices[0].GetEnd());
                    anchorPosition = captureIndices[0].GetEnd();

                    string contentName = pushedRule.GetContentName(lineText, captureIndices);
                    ScopeListElement contentNameScopesList = nameScopesList.Push(grammar, contentName);
                    stack = stack.setContentNameScopesList(contentNameScopesList);

                    if (pushedRule.endHasBackReferences)
                    {
                        stack = stack.SetEndRule(
                            pushedRule.GetEndWithResolvedBackReferences(lineText, captureIndices));
                    }

                    if (!hasAdvanced && beforePush.HasSameRuleAs(stack))
                    {
                        // Grammar pushed the same rule without advancing
                        System.Diagnostics.Debug.WriteLine("[2] - Grammar is in an endless loop - Grammar pushed the same rule without advancing");
                        stack = stack.Pop();
                        lineTokens.Produce(stack, lineLength);
                        stop = true;
                        return;
                    }
                }
                else if (rule is BeginWhileRule)
                {
                    BeginWhileRule pushedRule = (BeginWhileRule)rule;
                    // if (IN_DEBUG_MODE) {
                    // console.log(' pushing ' + pushedRule.debugName);
                    // }

                    HandleCaptures(grammar, lineText, isFirstLine, stack, lineTokens, pushedRule.beginCaptures,
                            captureIndices);
                    lineTokens.Produce(stack, captureIndices[0].GetEnd());
                    anchorPosition = captureIndices[0].GetEnd();

                    string contentName = pushedRule.GetContentName(lineText, captureIndices);
                    ScopeListElement contentNameScopesList = nameScopesList.Push(grammar, contentName);
                    stack = stack.setContentNameScopesList(contentNameScopesList);

                    if (pushedRule.whileHasBackReferences)
                    {
                        stack = stack.SetEndRule(
                                pushedRule.getWhileWithResolvedBackReferences(lineText, captureIndices));
                    }

                    if (!hasAdvanced && beforePush.HasSameRuleAs(stack))
                    {
                        // Grammar pushed the same rule without advancing
                        System.Diagnostics.Debug.WriteLine("[3] - Grammar is in an endless loop - Grammar pushed the same rule without advancing");
                        stack = stack.Pop();
                        lineTokens.Produce(stack, lineLength);
                        stop = true;
                        return;
                    }
                }
                else
                {
                    MatchRule matchingRule = (MatchRule)rule;
                    // if (IN_DEBUG_MODE) {
                    // console.log(' matched ' + matchingRule.debugName + ' - ' +
                    // matchingRule.debugMatchRegExp);
                    // }

                    HandleCaptures(grammar, lineText, isFirstLine, stack, lineTokens, matchingRule.captures,
                            captureIndices);
                    lineTokens.Produce(stack, captureIndices[0].GetEnd());

                    // pop rule immediately since it is a MatchRule
                    stack = stack.Pop();

                    if (!hasAdvanced)
                    {
                        // Grammar is not advancing, nor is it pushing/popping
                        System.Diagnostics.Debug.WriteLine("[4] - Grammar is in an endless loop - Grammar is not advancing, nor is it pushing/popping");
                        stack = stack.SafePop();
                        lineTokens.Produce(stack, lineLength);
                        stop = true;
                        return;
                    }
                }
            }

            if (captureIndices != null && captureIndices.Length > 0 && captureIndices[0].GetEnd() > linePos)
            {
                // Advance stream
                linePos = captureIndices[0].GetEnd();
                isFirstLine = false;
            }
        }

        private IMatchResult MatchRule(Grammar grammar, string lineText, bool isFirstLine, int linePos,
                StackElement stack, int anchorPosition)
        {
            Rule rule = stack.GetRule(grammar);
            ICompiledRule ruleScanner = rule.Compile(grammar, stack.endRule, isFirstLine, linePos == anchorPosition);
            IOnigNextMatchResult r = ruleScanner.scanner.FindNextMatchSync(lineText, linePos);

            if (r != null)
            {
                return new MatchResult(
                    r.GetCaptureIndices(),
                    ruleScanner.rules[r.GetIndex()]);
            }
            return null;
        }

        private IMatchResult MatchRuleOrInjections(Grammar grammar, string lineText, bool isFirstLine,
            int linePos, StackElement stack, int anchorPosition)
        {
            // Look for normal grammar rule
            IMatchResult matchResult = MatchRule(grammar, lineText, isFirstLine, linePos, stack, anchorPosition);

            // Look for injected rules
            List<Injection> injections = grammar.GetInjections();
            if (injections.Count == 0)
            {
                // No injections whatsoever => early return
                return matchResult;
            }

            IMatchInjectionsResult injectionResult = MatchInjections(injections, grammar, lineText, isFirstLine, linePos,
                    stack, anchorPosition);
            if (injectionResult == null)
            {
                // No injections matched => early return
                return matchResult;
            }

            if (matchResult == null)
            {
                // Only injections matched => early return
                return injectionResult;
            }

            // Decide if `matchResult` or `injectionResult` should win
            int matchResultScore = matchResult.GetCaptureIndices()[0].GetStart();
            int injectionResultScore = injectionResult.GetCaptureIndices()[0].GetStart();

            if (injectionResultScore < matchResultScore
                    || (injectionResult.IsPriorityMatch() && injectionResultScore == matchResultScore))
            {
                // injection won!
                return injectionResult;
            }

            return matchResult;
        }

        private IMatchInjectionsResult MatchInjections(List<Injection> injections, Grammar grammar, string lineText,
                bool isFirstLine, int linePos, StackElement stack, int anchorPosition)
        {
            // The lower the better
            int bestMatchRating = int.MaxValue;
            IOnigCaptureIndex[] bestMatchCaptureIndices = null;
            int? bestMatchRuleId = null;
            int bestMatchResultPriority = 0;

            List<string> scopes = stack.contentNameScopesList.GenerateScopes();

            foreach (Injection injection in injections)
            {
                if (!injection.Match(scopes))
                {
                    // injection selector doesn't match stack
                    continue;
                }

                ICompiledRule ruleScanner = grammar.GetRule(injection.ruleId).Compile(grammar, null, isFirstLine,
                        linePos == anchorPosition);
                IOnigNextMatchResult matchResult = ruleScanner.scanner.FindNextMatchSync(lineText, linePos);

                if (matchResult == null)
                {
                    continue;
                }

                int matchRating = matchResult.GetCaptureIndices()[0].GetStart();

                if (matchRating > bestMatchRating)
                {
                    // Injections are sorted by priority, so the previous injection had a better or
                    // equal priority
                    continue;
                }

                bestMatchRating = matchRating;
                bestMatchCaptureIndices = matchResult.GetCaptureIndices();
                bestMatchRuleId = ruleScanner.rules[matchResult.GetIndex()];
                bestMatchResultPriority = injection.priority;

                if (bestMatchRating == linePos)
                {
                    // No more need to look at the rest of the injections
                    break;
                }
            }

            if (bestMatchCaptureIndices != null)
            {
                int? matchedRuleId = bestMatchRuleId;
                IOnigCaptureIndex[] matchCaptureIndices = bestMatchCaptureIndices;
                bool isPriorityMatch = bestMatchResultPriority == -1;

                return new MatchInjectionsResult(
                    matchCaptureIndices,
                    matchedRuleId,
                    isPriorityMatch);
            }

            return null;
        }

        private void HandleCaptures(Grammar grammar, string lineText, bool isFirstLine, StackElement stack,
                LineTokens lineTokens, List<CaptureRule> captures, IOnigCaptureIndex[] captureIndices)
        {
            if (captures.Count == 0)
            {
                return;
            }

            int len = Math.Min(captures.Count, captureIndices.Length);
            List<LocalStackElement> localStack = new List<LocalStackElement>();
            int maxEnd = captureIndices[0].GetEnd();
            IOnigCaptureIndex captureIndex;

            for (int i = 0; i < len; i++)
            {
                CaptureRule captureRule = captures[i];
                if (captureRule == null)
                {
                    // Not interested
                    continue;
                }

                captureIndex = captureIndices[i];

                if (captureIndex.GetLength() == 0)
                {
                    // Nothing really captured
                    continue;
                }

                if (captureIndex.GetStart() > maxEnd)
                {
                    // Capture going beyond consumed string
                    break;
                }

                // pop captures while needed
                while (localStack.Count > 0 && localStack[localStack.Count - 1].GetEndPos() <= captureIndex.GetStart())
                {
                    // pop!
                    lineTokens.ProduceFromScopes(localStack[localStack.Count - 1].GetScopes(),
                            localStack[localStack.Count - 1].GetEndPos());
                    localStack.RemoveAt(localStack.Count - 1);
                }

                if (localStack.Count > 0)
                {
                    lineTokens.ProduceFromScopes(localStack[localStack.Count - 1].GetScopes(),
                            captureIndex.GetStart());
                }
                else
                {
                    lineTokens.Produce(stack, captureIndex.GetStart());
                }

                if (captureRule.retokenizeCapturedWithRuleId != null)
                {
                    // the capture requires additional matching
                    string scopeName = captureRule.GetName(lineText, captureIndices);
                    ScopeListElement nameScopesList = stack.contentNameScopesList.Push(grammar, scopeName);
                    string contentName = captureRule.GetContentName(lineText, captureIndices);
                    ScopeListElement contentNameScopesList = nameScopesList.Push(grammar, contentName);

                    // the capture requires additional matching
                    StackElement stackClone = stack.Push(captureRule.retokenizeCapturedWithRuleId, captureIndex.GetStart(),
                            null, nameScopesList, contentNameScopesList);
                    TokenizeString(grammar,
                            lineText.SubstringAtIndexes(0, captureIndex.GetEnd()),
                            (isFirstLine && captureIndex.GetStart() == 0), captureIndex.GetStart(), stackClone, lineTokens);
                    continue;
                }

                // push
                string captureRuleScopeName = captureRule.GetName(lineText, captureIndices);
                if (captureRuleScopeName != null)
                {
                    // push
                    ScopeListElement baseElement = localStack.Count == 0 ? stack.contentNameScopesList :
                        localStack[localStack.Count - 1].GetScopes();
                    ScopeListElement captureRuleScopesList = baseElement.Push(grammar, captureRuleScopeName);
                    localStack.Add(new LocalStackElement(captureRuleScopesList, captureIndex.GetEnd()));
                }
            }

            while (localStack.Count > 0)
            {
                // pop!
                lineTokens.ProduceFromScopes(localStack[localStack.Count - 1].GetScopes(),
                        localStack[localStack.Count - 1].GetEndPos());
                localStack.RemoveAt(localStack.Count - 1);
            }
        }

        /**
        * Walk the stack from bottom to top, and check each while condition in this
         * order. If any fails, cut off the entire stack above the failed while
         * condition. While conditions may also advance the linePosition.
         */
        private WhileCheckResult CheckWhileConditions(Grammar grammar, string lineText, bool isFirstLine,
                int linePos, StackElement stack, LineTokens lineTokens)
        {
            int currentanchorPosition = -1;
            List<WhileStack> whileRules = new List<WhileStack>();
            for (StackElement node = stack; node != null; node = node.Pop())
            {
                Rule nodeRule = node.GetRule(grammar);
                if (nodeRule is BeginWhileRule)
                {
                    whileRules.Add(new WhileStack(node, (BeginWhileRule)nodeRule));
                }
            }
            for (int i = whileRules.Count - 1; i >= 0; i--)
            {
                WhileStack whileRule = whileRules[i];
                ICompiledRule ruleScanner = whileRule.rule.CompileWhile(grammar, whileRule.stack.endRule, isFirstLine,
                        currentanchorPosition == linePos);
                IOnigNextMatchResult r = ruleScanner.scanner.FindNextMatchSync(lineText, linePos);


                if (r != null)
                {
                    int? matchedRuleId = ruleScanner.rules[r.GetIndex()];
                    if (matchedRuleId != -2)
                    {
                        // we shouldn't end up here
                        stack = whileRule.stack.Pop();
                        break;
                    }
                    if (r.GetCaptureIndices() != null && r.GetCaptureIndices().Length > 0)
                    {
                        lineTokens.Produce(whileRule.stack, r.GetCaptureIndices()[0].GetStart());
                        HandleCaptures(grammar, lineText, isFirstLine, whileRule.stack, lineTokens,
                                whileRule.rule.whileCaptures, r.GetCaptureIndices());
                        lineTokens.Produce(whileRule.stack, r.GetCaptureIndices()[0].GetEnd());
                        currentanchorPosition = r.GetCaptureIndices()[0].GetEnd();
                        if (r.GetCaptureIndices()[0].GetEnd() > linePos)
                        {
                            linePos = r.GetCaptureIndices()[0].GetEnd();
                            isFirstLine = false;
                        }
                    }
                }
                else
                {
                    stack = whileRule.stack.Pop();
                    break;
                }
            }

            return new WhileCheckResult(stack, linePos, currentanchorPosition, isFirstLine);
        }

        public static StackElement TokenizeString(Grammar grammar, string lineText, bool isFirstLine, int linePos,
                StackElement stack, LineTokens lineTokens)
        {
            return new LineTokenizer(grammar, lineText, isFirstLine, linePos, stack, lineTokens).Scan();
        }
    }
}