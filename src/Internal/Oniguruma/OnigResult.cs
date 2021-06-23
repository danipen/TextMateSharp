namespace TextMateSharp.Internal.Oniguruma
{
    public class OnigResult
    {
        private int indexInScanner;
        private Region region;

        public OnigResult(Region region, int indexInScanner)
        {
            this.region = region;
            this.indexInScanner = indexInScanner;
        }

        public int GetIndex()
        {
            return indexInScanner;
        }

        public void SetIndex(int index)
        {
            this.indexInScanner = index;
        }

        public int LocationAt(int index)
        {
            int bytes = region.beg[index];
            if (bytes > 0)
            {
                return bytes;
            }
            else
            {
                return 0;
            }
        }

        public int Count()
        {
            return region.numRegs;
        }

        public int LengthAt(int index)
        {
            int bytes = region.end[index] - region.beg[index];
            if (bytes > 0)
            {
                return bytes;
            }
            else
            {
                return 0;
            }
        }
    }
}