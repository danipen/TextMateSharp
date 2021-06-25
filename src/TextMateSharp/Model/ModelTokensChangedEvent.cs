using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class ModelTokensChangedEvent
    {
        public List<Range> ranges;
        public ITMModel model;

        public ModelTokensChangedEvent(Range range, ITMModel model) :
            this(new List<Range>() { range }, model)
        {
        }

        public ModelTokensChangedEvent(List<Range> ranges, ITMModel model)
        {
            this.ranges = ranges;
            this.model = model;
        }
    }
}