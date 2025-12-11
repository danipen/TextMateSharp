using System;
using System.Collections.Generic;
using System.Diagnostics;
using Onigwrap;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Matcher;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Grammars
{
    class LineTokenizer
    {
        private Grammar _grammar;
        private string _lineText;
        private bool _isFirstLine;
        private int _linePos;
        private StateStack _stack;
        private LineTokens _lineTokens;
        private int _anchorPosition = -1;
        private bool _stop;
        private int _lineLength;

        public LineTokenizer(Grammar grammar, string lineText, bool isFirstLine, int linePos, StateStack stack,
                LineTokens lineTokens)
        {
            this._grammar = grammar;
            this._lineText = lineText;
            this._lineLength = lineText.Length;
            this._isFirstLine = isFirstLine;
            this._linePos = linePos;
            this._stack = stack;
            this._lineTokens = lineTokens;
        }

        public TokenizeStringResult Scan(bool checkWhileConditions, TimeSpan timeLimit)
        {
            _stop = false;

            if (checkWhileConditions)
            {
                WhileCheckResult whileCheckResult = CheckWhileConditions(_grammar, _lineText, _isFirstLine, _linePos, _stack,
                        _lineTokens);
                _stack = whileCheckResult.Stack;
                _linePos = whileCheckResult.LinePos;
                _isFirstLine = whileCheckResult.IsFirstLine;
                _anchorPosition = whileCheckResult.AnchorPosition;
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (!_stop)
            {
                if (stopWatch.Elapsed > timeLimit)
                {
                    return new TokenizeStringResult(_stack, true);
                }
                ScanNext(); // potentially modifies linePos && anchorPosition
            }

            return new TokenizeStringResult(_stack, false);
        }

        private void ScanNext()
        {
            MatchResult r = MatchRuleOrInjections(_grammar, _lineText, _isFirstLine, _linePos, _stack, _anchorPosition);

            if (r == null)
            {
                // No match
                _lineTokens.Produce(_stack, _lineLength);
                _stop = true;
                return;
            }

            IOnigCaptureIndex[] captureIndices = r.CaptureIndexes;
            RuleId matchedRuleId = r.MatchedRuleId;

            bool hasAdvanced = captureIndices != null && captureIndices.Length > 0
                && captureIndices[0].End > _linePos;

            if (matchedRuleId.Equals(RuleId.END_RULE))
            {
                // We matched the `end` for this rule => pop it
                BeginEndRule poppedRule = (BeginEndRule)_stack.GetRule(_grammar);

                /*
                 * if (logger.isEnabled()) { logger.log("  popping " + poppedRule.debugName +
                 * " - " + poppedRule.debugEndRegExp); }
                 */

                _lineTokens.Produce(_stack, captureIndices[0].Start);
                _stack = _stack.WithContentNameScopesList(_stack.NameScopesList);
                HandleCaptures(_grammar, _lineText, _isFirstLine, _stack, _lineTokens, poppedRule.EndCaptures, captureIndices);
                _lineTokens.Produce(_stack, captureIndices[0].End);

                // pop
                StateStack popped = _stack;
                _stack = _stack.Pop();
                _anchorPosition = popped.GetAnchorPos();

                if (!hasAdvanced && popped.GetEnterPos() == _linePos)
                {
                    // Grammar pushed & popped a rule without advancing
                    System.Diagnostics.Debug.WriteLine("[1] - Grammar is in an endless loop - Grammar pushed & popped a rule without advancing");
                    // See https://github.com/Microsoft/vscode-textmate/issues/12
                    // Let's assume this was a mistake by the grammar author and the
                    // intent was to continue in this state
                    _stack = popped;

                    _lineTokens.Produce(_stack, _lineLength);
                    _stop = true;
                    return;
                }
            }
            else if (captureIndices != null && captureIndices.Length > 0)
            {
                // We matched a rule!
                Rule rule = _grammar.GetRule(matchedRuleId);

                _lineTokens.Produce(_stack, captureIndices[0].Start);

                StateStack beforePush = _stack;
                // push it on the stack rule
                string scopeName = rule.GetName(_lineText, captureIndices);
                AttributedScopeStack nameScopesList = _stack.ContentNameScopesList.PushAtributed(
                    scopeName,
                    _grammar);

                _stack = _stack.Push(
                    matchedRuleId,
                    _linePos,
                    _anchorPosition,
                    captureIndices[0].End == _lineText.Length,
                    null,
                    nameScopesList,
                    nameScopesList);

                if (rule is BeginEndRule)
                {
                    BeginEndRule pushedRule = (BeginEndRule)rule;

                    HandleCaptures(
                        _grammar,
                        _lineText,
                        _isFirstLine,
                        _stack,
                        _lineTokens,
                        pushedRule.BeginCaptures,
                        captureIndices);
                    _lineTokens.Produce(_stack, captureIndices[0].End);
                    _anchorPosition = captureIndices[0].End;

                    string contentName = pushedRule.GetContentName(
                        _lineText,
                        captureIndices);

                    AttributedScopeStack contentNameScopesList = nameScopesList.PushAtributed(
                        contentName,
                        _grammar);

                    _stack = _stack.WithContentNameScopesList(contentNameScopesList);

                    if (pushedRule.EndHasBackReferences)
                    {
                        _stack = _stack.WithEndRule(
                            pushedRule.GetEndWithResolvedBackReferences(
                                _lineText,
                                captureIndices));
                    }

                    if (!hasAdvanced && beforePush.HasSameRuleAs(_stack))
                    {
                        // Grammar pushed the same rule without advancing
                        System.Diagnostics.Debug.WriteLine("[2] - Grammar is in an endless loop - Grammar pushed the same rule without advancing");
                        _stack = _stack.Pop();
                        _lineTokens.Produce(_stack, _lineLength);
                        _stop = true;
                        return;
                    }
                }
                else if (rule is BeginWhileRule)
                {
                    BeginWhileRule pushedRule = (BeginWhileRule)rule;
                    // if (IN_DEBUG_MODE) {
                    // console.log(' pushing ' + pushedRule.debugName);
                    // }

                    HandleCaptures(
                        _grammar,
                        _lineText,
                        _isFirstLine,
                        _stack,
                        _lineTokens,
                        pushedRule.BeginCaptures,
                        captureIndices);
                    _lineTokens.Produce(_stack, captureIndices[0].End);
                    _anchorPosition = captureIndices[0].End;

                    string contentName = pushedRule.GetContentName(_lineText, captureIndices);
                    AttributedScopeStack contentNameScopesList = nameScopesList.PushAtributed(contentName, _grammar);
                    _stack = _stack.WithContentNameScopesList(contentNameScopesList);

                    if (pushedRule.WhileHasBackReferences)
                    {
                        _stack = _stack.WithEndRule(
                                pushedRule.getWhileWithResolvedBackReferences(_lineText, captureIndices));
                    }

                    if (!hasAdvanced && beforePush.HasSameRuleAs(_stack))
                    {
                        // Grammar pushed the same rule without advancing
                        System.Diagnostics.Debug.WriteLine("[3] - Grammar is in an endless loop - Grammar pushed the same rule without advancing");
                        _stack = _stack.Pop();
                        _lineTokens.Produce(_stack, _lineLength);
                        _stop = true;
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

                    HandleCaptures(_grammar, _lineText, _isFirstLine, _stack, _lineTokens, matchingRule.Captures,
                            captureIndices);
                    _lineTokens.Produce(_stack, captureIndices[0].End);

                    // pop rule immediately since it is a MatchRule
                    _stack = _stack.Pop();

                    if (!hasAdvanced)
                    {
                        // Grammar is not advancing, nor is it pushing/popping
                        System.Diagnostics.Debug.WriteLine("[4] - Grammar is in an endless loop - Grammar is not advancing, nor is it pushing/popping");
                        _stack = _stack.SafePop();
                        _lineTokens.Produce(_stack, _lineLength);
                        _stop = true;
                        return;
                    }
                }
            }

            if (captureIndices != null && captureIndices.Length > 0 && captureIndices[0].End > _linePos)
            {
                // Advance stream
                _linePos = captureIndices[0].End;
                _isFirstLine = false;
            }
        }

        private MatchResult MatchRule(Grammar grammar, string lineText, in bool isFirstLine, in int linePos,
                StateStack stack, in int anchorPosition)
        {
            Rule rule = stack.GetRule(grammar);

            if (rule == null)
                return null;

            CompiledRule ruleScanner = rule.Compile(grammar, stack.EndRule, isFirstLine, linePos == anchorPosition);

            if (ruleScanner == null)
                return null;

            IOnigNextMatchResult r = ruleScanner.Scanner.FindNextMatchSync(lineText.AsMemory(), linePos);

            if (r != null)
            {
                return new MatchResult(
                    r.GetCaptureIndices(),
                    ruleScanner.Rules[r.GetIndex()]);
            }
            return null;
        }

        private MatchResult MatchRuleOrInjections(Grammar grammar, string lineText, bool isFirstLine,
            in int linePos, StateStack stack, in int anchorPosition)
        {
            // Look for normal grammar rule
            MatchResult matchResult = MatchRule(grammar, lineText, isFirstLine, linePos, stack, anchorPosition);

            // Look for injected rules
            List<Injection> injections = grammar.GetInjections();
            if (injections.Count == 0)
            {
                // No injections whatsoever => early return
                return matchResult;
            }

            MatchInjectionsResult injectionResult = MatchInjections(injections, grammar, lineText, isFirstLine, linePos,
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
            int matchResultScore = matchResult.CaptureIndexes[0].Start;
            int injectionResultScore = injectionResult.CaptureIndexes[0].Start;

            if (injectionResultScore < matchResultScore
                    || (injectionResult.IsPriorityMatch && injectionResultScore == matchResultScore))
            {
                // injection won!
                return injectionResult;
            }

            return matchResult;
        }

        private MatchInjectionsResult MatchInjections(List<Injection> injections, Grammar grammar, string lineText,
                bool isFirstLine, in int linePos, StateStack stack, in int anchorPosition)
        {
            // The lower the better
            int bestMatchRating = int.MaxValue;
            IOnigCaptureIndex[] bestMatchCaptureIndices = null;
            RuleId bestMatchRuleId = null;
            int bestMatchResultPriority = 0;

            List<string> scopes = stack.ContentNameScopesList.GetScopeNames();

            foreach (Injection injection in injections)
            {
                if (!injection.Match(scopes))
                {
                    // injection selector doesn't match stack
                    continue;
                }

                CompiledRule ruleScanner = grammar.GetRule(injection.RuleId).Compile(grammar, null, isFirstLine,
                        linePos == anchorPosition);
                IOnigNextMatchResult matchResult = ruleScanner.Scanner.FindNextMatchSync(lineText.AsMemory(), linePos);

                if (matchResult == null)
                {
                    continue;
                }

                int matchRating = matchResult.GetCaptureIndices()[0].Start;

                if (matchRating > bestMatchRating)
                {
                    // Injections are sorted by priority, so the previous injection had a better or
                    // equal priority
                    continue;
                }

                bestMatchRating = matchRating;
                bestMatchCaptureIndices = matchResult.GetCaptureIndices();
                bestMatchRuleId = ruleScanner.Rules[matchResult.GetIndex()];
                bestMatchResultPriority = injection.Priority;

                if (bestMatchRating == linePos)
                {
                    // No more need to look at the rest of the injections
                    break;
                }
            }

            if (bestMatchCaptureIndices != null)
            {
                RuleId matchedRuleId = bestMatchRuleId;
                IOnigCaptureIndex[] matchCaptureIndices = bestMatchCaptureIndices;
                bool isPriorityMatch = bestMatchResultPriority == -1;

                return new MatchInjectionsResult(
                    matchCaptureIndices,
                    matchedRuleId,
                    isPriorityMatch);
            }

            return null;
        }

        private void HandleCaptures(Grammar grammar, string lineText, bool isFirstLine, StateStack stack,
                LineTokens lineTokens, List<CaptureRule> captures, IOnigCaptureIndex[] captureIndices)
        {
            if (captures.Count == 0)
            {
                return;
            }

            int len = Math.Min(captures.Count, captureIndices.Length);
            List<LocalStackElement> localStack = new List<LocalStackElement>();
            int maxEnd = captureIndices[0].End;
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

                if (captureIndex.Length == 0)
                {
                    // Nothing really captured
                    continue;
                }

                if (captureIndex.Start > maxEnd)
                {
                    // Capture going beyond consumed string
                    break;
                }

                // pop captures while needed
                while (localStack.Count > 0 && localStack[localStack.Count - 1].EndPos <= captureIndex.Start)
                {
                    // pop!
                    lineTokens.ProduceFromScopes(localStack[localStack.Count - 1].Scopes,
                            localStack[localStack.Count - 1].EndPos);
                    localStack.RemoveAt(localStack.Count - 1);
                }

                if (localStack.Count > 0)
                {
                    lineTokens.ProduceFromScopes(localStack[localStack.Count - 1].Scopes,
                            captureIndex.Start);
                }
                else
                {
                    lineTokens.Produce(stack, captureIndex.Start);
                }

                if (captureRule.RetokenizeCapturedWithRuleId != null)
                {
                    // the capture requires additional matching
                    string scopeName = captureRule.GetName(lineText, captureIndices);
                    AttributedScopeStack nameScopesList = stack.ContentNameScopesList.PushAtributed(scopeName, grammar);
                    string contentName = captureRule.GetContentName(lineText, captureIndices);
                    AttributedScopeStack contentNameScopesList = nameScopesList.PushAtributed(contentName, grammar);

                    // the capture requires additional matching
                    StateStack stackClone = stack.Push(
                        captureRule.RetokenizeCapturedWithRuleId,
                        captureIndex.Start,
                        -1,
                        false,
                        null,
                        nameScopesList,
                        contentNameScopesList);

                    TokenizeString(grammar,
                            lineText.SubstringAtIndexes(0, captureIndex.End),
                            (isFirstLine && captureIndex.Start == 0), captureIndex.Start, stackClone, lineTokens, false, TimeSpan.MaxValue);
                    continue;
                }

                // push
                string captureRuleScopeName = captureRule.GetName(lineText, captureIndices);
                if (captureRuleScopeName != null)
                {
                    // push
                    AttributedScopeStack baseElement = localStack.Count == 0 ? stack.ContentNameScopesList :
                        localStack[localStack.Count - 1].Scopes;
                    AttributedScopeStack captureRuleScopesList = baseElement.PushAtributed(captureRuleScopeName, grammar);
                    localStack.Add(new LocalStackElement(captureRuleScopesList, captureIndex.End));
                }
            }

            while (localStack.Count > 0)
            {
                // pop!
                lineTokens.ProduceFromScopes(localStack[localStack.Count - 1].Scopes,
                        localStack[localStack.Count - 1].EndPos);
                localStack.RemoveAt(localStack.Count - 1);
            }
        }

        /**
        * Walk the stack from bottom to top, and check each while condition in this
         * order. If any fails, cut off the entire stack above the failed while
         * condition. While conditions may also advance the linePosition.
         */
        private WhileCheckResult CheckWhileConditions(Grammar grammar, string lineText, bool isFirstLine,
                int linePos, StateStack stack, LineTokens lineTokens)
        {
            int anchorPosition = stack.BeginRuleCapturedEOL ? 0 : -1;
            List<WhileStack> whileRules = new List<WhileStack>();
            for (StateStack node = stack; node != null; node = node.Pop())
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
                CompiledRule ruleScanner = whileRule.Rule.CompileWhile(whileRule.Stack.EndRule, isFirstLine,
                        anchorPosition == linePos);
                IOnigNextMatchResult r = ruleScanner.Scanner.FindNextMatchSync(lineText.AsMemory(), linePos);

                if (r != null)
                {
                    RuleId matchedRuleId = ruleScanner.Rules[r.GetIndex()];
                    if (RuleId.WHILE_RULE.NotEquals(matchedRuleId))
                    {
                        // we shouldn't end up here
                        stack = whileRule.Stack.Pop();
                        break;
                    }
                    if (r.GetCaptureIndices() != null && r.GetCaptureIndices().Length > 0)
                    {
                        lineTokens.Produce(whileRule.Stack, r.GetCaptureIndices()[0].Start);
                        HandleCaptures(grammar, lineText, isFirstLine, whileRule.Stack, lineTokens,
                                whileRule.Rule.WhileCaptures, r.GetCaptureIndices());
                        lineTokens.Produce(whileRule.Stack, r.GetCaptureIndices()[0].End);
                        anchorPosition = r.GetCaptureIndices()[0].End;
                        if (r.GetCaptureIndices()[0].End > linePos)
                        {
                            linePos = r.GetCaptureIndices()[0].End;
                            isFirstLine = false;
                        }
                    }
                }
                else
                {
                    stack = whileRule.Stack.Pop();
                    break;
                }
            }

            return new WhileCheckResult(stack, linePos, anchorPosition, isFirstLine);
        }

        public static TokenizeStringResult TokenizeString(Grammar grammar, string lineText, bool isFirstLine, int linePos,
                StateStack stack, LineTokens lineTokens, bool checkWhileConditions, TimeSpan timeLimit)
        {
            return new LineTokenizer(grammar, lineText, isFirstLine, linePos, stack, lineTokens).Scan(checkWhileConditions, timeLimit);
        }

        class WhileStack
        {
            public StateStack Stack { get; private set; }
            public BeginWhileRule Rule { get; private set; }

            public WhileStack(StateStack stack, BeginWhileRule rule)
            {
                Stack = stack;
                Rule = rule;
            }
        }

        class WhileCheckResult
        {
            public StateStack Stack { get; private set; }
            public int LinePos { get; private set; }
            public int AnchorPosition { get; private set; }
            public bool IsFirstLine { get; private set; }

            public WhileCheckResult(StateStack stack, int linePos, int anchorPosition, bool isFirstLine)
            {
                Stack = stack;
                LinePos = linePos;
                AnchorPosition = anchorPosition;
                IsFirstLine = isFirstLine;
            }
        }
    }
}