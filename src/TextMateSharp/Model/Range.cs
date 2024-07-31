namespace TextMateSharp.Model
{
    public class Range
    {
        public int FromLineNumber { get; private set; }
        public int ToLineNumber { get; set; }

        public Range(int lineNumber)
        {
            FromLineNumber = lineNumber;
            ToLineNumber = lineNumber;
        }

        public Range(int fromLineNumber, int toLineNumber)
        {
            FromLineNumber = fromLineNumber;
            ToLineNumber = toLineNumber;
        }
    }
}