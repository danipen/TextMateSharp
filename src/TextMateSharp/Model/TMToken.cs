namespace TextMateSharp.Model
{
    public class TMToken
    {
        public const char SCOPE_SEPARATOR = '|';

        public int StartIndex;
        public string[] scopes { get; private set; }

        private string _type;

        public TMToken(int startIndex, string type)
        {
            this.StartIndex = startIndex;

            if (!string.IsNullOrEmpty(type))
                this.scopes = type.Split(SCOPE_SEPARATOR);
        }
    }
}