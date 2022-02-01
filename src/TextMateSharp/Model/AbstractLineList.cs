using System;
using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public abstract class AbstractLineList : IModelLines
    {
        //private static final Logger LOGGER = Logger.getLogger(AbstractLineList.class.getName());

        private IList<ModelLine> list = new List<ModelLine>();

        private TMModel model;

        public AbstractLineList()
        {
        }

        public void SetModel(TMModel model)
        {
            this.model = model;
        }

        public void AddLine(int line)
        {
            lock (mLock)
            {
                this.list.Insert(line, new ModelLine());
            }
        }

        public void RemoveLine(int line)
        {
            lock (mLock)
            {
                this.list.RemoveAt(line);
            }
        }

        public ModelLine Get(int index)
        {
            lock (mLock)
            {
                if (index < 0 || index >= this.list.Count)
                    return null;

                return this.list[index];
            }
        }

        public void ForEach(Action<ModelLine> action)
        {
            lock (mLock)
            {
                foreach (ModelLine modelLine in list)
                    action(modelLine);
            }
        }

        protected void InvalidateLine(int lineIndex)
        {
            if (model != null)
            {
                model.InvalidateLine(lineIndex);
            }
        }

        protected void InvalidateLineRange(int iniLineIndex, int endLineIndex)
        {
            if (model != null)
            {
                model.InvalidateLineRange(iniLineIndex, endLineIndex);
            }
        }

        protected void ForceTokenization(int startLineIndex, int endLineIndex)
        {
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

        public abstract string GetLineText(int lineIndex); 

        public abstract int GetLineLength(int lineIndex); 

        public abstract void Dispose(); 

        object mLock = new object();
    }
}