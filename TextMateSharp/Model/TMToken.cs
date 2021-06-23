namespace TextMateSharp.Model
{
    public class TMToken
    {
        public int StartIndex;
        public string type;

        public TMToken(int startIndex, string type)
        {
            this.StartIndex = startIndex;
            this.type = type;
        }
    }
}
