namespace TextMateSharp.Model
{
    public interface ITokenizationSupport
    {

        TMState GetInitialState();

        LineTokens Tokenize(string line, TMState state);

        LineTokens Tokenize(string line, TMState state, int offsetDelta, int stopAtOffset);

    }
}