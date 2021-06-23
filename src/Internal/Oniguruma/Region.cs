using System.Text;

namespace TextMateSharp.Internal.Oniguruma
{
    public class Region
    {
        public int numRegs;
        public int[] beg;
        public int[] end;

        public Region(int num)
        {
            this.numRegs = num;
            this.beg = new int[num];
            this.end = new int[num];
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Region: \n");
            for (int i = 0; i < beg.Length; i++) sb.Append(" " + i + ": (" + beg[i] + "-" + end[i] + ")");
            return sb.ToString();
        }
    }
}