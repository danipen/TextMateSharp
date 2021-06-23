using System;

namespace TextMateSharp.Model
{
    public interface IModelLines
    {
        void AddLine(int lineIndex);
        void RemoveLine(int lineIndex);
        void UpdateLine(int lineIndex);
        int GetSize();
        ModelLine Get(int lineIndex);
        void ForEach(Action<ModelLine> action);
        int GetNumberOfLines();
        string GetLineText(int lineIndex);
        int GetLineLength(int lineIndex);
        void Dispose();
    }
}