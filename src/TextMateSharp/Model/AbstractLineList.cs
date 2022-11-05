using System;
using System.Collections.Generic;

namespace TextMateSharp.Model
{
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
            lock(mLock)
            {
                foreach (var line in _list)
                {
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
                foreach (ModelLine modelLine in _list)
                    action(modelLine);
            }
        }

        protected void InvalidateLine(int lineIndex)
        {
            if (_model != null)
            {
                _model.InvalidateLine(lineIndex);
            }
        }

        protected void InvalidateLineRange(int iniLineIndex, int endLineIndex)
        {
            if (_model != null)
            {
                _model.InvalidateLineRange(iniLineIndex, endLineIndex);
            }
        }

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

        public abstract string GetLineText(int lineIndex); 

        public abstract int GetLineLength(int lineIndex); 

        public abstract void Dispose(); 

        object mLock = new object();
    }
}