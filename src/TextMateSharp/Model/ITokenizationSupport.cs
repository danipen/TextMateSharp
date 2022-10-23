using System;

namespace TextMateSharp.Model
{
    public interface ITokenizationSupport
    {
        TMState GetInitialState();

        LineTokens Tokenize(string line, TMState state, TimeSpan timeLimit);

        LineTokens Tokenize(string line, TMState state, int offsetDelta, int maxLen, TimeSpan timeLimit);

    }
}