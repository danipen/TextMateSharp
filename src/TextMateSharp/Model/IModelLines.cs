using System;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    /// <summary>
    /// Represents a document model that provides line-based access for TextMate tokenization.
    /// </summary>
    public interface IModelLines
    {
        /// <summary>
        /// Notifies that a new line has been added at the specified index.
        /// </summary>
        /// <param name="lineIndex">The zero-based index where the line was added.</param>
        void AddLine(int lineIndex);

        /// <summary>
        /// Notifies that a line has been removed at the specified index.
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the removed line.</param>
        void RemoveLine(int lineIndex);

        /// <summary>
        /// Notifies that the content of a line has changed.
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the updated line.</param>
        void UpdateLine(int lineIndex);

        /// <summary>
        /// Gets the number of model lines currently tracked.
        /// </summary>
        int GetSize();

        /// <summary>
        /// Gets the model line at the specified index.
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the line.</param>
        ModelLine Get(int lineIndex);

        /// <summary>
        /// Executes an action for each model line.
        /// </summary>
        /// <param name="action">The action to execute on each line.</param>
        void ForEach(Action<ModelLine> action);

        /// <summary>
        /// Gets the total number of lines in the document.
        /// </summary>
        int GetNumberOfLines();

        /// <summary>
        /// Gets the text content of the line at the specified index.
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the line.</param>
        /// <returns>The line text wrapped in a <see cref="LineText"/> structure.</returns>
        /// <remarks>
        /// For optimal performance, the returned text should include the line terminator
        /// (e.g., '\n' or "\r\n") if one exists. When line terminators are not included,
        /// the tokenization engine will allocate a new buffer to append a newline character,
        /// which impacts performance and memory usage.
        /// </remarks>
        LineText GetLineTextIncludingTerminators(int lineIndex);

        /// <summary>
        /// Gets the length of the line at the specified index.
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the line.</param>
        int GetLineLength(int lineIndex);

        /// <summary>
        /// Releases resources used by this model.
        /// </summary>
        void Dispose();
    }
}