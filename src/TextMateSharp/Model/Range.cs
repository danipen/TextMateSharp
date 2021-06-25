namespace TextMateSharp.Model
{
    public class Range
    {
        public int fromLineNumber;
        public int toLineNumber;

        public Range(int lineNumber)
        {
            this.fromLineNumber = lineNumber;
            this.toLineNumber = lineNumber;
        }
    }
}