using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class ModelTokensChangedEvent
    {
        public List<Range> Ranges { get; private set; }
        public ITMModel Model { get; private set; }

        public ModelTokensChangedEvent(Range range, ITMModel model) :
            this(new List<Range>() { range }, model)
        {
        }

        public ModelTokensChangedEvent(List<Range> ranges, ITMModel model)
        {
            Ranges = ranges;
            Model = model;
        }
    }
}