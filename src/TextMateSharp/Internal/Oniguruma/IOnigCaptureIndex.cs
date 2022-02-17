namespace TextMateSharp.Internal.Oniguruma
{
    public interface IOnigCaptureIndex
    {
        int Index { get; }

        int Start { get; }

        int End { get; }

        int Length { get; }
    }
}