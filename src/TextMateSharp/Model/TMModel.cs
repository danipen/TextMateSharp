using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    /// <summary>
    /// Represents a tokenization model that manages the tokenization of lines of text using a specified grammar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The TMModel class provides mechanisms for tokenizing text lines based on a grammar,
    /// supporting dynamic grammar updates, line invalidation, and asynchronous processing through a dedicated tokenizer
    /// thread. It allows clients to listen for tokenization changes and ensures thread safety for concurrent
    /// operations. The model is designed to be disposed of safely and will stop processing when disposed. Tokenization
    /// is performed efficiently, with safeguards for long lines and performance-sensitive scenarios.
    /// </para>
    /// <para>
    /// <b>Threading model:</b> The document is edited by the host (typically on the UI thread) via the supplied
    /// <see cref="IModelLines"/> implementation, while tokenization work is performed by an internal background
    /// <see cref="TokenizerThread"/>.
    /// </para>
    /// <para>
    /// <b>Synchronization:</b> TMModel protects its own lifecycle and tokenization state with an internal lock (<see cref="_lock"/>),
    /// and <see cref="AbstractLineList"/> protects its internal list with <see cref="AbstractLineList.mLock"/>.
    /// </para>
    /// <para>
    /// <b>Lock ordering:</b> To avoid deadlocks, TMModel code acquires <see cref="_lock"/> before calling into
    /// <see cref="IModelLines"/> (which may acquire <see cref="AbstractLineList.mLock"/>),
    /// i.e. <see cref="_lock"/> -> <see cref="AbstractLineList.mLock"/>. Implementations of
    /// <see cref="AbstractLineList"/> must not call back into TMModel
    /// (e.g. via <see cref="AbstractLineList.InvalidateLine(int)"/> / <see cref="AbstractLineList.ForceTokenization(int,int)"/>)
    /// while holding <see cref="AbstractLineList.mLock"/>.
    /// </para>
    /// <para>
    /// <b>Listeners:</b> The <see cref="listeners"/> collection is mutated under <c>lock(listeners)</c>, but listener callbacks are invoked
    /// outside that lock (using a snapshot) to avoid deadlocks and re-entrancy issues.
    /// </para>
    /// </remarks>
    public class TMModel : ITMModel
    {
        private const int MAX_LEN_TO_TOKENIZE = 10000;
        private IGrammar _grammar;
        private readonly List<IModelTokensChangedListener> listeners;
        private Tokenizer _tokenizer;
        private TokenizerThread _thread;
        private readonly IModelLines _lines;
        private readonly Queue<int> _invalidLines = new Queue<int>();
        private readonly object _lock = new object();
        private int _isDisposedFlag; // atomic disposed state (0 = not disposed, 1 = disposed)
        private int _grammarEpoch;

        private bool IsDisposed => Volatile.Read(ref _isDisposedFlag) != 0;

        public TMModel(IModelLines lines)
        {
            this.listeners = new List<IModelTokensChangedListener>();
            this._lines = lines;
            ((AbstractLineList)lines).SetModel(this);
        }

        public bool IsStopped
        {
            get
            {
                lock (_lock)
                {
                    return this._thread == null || this._thread.IsStopped;
                }
            }
        }

        sealed class TokenizerThread : IDisposable
        {
            // code depending on IsStopped alone may race. IsDisposed is the canonical
            // disposed state and IsStopped is for informational purposes only
            public volatile bool IsStopped;

            private string name;
            private TMModel model;
            private TMState lastState;
            private AutoResetEvent _workAvailable;
            private CancellationTokenSource _cts;
            private Task _task;
            private int _isDisposedFlag; // atomic disposed state (0 = not disposed, 1 = disposed)
            private readonly object _lifecycleLock = new object();

            private bool IsDisposed => Volatile.Read(ref _isDisposedFlag) != 0;

            public TokenizerThread(string name, TMModel model)
            {
                this.name = name;
                this.model = model;
                this.IsStopped = true;
                this._workAvailable = new AutoResetEvent(false);
                this._cts = new CancellationTokenSource();
            }

            // FIX #6 (updated): Guard Run() state transitions under a dedicated lifecycle lock
            // to prevent check-then-act races between Run/Dispose/DisposeAfterCompletion.
            public void Run()
            {
                CancellationToken token;

                if (IsDisposed)
                    return;

                lock (_lifecycleLock)
                {
                    if (IsDisposed)
                        return;

                    // Prevent double-start (or a start racing with DisposeAfterCompletion deciding _task is null).
                    if (_task != null)
                        return;

                    CancellationTokenSource cts = _cts;
                    if (cts == null)
                        return;

                    IsStopped = false;
                    token = cts.Token;

                    _task = Task.Factory.StartNew(
                        () => ThreadWorker(token),
                        token,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
                }
            }

            public void Stop()
            {
                IsStopped = true;

                CancellationTokenSource cts;
                lock (_lifecycleLock)
                {
                    cts = _cts;
                }

                try
                {
                    cts?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // CancellationTokenSource was already disposed - ignore
                }

                Signal();
            }

            public void Signal()
            {
                AutoResetEvent workAvailable;
                lock (_lifecycleLock)
                {
                    workAvailable = _workAvailable;
                }

                try
                {
                    workAvailable?.Set();
                }
                catch (ObjectDisposedException)
                {
                    // AutoResetEvent was already disposed - ignore
                }
            }

            /// <summary>
            /// Initiates non-blocking cleanup of this thread instance.
            /// If the worker task is still running, disposal is deferred
            /// until the task completes via a continuation. This method
            /// never blocks the calling thread.
            /// </summary>
            public void DisposeAfterCompletion()
            {
                Task task;
                lock (_lifecycleLock)
                {
                    task = _task;
                }

                if (task != null)
                {
                    // Observe any fault on the worker task itself
                    ObserveTask(task);

                    // When the worker task completes, dispose this instance
                    Task continuation = task.ContinueWith(_ =>
                    {
                        try
                        {
                            Dispose();
                        }
                        catch (Exception e)
                        {
                            // Dispose should not throw, but guard against it
                            Trace.TraceError($"Exception occurred while disposing TokenizerThread '{name}' after task completion: {e.Message}");
                        }
                    }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);

                    // Observe the continuation so its own faults don't go unobserved
                    ObserveTask(continuation);
                }
                else
                {
                    try
                    {
                        Dispose();
                    }
                    catch (Exception e)
                    {
                        // Dispose should not throw, but guard against it
                        Trace.TraceError($"Exception occurred while disposing TokenizerThread '{name}' with no task: {e.Message}");
                    }
                }
            }

            // FIX #3: Use Interlocked.CompareExchange for atomic dispose guard.
            // volatile bool check-then-set is not atomic - two threads can both
            // read false and proceed, causing double-dispose of CTS and AutoResetEvent.
            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _isDisposedFlag, 1, 0) != 0)
                    return;

                IsStopped = true;

                CancellationTokenSource cts;
                AutoResetEvent workAvailable;

                lock (_lifecycleLock)
                {
                    cts = _cts;
                    workAvailable = _workAvailable;

                    // Prevent further use by other racing calls (Signal/Stop/Run/DisposeAfterCompletion).
                    _cts = null;
                    _workAvailable = null;
                    _task = null;
                }

                try
                {
                    cts?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // CancellationTokenSource was already disposed - ignore
                }

                // Signal the AutoResetEvent before disposing it so any thread
                // blocked on WaitAny wakes up and can exit cleanly.
                try
                {
                    workAvailable?.Set();
                }
                catch (ObjectDisposedException)
                {
                    // AutoResetEvent was already disposed - ignore
                }

                try
                {
                    cts?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // CancellationTokenSource was already disposed - ignore
                }

                try
                {
                    workAvailable?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // AutoResetEvent was already disposed - ignore
                }
            }

            /// <summary>
            /// Observes a task's exception to prevent UnobservedTaskException.
            /// Accessing Task.Exception marks the exception as observed in the TPL.
            /// This is the canonical .NET pattern - the property getter sets an
            /// internal "observed" flag that cannot be compiled away by the
            /// compiler or JIT, because it is a side-effecting property access.
            /// </summary>
            private static void ObserveTask(Task task)
            {
                task?.ContinueWith(
                    t =>
                    {
                        // Intentionally access Exception for its side effect: TPL marks the exception
                        // as observed, preventing UnobservedTaskException
                        _ = t.Exception;
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }

            void ThreadWorker(CancellationToken cancellationToken)
            {
                if (IsStopped)
                {
                    return;
                }

                // Snapshot the wait handles at entry - these are owned by this
                // TokenizerThread instance and will not be replaced (only disposed
                // after this task completes via DisposeAfterCompletion).
                AutoResetEvent workAvailable;
                lock (_lifecycleLock)
                {
                    workAvailable = _workAvailable;
                }

                if (workAvailable == null)
                {
                    return;
                }

                // NOTE on CancellationToken.WaitHandle:
                // ThreadWorker waits on both _workAvailable and the cancellation token's wait handle.
                // CancellationToken.WaitHandle is lazily created (may allocate a WaitHandle on first access),
                // but it is owned by this TokenizerThread's CancellationTokenSource (_cts).
                // We intentionally do NOT dispose cancellationToken.WaitHandle directly; disposing _cts in Dispose()
                // releases any resources associated with the token (including its underlying wait handle).
                WaitHandle cancellationHandle = cancellationToken.WaitHandle;
                WaitHandle[] waitHandles = new WaitHandle[] { workAvailable, cancellationHandle };

                try
                {
                    do
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // Check if this thread instance has been replaced by SetGrammar/Stop.
                        // Reading _thread under _lock prevents a stale read.
                        lock (model._lock)
                        {
                            if (model._thread != this)
                                break;
                        }

                        int toProcess = -1;

                        // Snapshot _grammar under lock so we get a consistent read.
                        // SetGrammar can change _grammar on the UI thread at any time.
                        IGrammar grammar;

                        lock (this.model._lock)
                        {
                            grammar = model._grammar;
                        }

                        if (grammar != null && grammar.IsCompiling)
                        {
                            try
                            {
                                WaitHandle.WaitAny(waitHandles);
                            }
                            catch (ObjectDisposedException)
                            {
                                break;
                            }
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
                            try
                            {
                                WaitHandle.WaitAny(waitHandles);
                            }
                            catch (ObjectDisposedException)
                            {
                                break;
                            }
                            continue;
                        }

                        IModelLines lines = model._lines;

                        bool isInvalid;
                        lock (model._lock)
                        {
                            // If Stop()/SetGrammar swapped out the thread, exit quickly
                            if (model._thread != this)
                                break;

                            ModelLine modelLine = lines.Get(toProcess);
                            isInvalid = modelLine != null && modelLine.IsInvalid;
                        }

                        if (!isInvalid)
                            continue;

                        try
                        {
                            this.RevalidateTokens(toProcess, null);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e.Message);

                            if (toProcess < lines.GetNumberOfLines())
                            {
                                model.InvalidateLine(toProcess);
                            }
                        }
                    } while (!IsStopped && !cancellationToken.IsCancellationRequested && !ShouldStop());
                }
                catch (ObjectDisposedException)
                {
                    // WaitHandle or CancellationTokenSource was disposed - exit gracefully
                }
                catch (OperationCanceledException)
                {
                    // CancellationToken was cancelled - exit gracefully
                }
                catch (Exception e)
                {
                    // Last-resort safety net - exit rather than crash
                    Trace.TraceError($"Unexpected exception in TokenizerThread '{name}': {e.Message}");
                }
            }

            /// <summary>
            /// Checks whether this thread instance has been replaced.
            /// Must read model._thread under lock to prevent stale reads.
            /// </summary>
            private bool ShouldStop()
            {
                lock (model._lock)
                {
                    return model._thread != this;
                }
            }

            Stopwatch _stopwatch = new Stopwatch();

            private void RevalidateTokens(int startLine, int? toLineIndexOrNull)
            {
                Tokenizer tokenizer;

                lock (model._lock)
                {
                    tokenizer = model._tokenizer;
                }

                if (tokenizer == null)
                    return;

                model.BuildEventWithCallback(eventBuilder =>
                {
                    IModelLines lines = model._lines;

                    int toLineIndex = toLineIndexOrNull ?? 0;
                    if (toLineIndexOrNull == null || toLineIndex >= lines.GetNumberOfLines())
                    {
                        toLineIndex = lines.GetNumberOfLines() - 1;
                    }

                    long tokenizedChars = 0;
                    long currentCharsToTokenize = 0;
                    long MAX_ALLOWED_TIME = 5;
                    long currentEstimatedTimeToTokenize = 0;
                    long elapsedTime;
                    _stopwatch.Restart();
                    // Tokenize at most 1000 lines. Estimate the tokenization speed per
                    // character and stop when:
                    // - MAX_ALLOWED_TIME is reached
                    // - tokenizing the next line would go above MAX_ALLOWED_TIME

                    int lineIndex = startLine;
                    while (lineIndex <= toLineIndex && lineIndex < lines.GetNumberOfLines())
                    {
                        elapsedTime = _stopwatch.ElapsedMilliseconds;
                        if (elapsedTime > MAX_ALLOWED_TIME)
                        {
                            // Stop if MAX_ALLOWED_TIME is reached
                            model.InvalidateLine(lineIndex);
                            return;
                        }
                        // Compute how many characters will be tokenized for this line
                        try
                        {
                            currentCharsToTokenize = lines.GetLineLength(lineIndex);
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
                // Snapshot _grammar, _tokenizer, and _lines so we have consistent
                // references. SetGrammar can null _grammar/_tokenizer on the UI
                // thread at any time. _lines is assigned once in the constructor
                // but we snapshot for consistency and to avoid repeated field reads
                IGrammar grammar;
                Tokenizer tokenizer;
                IModelLines lines;
                int expectedGrammarEpoch;

                lock (model._lock)
                {
                    grammar = model._grammar;
                    tokenizer = model._tokenizer;
                    lines = model._lines;
                    expectedGrammarEpoch = model._grammarEpoch;
                }

                if (tokenizer == null || lines == null)
                    return startIndex;

                TimeSpan stopLineTokenizationAfter = TimeSpan.FromMilliseconds(3000);
                int nextInvalidLineIndex = startIndex;
                int lineIndex = startIndex;
                while (lineIndex <= endLineIndex && lineIndex < lines.GetNumberOfLines())
                {
                    if (grammar != null && grammar.IsCompiling)
                    {
                        lineIndex++;
                        continue;
                    }

                    int endStateIndex = lineIndex + 1;
                    LineTokens r = null;
                    LineText text = default;
                    ModelLine modelLine = lines.Get(lineIndex);

                    if (modelLine == null)
                    {
                        lineIndex++;
                        continue;
                    }

                    TMState startingState = null;
                    try
                    {
                        text = lines.GetLineTextIncludingTerminators(lineIndex);
                        // Tokenize only the first X characters

                        lock (model._lock)
                        {
                            // If the model was disposed/stopped or grammar swapped while we were preparing work,
                            // do not use a potentially stale ModelLine state.
                            if (model._thread != this ||
                                model._grammarEpoch != expectedGrammarEpoch ||
                                !object.ReferenceEquals(model._tokenizer, tokenizer))
                            {
                                model.InvalidateLine(lineIndex);
                                return nextInvalidLineIndex;
                            }

                            startingState = modelLine.State;
                        }

                        r = tokenizer.Tokenize(text, startingState, 0, MAX_LEN_TO_TOKENIZE, stopLineTokenizationAfter);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                        lineIndex++;
                        continue;
                    }

                    if (r != null && r.Tokens != null && r.Tokens.Count != 0)
                    {
                        TMToken tmToken = r.Tokens[r.Tokens.Count - 1];
                        if (tmToken != null)
                        {
                            // Cannot have a stop offset before the last token
                            r.ActualStopOffset = Math.Max(r.ActualStopOffset, tmToken.StartIndex + 1);
                        }
                    }

                    if (r != null && r.ActualStopOffset < text.Length && r.Tokens != null)
                    {
                        // Treat the rest of the line (if above limit) as one default token
                        r.Tokens.Add(new TMToken(r.ActualStopOffset, new List<string>()));
                        // Use as end state the starting state
                        r.EndState = startingState;
                    }

                    if (r == null)
                    {
                        r = new LineTokens(new List<TMToken>() { new TMToken(0, new List<string>()) }, text.Length,
                            startingState);
                    }

                    // Hold _lock across both the epoch check AND all state writes so
                    // that SetGrammar cannot reset lines between the check and the
                    // writes. The work inside the lock is only field assignments and
                    // _lines.Get calls (which acquire AbstractLineList.mLock - same
                    // lock ordering as everywhere else: _lock -> mLock, never reversed).
                    lock (model._lock)
                    {
                        if (model._thread != this || model._grammarEpoch != expectedGrammarEpoch || !object.ReferenceEquals(model._tokenizer, tokenizer))
                        {
                            // Stale result (grammar/tokenizer/thread changed). Re-queue under the new epoch.
                            // InvalidateLine re-acquires _lock, which is safe because Monitor is reentrant.
                            model.InvalidateLine(lineIndex);
                            return nextInvalidLineIndex;
                        }

                        modelLine.Tokens = r.Tokens;
                        eventBuilder.registerChangedTokens(lineIndex + 1);
                        modelLine.IsInvalid = false;

                        if (endStateIndex < lines.GetNumberOfLines())
                        {
                            ModelLine endStateLine = lines.Get(endStateIndex);
                            if (endStateLine?.State != null && r.EndState != null && r.EndState.Equals(endStateLine.State))
                            {
                                // The end state of this line remains the same
                                nextInvalidLineIndex = lineIndex + 1;
                                while (nextInvalidLineIndex < lines.GetNumberOfLines())
                                {
                                    bool isLastLine = nextInvalidLineIndex + 1 >= lines.GetNumberOfLines();
                                    ModelLine nextLine = lines.Get(nextInvalidLineIndex);
                                    ModelLine nextNextLine = !isLastLine ? lines.Get(nextInvalidLineIndex + 1) : null;
                                    if (nextLine == null || nextLine.IsInvalid
                                        || (!isLastLine && (nextNextLine == null || nextNextLine.State == null))
                                        || (isLastLine && this.lastState == null))
                                    {
                                        break;
                                    }

                                    nextInvalidLineIndex++;
                                }

                                lineIndex = nextInvalidLineIndex;
                            }
                            else if (endStateLine != null)
                            {
                                endStateLine.State = r.EndState;
                                lineIndex++;
                            }
                            else
                            {
                                lineIndex++;
                            }
                        }
                        else
                        {
                            this.lastState = r.EndState;
                            lineIndex++;
                        }
                    }
                }

                return nextInvalidLineIndex;
            }
        }

        public IGrammar GetGrammar()
        {
            if (IsDisposed)
                return null;

            lock (_lock)
            {
                return _grammar;
            }
        }

        // FIX #1: Merge SetGrammar into a single lock region to eliminate the
        // split-lock gap where concurrent SetGrammar calls could orphan threads.
        // The old thread is stopped and disposed AFTER the lock, identical to
        // how Stop() already works. The old thread will self-exit via the
        // model._thread != this check in its worker loop.
        public void SetGrammar(IGrammar grammar)
        {
            if (IsDisposed)
                return;

            TokenizerThread oldThread = null;
            TokenizerThread newThreadToRun = null;
            bool shouldInvalidate = false;
            int endLineIndex = 0;

            // Single lock: snapshot old thread, bump epoch, apply new grammar
            // state, and install new thread - all atomically.
            lock (_lock)
            {
                if (IsDisposed)
                    return;

                if (Object.Equals(grammar, this._grammar))
                    return;

                Interlocked.Increment(ref _grammarEpoch);
                oldThread = _thread;
                _thread = null;

                this._grammar = grammar;
                _lines.ForEach((line) => line.ResetTokenizationState());
                _invalidLines.Clear();

                if (grammar != null)
                {
                    this._tokenizer = new Tokenizer(grammar);
                    ModelLine firstLine = _lines.Get(0);
                    if (firstLine != null)
                    {
                        firstLine.State = _tokenizer.GetInitialState();
                    }

                    this._thread = new TokenizerThread("TMModelThread", this);
                    newThreadToRun = this._thread;
                    shouldInvalidate = true;
                }
                else
                {
                    this._tokenizer = null;
                    endLineIndex = _lines.GetNumberOfLines() - 1;
                }
            }

            newThreadToRun?.Run();

            // Outside lock: stop and dispose old thread (non-blocking)
            // The old worker will see IsStopped/CancellationRequested/_thread!=this
            // and exit its loop, then disposal is handled after the task completes
            if (oldThread != null)
            {
                oldThread.Stop();
                oldThread.DisposeAfterCompletion();
            }

            // Outside lock: signal work or emit event
            if (shouldInvalidate)
            {
                InvalidateLine(0);
            }
            else
            {
                Emit(new ModelTokensChangedEvent(new Range(0, endLineIndex), this));
            }
        }

        // FIX #5: Re-check IsDisposed inside lock(listeners) to prevent adding
        // a listener to a disposed model if Dispose() runs between the two locks.
        // FIX #4: Handle old stopped thread returned by StartInternal - dispose
        // it outside _lock to prevent resource leaks.
        // FIX #7: Lock ordering is now safe - _lock is always acquired before
        // listeners (never reversed) because RemoveModelTokensChangedListener
        // no longer calls Stop() inside lock(listeners).
        public void AddModelTokensChangedListener(IModelTokensChangedListener listener)
        {
            if (IsDisposed)
                return;

            TokenizerThread oldThreadToDispose = null;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                lock (listeners)
                {
                    if (IsDisposed)
                        return;

                    if (!listeners.Contains(listener))
                    {
                        listeners.Add(listener);
                    }
                }

                // If grammar is set, ensure the tokenizer thread is running now that someone is listening.
                if (this._grammar != null)
                {
                    oldThreadToDispose = StartInternal();
                }
            }

            // Dispose any old stopped thread outside _lock to avoid blocking the model's critical section
            if (oldThreadToDispose != null)
            {
                oldThreadToDispose.Stop();
                oldThreadToDispose.DisposeAfterCompletion();
            }
        }

        // FIX #7: Move Stop() outside lock(listeners) to eliminate lock-order
        // inversion. Previously: listeners -> _lock (via Stop()). Now: compute
        // decision under lock(listeners), release, then call Stop() which takes
        // _lock - consistent with the _lock -> listeners ordering everywhere else.
        public void RemoveModelTokensChangedListener(IModelTokensChangedListener listener)
        {
            if (IsDisposed)
                return;

            bool shouldStop = false;

            // Make remove + stop decision atomic relative to Add by synchronizing under _lock.
            // Correct lock ordering is: _lock -> listeners.
            lock (_lock)
            {
                if (IsDisposed)
                    return;

                lock (listeners)
                {
                    listeners.Remove(listener);
                    if (listeners.Count == 0)
                    {
                        // no need to keep tokenizing if no-one cares
                        shouldStop = true;
                    }
                }

                if (shouldStop)
                {
                    // Stop internally takes _lock, but Monitor is re-entrant
                    // This keeps the decision and action atomic relative to other operations guarded by _lock
                    Stop();
                }
            }
        }

        // FIX #2: Use Interlocked.CompareExchange for atomic dispose guard.
        // volatile bool check-then-set is not atomic - two threads can both
        // read false and proceed, causing double-Stop(), double-Clear(), and
        // double-GetLines().Dispose().
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposedFlag, 1, 0) != 0)
                return;

            // Stop first (acquires _lock internally) without holding listeners. This preserves the global
            // lock ordering _lock -> listeners used elsewhere (e.g. in AddModelTokensChangedListener) and
            // avoids any lock inversion between _lock and listeners.
            Stop();

            lock (listeners)
            {
                listeners.Clear();
            }

            GetLines().Dispose();
        }

        private void Stop()
        {
            TokenizerThread threadToStop = null;

            lock (_lock)
            {
                threadToStop = _thread;
                _thread = null;
            }

            if (threadToStop == null)
            {
                return;
            }

            threadToStop.Stop();
            threadToStop.DisposeAfterCompletion();
        }

        /// <summary>
        /// Starts the tokenizer thread if there is no current thread or the current thread has stopped, and returns the
        /// previously stopped thread instance (if any) so that it can be disposed by the caller.
        /// </summary>
        /// <remarks>
        /// This method must be called while holding the appropriate lock to ensure thread safety. If a previous tokenizer
        /// thread instance exists and is already stopped, it is returned and must be disposed by the caller to prevent
        /// resource leaks. If no previous thread existed, or if the existing thread is still running, this method returns null.
        /// </remarks>
        /// <returns>
        /// The previous instance of the tokenizer thread if it existed and was stopped; otherwise, null (for example, when there
        /// was no previous thread or the existing thread is still running).
        /// </returns>
        private TokenizerThread StartInternal()
        {
            if (IsDisposed)
                return null;

            TokenizerThread oldThread = null;

            if (this._thread == null || this._thread.IsStopped)
            {
                oldThread = this._thread; // may be non-null but stopped - caller must dispose
                this._thread = new TokenizerThread("TMModelThread", this);
            }

            if (this._thread.IsStopped)
            {
                this._thread.Run();
            }

            return oldThread;
        }

        // FIX #8: Re-check IsDisposed and _thread identity after callback returns
        // and before emitting. Stop()/Dispose()/SetGrammar can run during the callback,
        // and without the re-check, stale events can be emitted on a dead/new model state.
        private void BuildEventWithCallback(Action<ModelTokensChangedEventBuilder> callback)
        {
            if (IsDisposed)
                return;

            TokenizerThread thread;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                thread = this._thread;
                if (thread == null || thread.IsStopped)
                    return;
            }

            ModelTokensChangedEventBuilder eventBuilder = new ModelTokensChangedEventBuilder(this);

            callback(eventBuilder);

            // Re-check: Stop()/Dispose() may have run during the callback
            if (IsDisposed)
                return;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                // If Stop or SetGrammar swapped out the thread while callback ran, drop the event
                if (!object.ReferenceEquals(this._thread, thread) || thread.IsStopped)
                    return;
            }

            ModelTokensChangedEvent e = eventBuilder.Build();
            if (e != null)
            {
                this.Emit(e);
            }
        }

        // FIX #9: Snapshot the listener list under lock, then invoke callbacks
        // outside the lock. Invoking arbitrary listener code under lock(listeners)
        // creates deadlock vectors if listener code blocks on work that needs the
        // listeners lock (or any lock in the _lock -> listeners ordering chain).
        private void Emit(ModelTokensChangedEvent e)
        {
            // Avoid possible deadlocks by not invoking listeners under lock. Invoking listeners can cause
            // arbitrary reentrant code to run, including code that tries to acquire locks that would
            // deadlock with listeners lock (e.g. _lock or external locks in listener code). To avoid this,
            // snapshot the listener list under lock, then invoke callbacks outside the lock
            IModelTokensChangedListener[] listenerSnapshot;
            lock (listeners)
            {
                if (listeners.Count == 0)
                {
                    return;
                }

                listenerSnapshot = listeners.ToArray();
            }

            // Call the listeners outside the lock to avoid deadlocks. Listeners may synchronously call back into the model,
            // but they will see a consistent state because we release the lock before invoking them, and we re-check
            // IsDisposed and thread identity after the callback and before emitting.
            foreach (IModelTokensChangedListener listener in listenerSnapshot)
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
            ForceTokenization(lineIndex, lineIndex);
        }

        public void ForceTokenization(int startLineIndex, int endLineIndex)
        {
            if (IsDisposed)
                return;

            TokenizerThread tokenizerThread;
            int clampedStartLineIndex;
            int clampedEndLineIndex;

            lock (_lock)
            {
                if (IsDisposed)
                    return;

                if (_grammar == null)
                    return;

                tokenizerThread = this._thread;
                if (tokenizerThread == null || tokenizerThread.IsStopped)
                    return;

                int lineCount = this._lines.GetNumberOfLines();
                if (lineCount <= 0)
                    return;

                clampedStartLineIndex = Math.Max(0, startLineIndex);
                clampedEndLineIndex = Math.Min(endLineIndex, lineCount - 1);
                if (clampedStartLineIndex > clampedEndLineIndex)
                    return;
            }

            BuildEventWithCallback(eventBuilder =>
            {
                tokenizerThread.UpdateTokensInRange(eventBuilder, clampedStartLineIndex, clampedEndLineIndex);
            });
        }

        /// <summary>
        /// Gets the tokens for the specified line, if available.
        /// </summary>
        /// <param name="lineIndex">Zero-based line index.</param>
        /// <returns>
        /// The current token list for the line, or <c>null</c> if the model is disposed, the index is out of range,
        /// or the line has not been tokenized yet.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <b>Ownership:</b> The returned <see cref="List{TMToken}"/> is owned by the model and must be treated as read-only.
        /// Do not add, remove, or modify items in the returned list.
        /// </para>
        /// <para>
        /// <b>Stability:</b> The model may replace the underlying list during subsequent tokenization updates for the same line.
        /// Callers that need a stable view should cache the returned reference only for the duration of a single rendering pass.
        /// </para>
        /// <para>
        /// <b>Threading:</b> This method synchronizes access to the underlying line state.
        /// </para>
        /// </remarks>
        public List<TMToken> GetLineTokens(int lineIndex)
        {
            if (IsDisposed)
                return null;

            lock (_lock)
            {
                if (IsDisposed)
                    return null;

                ModelLine line = _lines.Get(lineIndex);
                if (line == null)
                    return null;

                return line.Tokens;
            }
        }

        /// <summary>
        /// Returns whether the specified line is currently marked as needing (re)tokenization.
        /// </summary>
        /// <param name="lineIndex">Zero-based line index.</param>
        /// <returns>
        /// <c>true</c> if the line is invalid; otherwise <c>false</c>. Returns <c>false</c> if the model is disposed
        /// or the index is out of range.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is a lightweight state query used by the tokenization pipeline.
        /// </para>
        /// </remarks>
        public bool IsLineInvalid(int lineIndex)
        {
            if (IsDisposed)
                return false;

            lock (_lock)
            {
                if (IsDisposed)
                    return false;

                ModelLine line = _lines.Get(lineIndex);
                if (line == null)
                    return false;

                return line.IsInvalid;
            }
        }

        public void InvalidateLine(int lineIndex)
        {
            if (IsDisposed)
                return;

            TokenizerThread thread;
            lock (_lock)
            {
                if (IsDisposed)
                    return;

                ModelLine line = this._lines.Get(lineIndex);
                if (line == null)
                    return;

                line.IsInvalid = true;
                this._invalidLines.Enqueue(lineIndex);
                thread = _thread;
            }

            thread?.Signal();
        }

        public void InvalidateLineRange(int iniLineIndex, int endLineIndex)
        {
            if (IsDisposed)
                return;

            TokenizerThread thread;
            lock (_lock)
            {
                int lineCount = this._lines.GetNumberOfLines();
                if (lineCount != 0)
                {
                    // clamp to valid range
                    int clampedStart = Math.Max(0, iniLineIndex);
                    int clampedEnd = Math.Min(endLineIndex, lineCount - 1);

                    for (int i = clampedStart; i <= clampedEnd; i++)
                    {
                        ModelLine line = this._lines.Get(i);
                        if (line != null)
                        {
                            line.IsInvalid = true;
                            this._invalidLines.Enqueue(i);
                        }
                    }
                }

                thread = _thread;
            }

            thread?.Signal();
        }

        public IModelLines GetLines()
        {
            return this._lines;
        }
    }
}
