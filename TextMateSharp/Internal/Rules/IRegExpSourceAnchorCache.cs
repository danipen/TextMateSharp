namespace TextMateSharp.Internal.Rules
{
    public class IRegExpSourceAnchorCache
    {

        public string A0_G0;
        public string A0_G1;
        public string A1_G0;
        public string A1_G1;

        public IRegExpSourceAnchorCache(string A0_G0, string A0_G1, string A1_G0, string A1_G1)
        {
            this.A0_G0 = A0_G0;
            this.A0_G1 = A0_G1;
            this.A1_G0 = A1_G0;
            this.A1_G1 = A1_G1;
        }
    }
}