using System;
using System.Collections.Generic;

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
        private IList<ModelLine> _list = new List<ModelLine>();

        private TMModel _model;

        public AbstractLineList()
        {
        }

        public void SetModel(TMModel model)
        {
            this._model = model;
            lock (mLock)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    ModelLine line = _list[i];
                    line.IsInvalid = true;
                }
            }
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
        protected void InvalidateLine(int lineIndex)
        {
            if (_model != null)
            {
                _model.InvalidateLine(lineIndex);
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
        protected void InvalidateLineRange(int iniLineIndex, int endLineIndex)
        {
            if (_model != null)
            {
                _model.InvalidateLineRange(iniLineIndex, endLineIndex);
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
        protected void ForceTokenization(int startLineIndex, int endLineIndex)
        {
            if (_model != null)
            {
                _model.ForceTokenization(startLineIndex, endLineIndex);
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
    }
}