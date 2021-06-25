namespace TextMateSharp.Internal.Types
{
    public interface IRawCaptures : IBaseRaw
    {
        IRawRule GetCapture(string captureId);
    }
}
