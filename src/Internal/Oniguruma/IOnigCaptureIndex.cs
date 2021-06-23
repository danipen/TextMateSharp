namespace TextMateSharp.Internal.Oniguruma
{
    public interface IOnigCaptureIndex
    {
        int GetIndex();

        int GetStart();

        int GetEnd();

        int GetLength();
    }
}