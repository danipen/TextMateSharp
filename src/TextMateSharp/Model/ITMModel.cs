using System;
using System.Collections.Generic;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    /// <summary>
    /// Represents a TextMate model that manages tokenization of a document.
    /// The model coordinates between the document content and the grammar to produce tokens.
    /// Implementations of this interface also implement <see cref="IDisposable"/>
    /// and must properly dispose of any resources they hold.
    /// </summary>
    public interface ITMModel : IDisposable
    {
        /// <summary>
        /// Gets the grammar currently used for tokenization.
        /// </summary>
        /// <returns>The current grammar, or null if no grammar is set.</returns>
        IGrammar GetGrammar();

        /// <summary>
        /// Sets the grammar to use for tokenization.
        /// Changing the grammar will invalidate existing tokens and trigger re-tokenization.
        /// </summary>
        /// <param name="grammar">The grammar to use for tokenization.</param>
        void SetGrammar(IGrammar grammar);

        /// <summary>
        /// Registers a listener to be notified when tokens change in the model.
        /// </summary>
        /// <param name="listener">The listener to receive token change notifications.</param>
        void AddModelTokensChangedListener(IModelTokensChangedListener listener);

        /// <summary>
        /// Removes a previously registered token change listener.
        /// </summary>
        /// <param name="listener">The listener to remove.</param>
        void RemoveModelTokensChangedListener(IModelTokensChangedListener listener);

        /// <summary>
        /// Gets the tokens for a specific line.
        /// </summary>
        /// <param name="line">The zero-based line index.</param>
        /// <returns>A list of tokens for the specified line, or null if the line has not been tokenized yet.</returns>
        List<TMToken> GetLineTokens(int line);

        /// <summary>
        /// Forces immediate tokenization of a specific line, bypassing the background tokenization queue.
        /// Use this when you need tokens for a line immediately (e.g., for visible lines in the viewport).
        /// </summary>
        /// <param name="lineIndex">The zero-based index of the line to tokenize.</param>
        void ForceTokenization(int lineIndex);
    }
}