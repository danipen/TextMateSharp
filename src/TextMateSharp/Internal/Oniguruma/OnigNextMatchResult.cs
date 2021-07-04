using System.Text;

namespace TextMateSharp.Internal.Oniguruma
{
    class OnigNextMatchResult : IOnigNextMatchResult
    {
        private int index;

        private IOnigCaptureIndex[] captureIndices;

        public OnigNextMatchResult(OnigResult result)
        {
            this.index = result.GetIndex();
            this.captureIndices = CaptureIndicesForMatch(result);
        }

        public int GetIndex()
        {
            return index;
        }

        public IOnigCaptureIndex[] GetCaptureIndices()
        {
            return captureIndices;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append("{\n");
            result.Append("  \"index\": ");
            result.Append(GetIndex());
            result.Append(",\n");
            result.Append("  \"captureIndices\": [\n");
            int i = 0;
            foreach (IOnigCaptureIndex captureIndex in GetCaptureIndices())
            {
                if (i > 0)
                {
                    result.Append(",\n");
                }
                result.Append("    ");
                result.Append(captureIndex);
                i++;
            }
            result.Append("\n");
            result.Append("  ]\n");
            result.Append("}");
            return result.ToString();
        }

        private static IOnigCaptureIndex[] CaptureIndicesForMatch(OnigResult result)
        {
            int resultCount = result.Count();
            IOnigCaptureIndex[] captures = new IOnigCaptureIndex[resultCount];
            for (int index = 0; index < resultCount; index++)
            {
                int captureStart = result.LocationAt(index);
                int captureEnd = result.LocationAt(index) + result.LengthAt(index);

                captures[index] = new OnigCaptureIndex(index, captureStart, captureEnd);
            }

            return captures;
        }

        class OnigCaptureIndex : IOnigCaptureIndex
        {

            private int index;
            private int start;
            private int end;

            public OnigCaptureIndex(int index, int start, int end)
            {
                this.index = index;
                this.start = start >= 0 ? start : 0;
                this.end = end >= 0 ? end : 0;
            }

            public int GetIndex()
            {
                return index;
            }

            public int GetStart()
            {
                return start;
            }

            public int GetEnd()
            {
                return end;
            }

            public int GetLength()
            {
                return end - start;
            }

            public string toString()
            {
                StringBuilder result = new StringBuilder();
                result.Append("{\"index\": ");
                result.Append(GetIndex());
                result.Append(", \"start\": ");
                result.Append(GetStart());
                result.Append(", \"end\": ");
                result.Append(GetEnd());
                result.Append(", \"length\": ");
                result.Append(GetLength());
                result.Append("}");
                return result.ToString();
            }
        }
    }
}
