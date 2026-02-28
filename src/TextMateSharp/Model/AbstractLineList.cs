using System;
using System.Collections.Generic;
using System.Threading;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    /// <summary>
    /// Base implementation of <see cref="IModelLines"/> providing a thread-safe internal <see cref="ModelLine"/> list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Threading:</b> This class serializes access to its internal line list via <see cref="mLock"/>.
    /// </para>
    /// <para>
    /// <b>Important:</b> Do not call back into <see cref="TMModel"/> (e.g. via <see cref="InvalidateLine(int)"/>,
    /// <see cref="InvalidateLineRange(int,int)"/>, or <see cref="ForceTokenization(int,int)"/>) while holding <see cref="mLock"/>.
    /// <see cref="TMModel"/> may hold its own internal lock while calling <see cref="IModelLines.Get(int)"/>
    /// or <see cref="IModelLines.ForEach(Action{ModelLine})"/>, which can acquire <see cref="mLock"/>;
    /// reversing that order can deadlock.
    /// </para>
    /// </remarks>
    public abstract class AbstractLineList : IModelLines
    {
        // readonly only protects the reference to the list, not the contents; we still need mLock to synchronize access to the list contents
        private readonly IList<ModelLine> _list = new List<ModelLine>();

        // Published by SetModel(), consumed by Invalidate* / ForceTokenization()
        // Use Volatile.Read/Write for safe cross-thread publication without involving mLock.
        // Lock ordering: TMModel has its own lock, and AbstractLineList uses mLock for its internal list.
        // The correct lock acquisition order is: TMModel lock (if any) -> mLock.
        // Never acquire TMModel's lock while holding mLock to avoid deadlocks.
        private TMModel _model;

        // Guard to ensure SetModel is only called once per instance. We use Interlocked.CompareExchange for atomicity and fail-fast behavior
        private int _setModelCalledFlag; // 0 = not called, 1 = called

        /// <summary>
        /// Sets the model for the current instance. This method can only be called once per instance to ensure
        /// consistency.
        /// </summary>
        /// <remarks>This method atomically assigns the model and invalidates all existing lines in the
        /// list. It is intended to be invoked only once per instance; subsequent calls will result in an
        /// exception.</remarks>
        /// <param name="model">The model to associate with this instance. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="model"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see langword="SetModel"/>
        /// is called more than once on the same instance.</exception>
        public void SetModel(TMModel model)
        {
            // Initialization contract:
            // 1. SetModel must be called exactly once; we fail-fast on repeats via CompareExchange
            // 2. SetModel is expected to be called before any Invalidate* / ForceTokenization() calls:
            //      - Calls made before SetModel is ever invoked (flag == 0, _model == null) are treated as no-ops.
            //      - Calls made while SetModel is in progress (flag == 1, _model == null) will throw,
            //        so callers must ensure correct initialization order and not use this instance until SetModel has completed
            // 3. Publication ordering: we publish _model only after invalidating existing lines; callers must still not use this instance until SetModel completes
            // 4. Lock ordering is preserved: no mLock around _model access, and no TMModel callbacks while holding mLock
            if (model == null) throw new ArgumentNullException(nameof(model));

            // Fail-fast: only allow one successful caller
            if (Interlocked.CompareExchange(ref this._setModelCalledFlag, 1, 0) != 0)
                throw new InvalidOperationException($"{nameof(SetModel)} can only be called once per {nameof(AbstractLineList)} instance");

            lock (mLock)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    ModelLine line = _list[i];
                    line.IsInvalid = true;
                }
            }

            // Publish only after invalidation completes
            Volatile.Write(ref this._model, model);
        }

        public void AddLine(int line)
        {
            lock (mLock)
            {
                this._list.Insert(line, new ModelLine());
            }
        }

        public void RemoveLine(int line)
        {
            lock (mLock)
            {
                this._list.RemoveAt(line);
            }
        }

        public ModelLine Get(int index)
        {
            lock (mLock)
            {
                if (index < 0 || index >= this._list.Count)
                    return null;

                return this._list[index];
            }
        }

        public void ForEach(Action<ModelLine> action)
        {
            lock (mLock)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    ModelLine modelLine = _list[i];
                    action(modelLine);
                }
            }
        }

        /// <summary>
        /// Notifies the owning <see cref="TMModel"/> that a single line must be (re)tokenized.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Threading / lock ordering:</b> This method calls back into <see cref="TMModel"/>, which may acquire TMModel's
        /// internal lock.
        /// </para>
        /// <para>
        /// Do <b>not</b> call this method while holding <see cref="mLock"/> (or from within <see cref="AddLine(int)"/>,
        /// <see cref="RemoveLine(int)"/>, <see cref="Get(int)"/>, or <see cref="ForEach(Action{ModelLine})"/> callbacks that
        /// execute under <see cref="mLock"/>), otherwise you can create a lock-order inversion deadlock
        /// (<c>mLock -> TMModel lock</c>) with the tokenizer thread (<c>TMModel lock -> mLock</c>).
        /// </para>
        /// <para>
        /// Safe pattern for derived classes:
        /// <list type="number">
        /// <item><description><c>lock(mLock)</c>: mutate the internal list and compute affected ranges.</description></item>
        /// <item><description>Release <see cref="mLock"/>.</description></item>
        /// <item><description>Call <see cref="InvalidateLine(int)"/> / <see cref="InvalidateLineRange(int,int)"/> / <see cref="ForceTokenization(int,int)"/>.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="lineIndex">Zero-based line index to invalidate.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="SetModel(TMModel)"/> has started but has not yet completed publishing the model.
        /// </exception>
        protected void InvalidateLine(int lineIndex)
        {
            TMModel model = GetModelIfAvailable();
            if (model != null)
            {
                model.InvalidateLine(lineIndex);
            }
        }

        /// <summary>
        /// Notifies the owning <see cref="TMModel"/> that a range of lines must be (re)tokenized.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="InvalidateLine(int)"/>
        /// </remarks>
        /// <param name="iniLineIndex">Zero-based start line index (inclusive).</param>
        /// <param name="endLineIndex">Zero-based end line index (inclusive).</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="SetModel(TMModel)"/> has started but has not yet completed publishing the model.
        /// </exception>
        protected void InvalidateLineRange(int iniLineIndex, int endLineIndex)
        {
            TMModel model = GetModelIfAvailable();
            if (model != null)
            {
                model.InvalidateLineRange(iniLineIndex, endLineIndex);
            }
        }

        /// <summary>
        /// Forces synchronous tokenization over a range of lines by delegating to the owning <see cref="TMModel"/>.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="InvalidateLine(int)"/>
        /// </remarks>
        /// <param name="startLineIndex">Zero-based start line index (inclusive).</param>
        /// <param name="endLineIndex">Zero-based end line index (inclusive).</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="SetModel(TMModel)"/> has started but has not yet completed publishing the model.
        /// </exception>
        protected void ForceTokenization(int startLineIndex, int endLineIndex)
        {
            TMModel model = GetModelIfAvailable();
            if (model != null)
            {
                model.ForceTokenization(startLineIndex, endLineIndex);
            }
        }

        public int GetSize()
        {
            return GetNumberOfLines();
        }

        public abstract void UpdateLine(int lineIndex);

        public abstract int GetNumberOfLines();

        public abstract LineText GetLineTextIncludingTerminators(int lineIndex);

        public abstract int GetLineLength(int lineIndex);

        public abstract void Dispose();

        /// <summary>
        /// Lock protecting the internal <see cref="_list"/> structure.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Threading / lock-ordering contract:</b> This class protects its internal line list with <see cref="mLock"/>.
        /// <see cref="TMModel"/> has its own internal lock.
        /// </para>
        /// <para>
        /// The tokenizer thread in <see cref="TMModel"/> may acquire TMModel's lock and then call into
        /// <see cref="IModelLines.Get(int)"/>, which acquires <see cref="mLock"/> (lock order: <c>TMModel lock -> mLock</c>).
        /// </para>
        /// <para>
        /// To avoid deadlocks, derived implementations must never re-enter <see cref="TMModel"/> (via
        /// <see cref="InvalidateLine(int)"/>, <see cref="InvalidateLineRange(int,int)"/>, or <see cref="ForceTokenization(int,int)"/>)
        /// while holding <see cref="mLock"/> (which would create <c>mLock -> TMModel lock</c>).
        /// </para>
        /// </remarks>
        readonly object mLock = new object();

        /// <summary>
        /// Gets the current model instance if it is available.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns the current model instance if it has been published via <see cref="SetModel(TMModel)"/>.
        /// </para>
        /// <para>
        /// If <see cref="_setModelCalledFlag"/> indicates that <see cref="SetModel(TMModel)"/> has started but the
        /// model has not yet been published to <see cref="_model"/>, an <see cref="InvalidOperationException"/> is thrown
        /// because callbacks into this API during binding are considered a misuse.
        /// </para>
        /// <para>
        /// If <see cref="_model"/> has not yet been set and <see cref="_setModelCalledFlag"/> is still zero, this method
        /// returns <c>null</c>.
        /// </para>
        /// </remarks>
        /// <returns>
        /// The current instance of the model if it is available; otherwise, <c>null</c> if the model has not yet been set.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="SetModel(TMModel)"/> has started but has not yet completed publishing the model.
        /// </exception>
        private TMModel GetModelIfAvailable()
        {
            TMModel model = Volatile.Read(ref this._model);
            if (model != null)
            {
                return model;
            }

            // if SetModel has never been called, treat as a no-op
            if (Volatile.Read(ref this._setModelCalledFlag) == 0)
            {
                return null;
            }

            // SetModel was called but has not yet completed publishing the model (as observed by this thread).
            throw new InvalidOperationException($"{nameof(SetModel)} must complete before calling Invalidate*/ForceTokenization.");
        }
    }
}
