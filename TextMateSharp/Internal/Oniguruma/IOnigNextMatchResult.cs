namespace TextMateSharp.Internal.Oniguruma
{
    public interface IOnigNextMatchResult
    {
        int GetIndex();
        IOnigCaptureIndex[] GetCaptureIndices();
    }
}