namespace TextMateSharp.Internal.Rules
{
    public class IRegExpSourceAnchorCache
    {
        public string A0_G0 { get; private set; }
        public string A0_G1 { get; private set; }
        public string A1_G0 { get; private set; }
        public string A1_G1 { get; private set; }

        public IRegExpSourceAnchorCache(string a0_G0, string a0_G1, string a1_G0, string a1_G1)
        {
            A0_G0 = A0_G0;
            A0_G1 = A0_G1;
            A1_G0 = A1_G0;
            A1_G1 = A1_G1;
        }
    }
}