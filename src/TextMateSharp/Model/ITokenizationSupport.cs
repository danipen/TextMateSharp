using System;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public interface ITokenizationSupport
    {
        TMState GetInitialState();
        LineTokens Tokenize(LineText line, TMState state, TimeSpan timeLimit);
        LineTokens Tokenize(LineText line, TMState state, int offsetDelta, int maxLen, TimeSpan timeLimit);
    }
}