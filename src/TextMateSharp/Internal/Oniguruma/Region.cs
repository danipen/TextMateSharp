using System.Text;

namespace TextMateSharp.Internal.Oniguruma
{
    public class Region
    {
        public int NumRegs { get; private set; }
        public int[] Start { get; private set; }
        public int[] End { get; private set; }

        public Region(in int num)
        {
            NumRegs = num;
            Start = new int[num];
            End = new int[num];
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Region: \n");
            for (int i = 0; i < Start.Length; i++) sb.Append(" " + i + ": (" + Start[i] + "-" + End[i] + ")");
            return sb.ToString();
        }
    }
}