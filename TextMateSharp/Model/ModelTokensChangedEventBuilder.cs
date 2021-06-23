using System.Collections.Generic;

namespace TextMateSharp.Model
{
    class ModelTokensChangedEventBuilder
    {
        private ITMModel model;
        private List<Range> ranges;

        public ModelTokensChangedEventBuilder(ITMModel model)
        {
            this.model = model;
            this.ranges = new List<Range>();
        }

        public void registerChangedTokens(int lineNumber)
        {
            Range previousRange = ranges.Count == 0 ? null : ranges[ranges.Count - 1];

            if (previousRange != null && previousRange.toLineNumber == lineNumber - 1)
            {
                // extend previous range
                previousRange.toLineNumber++;
            }
            else
            {
                // insert new range
                ranges.Add(new Range(lineNumber));
            }
        }

        public ModelTokensChangedEvent Build()
        {
            if (this.ranges.Count == 0)
            {
                return null;
            }
            return new ModelTokensChangedEvent(ranges, model);
        }
    }
}