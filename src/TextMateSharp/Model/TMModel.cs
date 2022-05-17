using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public class TMModel : ITMModel
    {
        private const int MAX_LEN_TO_TOKENIZE = 10000;
        private IGrammar _grammar;
        private List<IModelTokensChangedListener> listeners;
        private Tokenizer _tokenizer;
        private TokenizerThread _thread;
        private IModelLines _lines;
        private Queue<int> _invalidLines = new Queue<int>();
        private object _lock = new object();
        private ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public TMModel(IModelLines lines)
        {
            this.listeners = new List<IModelTokensChangedListener>();
            this._lines = lines;
            ((AbstractLineList)lines).SetModel(this);
        }

        public bool IsStopped
        {
            get { return this._thread == null || this._thread.IsStopped; }
        }

        class TokenizerThread
        {
            public volatile bool IsStopped;

            private string name;
            private TMModel model;
            private TMState lastState;

            public TokenizerThread(string name, TMModel model)
            {
                this.name = name;
                this.model = model;
                this.IsStopped = true;
            }

            public void Run()
            {
                IsStopped = false;

                ThreadPool.QueueUserWorkItem(ThreadWorker);
            }

            public void Stop()
            {
                IsStopped = true;
            }

            void ThreadWorker(object state)
            {
                if (IsStopped)
                {
                    return;
                }

                do
                {
                    int toProcess = -1;

                    if (model._grammar.IsCompiling)
                    {
                        this.model._resetEvent.Reset();
                        this.model._resetEvent.WaitOne();
                        continue;
                    }

                    lock (this.model._lock)
                    {
                        if (model._invalidLines.Count > 0)
                        {
                            toProcess = model._invalidLines.Dequeue();
                        }
                    }

                    if (toProcess == -1)
                    {
                        this.model._resetEvent.Reset();
                        this.model._resetEvent.WaitOne();
                        continue;
                    }

                    var modelLine = model._lines.Get(toProcess);

                    if (modelLine != null && modelLine.IsInvalid)
                    {
                        try
                        {
                            this.RevalidateTokensNow(toProcess, null);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e.Message);

                            if (toProcess < model._lines.GetNumberOfLines())
                            {
                                model.InvalidateLine(toProcess);
                            }
                        }
                    }
                } while (!IsStopped && model._thread != null);
            }

            private void RevalidateTokensNow(int startLine, int? toLineIndexOrNull)
            {
                if (model._tokenizer == null)
                    return;

                model.BuildEventWithCallback(eventBuilder =>
                {
                    int toLineIndex = toLineIndexOrNull ?? 0;
                    if (toLineIndexOrNull == null || toLineIndex >= model._lines.GetNumberOfLines())
                    {
                        toLineIndex = model._lines.GetNumberOfLines() - 1;
                    }

                    long tokenizedChars = 0;
                    long currentCharsToTokenize = 0;
                    long MAX_ALLOWED_TIME = 5;
                    long currentEstimatedTimeToTokenize = 0;
                    long elapsedTime;
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    // Tokenize at most 1000 lines. Estimate the tokenization speed per
                    // character and stop when:
                    // - MAX_ALLOWED_TIME is reached
                    // - tokenizing the next line would go above MAX_ALLOWED_TIME

                    int lineIndex = startLine;
                    while (lineIndex <= toLineIndex && lineIndex < model.GetLines().GetNumberOfLines())
                    {
                        elapsedTime = stopwatch.ElapsedMilliseconds;
                        if (elapsedTime > MAX_ALLOWED_TIME)
                        {
                            // Stop if MAX_ALLOWED_TIME is reached
                            model.InvalidateLine(lineIndex);
                            return;
                        }
                        // Compute how many characters will be tokenized for this line
                        try
                        {
                            currentCharsToTokenize = model._lines.GetLineLength(lineIndex);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                        if (tokenizedChars > 0)
                        {
                            // If we have enough history, estimate how long tokenizing this line would take
                            currentEstimatedTimeToTokenize = (long)((double)elapsedTime / tokenizedChars) * currentCharsToTokenize;
                            if (elapsedTime + currentEstimatedTimeToTokenize > MAX_ALLOWED_TIME)
                            {
                                // Tokenizing this line will go above MAX_ALLOWED_TIME
                                model.InvalidateLine(lineIndex);
                                return;
                            }
                        }

                        lineIndex = this.UpdateTokensInRange(eventBuilder, lineIndex, lineIndex) + 1;
                        tokenizedChars += currentCharsToTokenize;
                    }
                });
            }

            public int UpdateTokensInRange(ModelTokensChangedEventBuilder eventBuilder, int startIndex,
                int endLineIndex)
            {
                int nextInvalidLineIndex = startIndex;
                int lineIndex = startIndex;
                while (lineIndex <= endLineIndex && lineIndex < model._lines.GetNumberOfLines())
                {
                    if (model._grammar != null && model._grammar.IsCompiling)
                    {
                        lineIndex++;
                        continue;
                    }

                    int endStateIndex = lineIndex + 1;
                    LineTokens r = null;
                    string text = null;
                    ModelLine modeLine = model._lines.Get(lineIndex);
                    try
                    {
                        text = model._lines.GetLineText(lineIndex);
                        if (text == null)
                            continue;
                        // Tokenize only the first X characters
                        r = model._tokenizer.Tokenize(text, modeLine.State, 0, MAX_LEN_TO_TOKENIZE);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                        lineIndex++;
                        continue;
                    }

                    if (r != null && r.Tokens != null && r.Tokens.Count != 0)
                    {
                        // Cannot have a stop offset before the last token
                        r.ActualStopOffset = Math.Max(r.ActualStopOffset, r.Tokens[r.Tokens.Count - 1].StartIndex + 1);
                    }

                    if (r != null && r.ActualStopOffset < text.Length)
                    {
                        // Treat the rest of the line (if above limit) as one default token
                        r.Tokens.Add(new TMToken(r.ActualStopOffset, new List<string>()));
                        // Use as end state the starting state
                        r.EndState = modeLine.State;
                    }

                    if (r == null)
                    {
                        r = new LineTokens(new List<TMToken>() { new TMToken(0, new List<string>()) }, text.Length,
                            modeLine.State);
                    }

                    modeLine.Tokens = r.Tokens;
                    eventBuilder.registerChangedTokens(lineIndex + 1);
                    modeLine.IsInvalid = false;

                    if (endStateIndex < model._lines.GetNumberOfLines())
                    {
                        ModelLine endStateLine = model._lines.Get(endStateIndex);
                        if (endStateLine.State != null && r.EndState.Equals(endStateLine.State))
                        {
                            // The end state of this line remains the same
                            nextInvalidLineIndex = lineIndex + 1;
                            while (nextInvalidLineIndex < model._lines.GetNumberOfLines())
                            {
                                bool isLastLine = nextInvalidLineIndex + 1 >= model._lines.GetNumberOfLines();
                                if (model._lines.Get(nextInvalidLineIndex).IsInvalid
                                    || (!isLastLine && model._lines.Get(nextInvalidLineIndex + 1).State == null)
                                    || (isLastLine && this.lastState == null))
                                {
                                    break;
                                }

                                nextInvalidLineIndex++;
                            }

                            lineIndex = nextInvalidLineIndex;
                        }
                        else
                        {
                            endStateLine.State = r.EndState;
                            lineIndex++;
                        }
                    }
                    else
                    {
                        this.lastState = r.EndState;
                        lineIndex++;
                    }
                }

                return nextInvalidLineIndex;
            }
        }

        public IGrammar GetGrammar()
        {
            return _grammar;
        }

        public void SetGrammar(IGrammar grammar)
        {
            if (!Object.Equals(grammar, this._grammar))
            {
                Stop();

                this._grammar = grammar;
                _lines.ForEach((line) => line.ResetTokenizationState());

                if (grammar != null)
                {
                    this._tokenizer = new Tokenizer(grammar);
                    _lines.Get(0).State = _tokenizer.GetInitialState();
                    Start();
                    InvalidateLine(0);
                }
            }
        }

        public void AddModelTokensChangedListener(IModelTokensChangedListener listener)
        {
            if (this._grammar != null)
                Start();

            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        public void RemoveModelTokensChangedListener(IModelTokensChangedListener listener)
        {
            listeners.Remove(listener);
            if (listeners.Count == 0)
            {
                // no need to keep tokenizing if no-one cares
                Stop();
            }
        }

        public void Dispose()
        {
            listeners.Clear();
            Stop();
            GetLines().Dispose();
        }

        private void Stop()
        {
            if (_thread == null)
            {
                return;
            }

            this._thread.Stop();
            _resetEvent.Set();
            this._thread = null;
        }

        private void Start()
        {
            if (this._thread == null || this._thread.IsStopped)
            {
                this._thread = new TokenizerThread("TMModelThread", this);
            }

            if (this._thread.IsStopped)
            {
                this._thread.Run();
            }
        }

        private void BuildEventWithCallback(Action<ModelTokensChangedEventBuilder> callback)
        {
            if (this._thread == null || this._thread.IsStopped)
                return;

            ModelTokensChangedEventBuilder eventBuilder = new ModelTokensChangedEventBuilder(this);

            callback(eventBuilder);

            ModelTokensChangedEvent e = eventBuilder.Build();
            if (e != null)
            {
                this.Emit(e);
            }
        }

        private void Emit(ModelTokensChangedEvent e)
        {
            foreach (IModelTokensChangedListener listener in listeners)
            {
                try
                {
                    listener.ModelTokensChanged(e);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        public void ForceTokenization(int lineIndex)
        {
            if (_grammar == null)
                return;

            this.BuildEventWithCallback(eventBuilder =>
                this._thread.UpdateTokensInRange(eventBuilder, lineIndex, lineIndex)
            );
        }

        public void ForceTokenization(int startLineIndex, int endLineIndex)
        {
            if (_grammar == null)
                return;

            this.BuildEventWithCallback(eventBuilder =>
                this._thread.UpdateTokensInRange(eventBuilder, startLineIndex, endLineIndex)
            );
        }

        public List<TMToken> GetLineTokens(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex > _lines.GetNumberOfLines() - 1)
                return null;

            return _lines.Get(lineIndex).Tokens;
        }

        public bool IsLineInvalid(int lineIndex)
        {
            return _lines.Get(lineIndex).IsInvalid;
        }

        public void InvalidateLine(int lineIndex)
        {
            this._lines.Get(lineIndex).IsInvalid = true;

            lock (_lock)
            {
                this._invalidLines.Enqueue(lineIndex);
                _resetEvent.Set();
            }
        }

        public void InvalidateLineRange(int iniLineIndex, int endLineIndex)
        {
            lock (_lock)
            {
                for (int i = iniLineIndex; i <= endLineIndex; i++)
                {
                    this._lines.Get(i).IsInvalid = true;
                    this._invalidLines.Enqueue(i);
                }
                _resetEvent.Set();
            }
        }

        public IModelLines GetLines()
        {
            return this._lines;
        }
    }
}