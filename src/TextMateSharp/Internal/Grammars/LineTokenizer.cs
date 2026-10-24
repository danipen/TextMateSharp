using TextMateSharp.Internal.Oniguruma;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Matcher;
using TextMateSharp.Internal.Rules;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Internal.Grammars
{
    internal sealed class LineTokenizer
    {
        // A grammar keeps one tokenizer per nesting depth (capture retokenization nests) and reuses it for every line,
        // with its buffers and capture pool: tokenizing is serialized per grammar.
        private Grammar _grammar;
        private ReadOnlyMemory<char> _lineText;
        private bool _isFirstLine;
        private int _linePos;
        private StateStack _stack;
        private LineTokens _lineTokens;
        private int _anchorPosition = -1;
        private bool _stop;
        private int _lineLength;
        private readonly List<LocalStackElement> _localStackBuffer = new List<LocalStackElement>();
        private readonly List<WhileStack> _whileRulesBuffer = new List<WhileStack>();
        private readonly OnigCapturePool _capturePool = new OnigCapturePool();

        private void Start(Grammar grammar, ReadOnlyMemory<char> lineText, bool isFirstLine, int linePos, StateStack stack,
                LineTokens lineTokens)
        {
            this._grammar = grammar;
            this._lineText = lineText;
            this._lineLength = lineText.Length;
            this._isFirstLine = isFirstLine;
            this._linePos = linePos;
            this._stack = stack;
            this._lineTokens = lineTokens;
            this._anchorPosition = -1;
            this._stop = false;
            this._capturePool.Reset();
        }

        private void Finish()
        {
            // Keep no line, stack or tokens alive between lines.
            this._lineText = default;
            this._stack = null;
            this._lineTokens = null;
            this._localStackBuffer.Clear();
            this._whileRulesBuffer.Clear();
        }

        private TokenizeStringResult Scan(bool checkWhileConditions, TimeSpan timeLimit)
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

            // The time limit as a Stopwatch timestamp, read once per line rather than as a TimeSpan per scan.
            long started = Stopwatch.GetTimestamp();
            double limitTicks = timeLimit.TotalSeconds * Stopwatch.Frequency;
            long deadline = limitTicks >= long.MaxValue - started ? long.MaxValue : started + (long)limitTicks;
            while (!_stop)
            {
                if (Stopwatch.GetTimestamp() > deadline)
                {
                    return new TokenizeStringResult(_stack, true);
                }
                ScanNext(); // potentially modifies linePos && anchorPosition
            }

            return new TokenizeStringResult(_stack, false);
        }

        private void ScanNext()
        {
            _capturePool.Reset();
            MatchResult r = MatchRuleOrInjections(_grammar, _lineText, _isFirstLine, _linePos, _stack, _anchorPosition);

            if (!r.IsMatch)
            {
                // No match
                _lineTokens.Produce(_stack, _lineLength);
                _stop = true;
                return;
            }

            OnigCaptureIndex[] captureIndices = r.CaptureIndexes;
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
                // The rule's state with its name scopes as content, created only if a capture or the endless loop
                // check needs it.
                var popped = new CaptureState(_stack, _stack.NameScopesList);
                HandleCaptures(_grammar, _lineText, _isFirstLine, ref popped, _lineTokens, poppedRule.EndCaptures, captureIndices);
                _lineTokens.ProduceFromScopes(popped.ContentNameScopesList, captureIndices[0].End);

                // pop
                _anchorPosition = _stack.GetAnchorPos();
                int poppedEnterPos = _stack.GetEnterPos();
                _stack = _stack.Pop();

                if (!hasAdvanced && poppedEnterPos == _linePos)
                {
                    // Grammar pushed & popped a rule without advancing
                    System.Diagnostics.Debug.WriteLine("[1] - Grammar is in an endless loop - Grammar pushed & popped a rule without advancing");
                    // See https://github.com/Microsoft/vscode-textmate/issues/12
                    // Let's assume this was a mistake by the grammar author and the
                    // intent was to continue in this state
                    _stack = popped.Get();

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
                AttributedScopeStack nameScopesList = _stack.ContentNameScopesList.PushAttributed(
                    scopeName,
                    _grammar);

                // The pushed rule, created only if a capture needs it: a match rule pops right away, and a begin rule
                // pushes its final state (with its content scopes and resolved end) once.
                int anchorPositionBeforePush = _anchorPosition;
                var pushed = new CaptureState(_stack, matchedRuleId, _linePos, _anchorPosition,
                    captureIndices[0].End == _lineText.Length, nameScopesList);

                if (rule is BeginEndRule beginEndRule)
                {
                    // The content scopes and end depend only on the begin match: resolved first, so that the state is
                    // pushed once when the captures' state is the final one.
                    string contentName = beginEndRule.GetContentName(
                        _lineText,
                        captureIndices);

                    AttributedScopeStack contentNameScopesList = nameScopesList.PushAttributed(
                        contentName,
                        _grammar);

                    string endRule = beginEndRule.EndHasBackReferences
                        ? beginEndRule.GetEndWithResolvedBackReferences(_lineText, captureIndices)
                        : null;

                    StateStack final = ReferenceEquals(contentNameScopesList, nameScopesList) && endRule == null ? pushed.Get() : null;

                    HandleCaptures(
                        _grammar,
                        _lineText,
                        _isFirstLine,
                        ref pushed,
                        _lineTokens,
                        beginEndRule.BeginCaptures,
                        captureIndices);
                    _lineTokens.ProduceFromScopes(nameScopesList, captureIndices[0].End);
                    _anchorPosition = captureIndices[0].End;

                    _stack = final ?? beforePush.Push(matchedRuleId, _linePos, anchorPositionBeforePush,
                        captureIndices[0].End == _lineText.Length, endRule, nameScopesList, contentNameScopesList);

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
                else if (rule is BeginWhileRule beginWhileRule)
                {
                    // if (IN_DEBUG_MODE) {
                    // console.log(' pushing ' + pushedRule.debugName);
                    // }

                    string contentName = beginWhileRule.GetContentName(_lineText, captureIndices);
                    AttributedScopeStack contentNameScopesList = nameScopesList.PushAttributed(contentName, _grammar);

                    string whileRule = beginWhileRule.WhileHasBackReferences
                        ? beginWhileRule.GetWhileWithResolvedBackReferences(_lineText, captureIndices)
                        : null;

                    StateStack final = ReferenceEquals(contentNameScopesList, nameScopesList) && whileRule == null ? pushed.Get() : null;

                    HandleCaptures(
                        _grammar,
                        _lineText,
                        _isFirstLine,
                        ref pushed,
                        _lineTokens,
                        beginWhileRule.BeginCaptures,
                        captureIndices);
                    _lineTokens.ProduceFromScopes(nameScopesList, captureIndices[0].End);
                    _anchorPosition = captureIndices[0].End;

                    _stack = final ?? beforePush.Push(matchedRuleId, _linePos, anchorPositionBeforePush,
                        captureIndices[0].End == _lineText.Length, whileRule, nameScopesList, contentNameScopesList);

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

                    HandleCaptures(_grammar, _lineText, _isFirstLine, ref pushed, _lineTokens, matchingRule.Captures,
                            captureIndices);
                    _lineTokens.ProduceFromScopes(pushed.ContentNameScopesList, captureIndices[0].End);

                    // pop rule immediately since it is a MatchRule: the stack is the one before the push

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

        private MatchResult MatchRule(Grammar grammar, ReadOnlyMemory<char> lineText, in bool isFirstLine, in int linePos,
                StateStack stack, in int anchorPosition)
        {
            Rule rule = stack.GetRule(grammar);

            if (rule == null)
                return default;

            CompiledRule ruleScanner = rule.Compile(grammar, stack.EndRule, isFirstLine, linePos == anchorPosition);

            if (ruleScanner == null)
                return default;

            if (ruleScanner.Scanner.FindNextMatch(lineText, linePos, _capturePool, out int index, out OnigCaptureIndex[] captures))
            {
                return new MatchResult(captures, ruleScanner.Rules[index]);
            }
            return default;
        }

        private MatchResult MatchRuleOrInjections(Grammar grammar, ReadOnlyMemory<char> lineText, bool isFirstLine,
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

            MatchResult injectionResult = MatchInjections(injections, grammar, lineText, isFirstLine, linePos,
                    stack, anchorPosition);
            if (!injectionResult.IsMatch)
            {
                // No injections matched => early return
                return matchResult;
            }

            if (!matchResult.IsMatch)
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

        private MatchResult MatchInjections(List<Injection> injections, Grammar grammar, ReadOnlyMemory<char> lineText,
                bool isFirstLine, in int linePos, StateStack stack, in int anchorPosition)
        {
            // The lower the better
            int bestMatchRating = int.MaxValue;
            OnigCaptureIndex[] bestMatchCaptureIndices = null;
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
                if (!ruleScanner.Scanner.FindNextMatch(lineText, linePos, _capturePool, out int matchIndex, out OnigCaptureIndex[] matchCaptures))
                {
                    continue;
                }

                int matchRating = matchCaptures[0].Start;

                if (matchRating > bestMatchRating)
                {
                    // Injections are sorted by priority, so the previous injection had a better or
                    // equal priority
                    continue;
                }

                bestMatchRating = matchRating;
                bestMatchCaptureIndices = matchCaptures;
                bestMatchRuleId = ruleScanner.Rules[matchIndex];
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
                OnigCaptureIndex[] matchCaptureIndices = bestMatchCaptureIndices;
                bool isPriorityMatch = bestMatchResultPriority == -1;

                return new MatchResult(
                    matchCaptureIndices,
                    matchedRuleId,
                    isPriorityMatch);
            }

            return default;
        }

        private void HandleCaptures(Grammar grammar, ReadOnlyMemory<char> lineText, bool isFirstLine, ref CaptureState stack,
                LineTokens lineTokens, List<CaptureRule> captures, OnigCaptureIndex[] captureIndices)
        {
            if (captures.Count == 0)
            {
                return;
            }

            int len = Math.Min(captures.Count, captureIndices.Length);
            _localStackBuffer.Clear();
            var localStack = _localStackBuffer;
            int maxEnd = captureIndices[0].End;
            OnigCaptureIndex captureIndex;

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
                    lineTokens.ProduceFromScopes(stack.ContentNameScopesList, captureIndex.Start);
                }

                if (captureRule.RetokenizeCapturedWithRuleId != null)
                {
                    // the capture requires additional matching
                    string scopeName = captureRule.GetName(lineText, captureIndices);
                    AttributedScopeStack nameScopesList = stack.ContentNameScopesList.PushAttributed(scopeName, grammar);
                    string contentName = captureRule.GetContentName(lineText, captureIndices);
                    AttributedScopeStack contentNameScopesList = nameScopesList.PushAttributed(contentName, grammar);

                    // the capture requires additional matching
                    StateStack stackClone = stack.Get().Push(
                        captureRule.RetokenizeCapturedWithRuleId,
                        captureIndex.Start,
                        -1,
                        false,
                        null,
                        nameScopesList,
                        contentNameScopesList);

                    TokenizeString(grammar,
                            lineText.SliceAtIndexes(0, captureIndex.End),
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
                    AttributedScopeStack captureRuleScopesList = baseElement.PushAttributed(captureRuleScopeName, grammar);
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
        private WhileCheckResult CheckWhileConditions(Grammar grammar, ReadOnlyMemory<char> lineText, bool isFirstLine,
                int linePos, StateStack stack, LineTokens lineTokens)
        {
            int anchorPosition = stack.BeginRuleCapturedEOL ? 0 : -1;
            _whileRulesBuffer.Clear();
            var whileRules = _whileRulesBuffer;
            for (StateStack node = stack; node != null; node = node.Pop())
            {
                Rule nodeRule = node.GetRule(grammar);
                if (nodeRule is BeginWhileRule beginWhileRule)
                {
                    whileRules.Add(new WhileStack(node, beginWhileRule));
                }
            }
            for (int i = whileRules.Count - 1; i >= 0; i--)
            {
                WhileStack whileRule = whileRules[i];
                CompiledRule ruleScanner = whileRule.Rule.CompileWhile(whileRule.Stack.EndRule, isFirstLine,
                        anchorPosition == linePos);
                if (ruleScanner.Scanner.FindNextMatch(lineText, linePos, _capturePool, out int whileIndex, out OnigCaptureIndex[] whileCaptures))
                {
                    RuleId matchedRuleId = ruleScanner.Rules[whileIndex];
                    if (RuleId.WHILE_RULE.NotEquals(matchedRuleId))
                    {
                        // we shouldn't end up here
                        stack = whileRule.Stack.Pop();
                        break;
                    }
                    if (whileCaptures.Length > 0)
                    {
                        lineTokens.Produce(whileRule.Stack, whileCaptures[0].Start);
                        var whileState = new CaptureState(whileRule.Stack);
                        HandleCaptures(grammar, lineText, isFirstLine, ref whileState, lineTokens,
                                whileRule.Rule.WhileCaptures, whileCaptures);
                        lineTokens.Produce(whileRule.Stack, whileCaptures[0].End);
                        anchorPosition = whileCaptures[0].End;
                        if (whileCaptures[0].End > linePos)
                        {
                            linePos = whileCaptures[0].End;
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

        internal static TokenizeStringResult TokenizeString(Grammar grammar, ReadOnlyMemory<char> lineText, bool isFirstLine, int linePos,
                StateStack stack, LineTokens lineTokens, bool checkWhileConditions, TimeSpan timeLimit)
        {
            LineTokenizer tokenizer = grammar.EnterTokenizer();
            tokenizer.Start(grammar, lineText, isFirstLine, linePos, stack, lineTokens);
            try
            {
                return tokenizer.Scan(checkWhileConditions, timeLimit);
            }
            finally
            {
                tokenizer.Finish();
                grammar.ExitTokenizer();
            }
        }

        readonly struct WhileStack
        {
            internal StateStack Stack { get; }
            internal BeginWhileRule Rule { get; }

            internal WhileStack(StateStack stack, BeginWhileRule rule)
            {
                Stack = stack;
                Rule = rule;
            }
        }

        // A state for the captures of a match: an existing state, or one pushed (or with other content scopes)
        // that is only created when needed, as match rules pop right away.
        struct CaptureState
        {
            private readonly StateStack _parent;
            private readonly RuleId _ruleId;
            private readonly int _enterPos;
            private readonly int _anchorPos;
            private readonly bool _beginRuleCapturedEOL;
            private readonly string _endRule;
            private readonly AttributedScopeStack _nameScopesList;
            private StateStack _stack;

            internal CaptureState(StateStack stack)
            {
                _stack = stack;
                _parent = null;
                _ruleId = null;
                _enterPos = 0;
                _anchorPos = 0;
                _beginRuleCapturedEOL = false;
                _endRule = null;
                _nameScopesList = null;
                ContentNameScopesList = stack.ContentNameScopesList;
            }

            // A push on parent with the same name and content scopes.
            internal CaptureState(StateStack parent, RuleId ruleId, int enterPos, int anchorPos, bool beginRuleCapturedEOL,
                AttributedScopeStack nameScopesList)
            {
                _stack = null;
                _parent = parent;
                _ruleId = ruleId;
                _enterPos = enterPos;
                _anchorPos = anchorPos;
                _beginRuleCapturedEOL = beginRuleCapturedEOL;
                _endRule = null;
                _nameScopesList = nameScopesList;
                ContentNameScopesList = nameScopesList;
            }

            // stack.WithContentNameScopesList(contentNameScopesList).
            internal CaptureState(StateStack stack, AttributedScopeStack contentNameScopesList)
            {
                if (AttributedScopeStack.Equals(stack.ContentNameScopesList, contentNameScopesList))
                {
                    this = new CaptureState(stack);
                    return;
                }

                _stack = null;
                _parent = stack.Parent;
                _ruleId = stack.RuleId;
                _enterPos = stack.GetEnterPos();
                _anchorPos = stack.GetAnchorPos();
                _beginRuleCapturedEOL = stack.BeginRuleCapturedEOL;
                _endRule = stack.EndRule;
                _nameScopesList = stack.NameScopesList;
                ContentNameScopesList = contentNameScopesList;
            }

            internal AttributedScopeStack ContentNameScopesList { get; }

            internal StateStack Get()
            {
                return _stack ??= _parent.Push(_ruleId, _enterPos, _anchorPos, _beginRuleCapturedEOL, _endRule,
                    _nameScopesList, ContentNameScopesList);
            }
        }

        readonly struct WhileCheckResult
        {
            internal StateStack Stack { get; }
            internal int LinePos { get; }
            internal int AnchorPosition { get; }
            internal bool IsFirstLine { get; }

            internal WhileCheckResult(StateStack stack, int linePos, int anchorPosition, bool isFirstLine)
            {
                Stack = stack;
                LinePos = linePos;
                AnchorPosition = anchorPosition;
                IsFirstLine = isFirstLine;
            }
        }
    }
}