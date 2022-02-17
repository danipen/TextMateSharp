using System.Collections.Generic;

namespace TextMateSharp.Model
{
    class ModelTokensChangedEventBuilder
    {
        private ITMModel _model;
        private List<Range> _ranges;

        public ModelTokensChangedEventBuilder(ITMModel model)
        {
            this._model = model;
            this._ranges = new List<Range>();
        }

        public void registerChangedTokens(int lineNumber)
        {
            Range previousRange = _ranges.Count == 0 ? null : _ranges[_ranges.Count - 1];

            if (previousRange != null && previousRange.ToLineNumber == lineNumber - 1)
            {
                // extend previous range
                previousRange.ToLineNumber++;
            }
            else
            {
                // insert new range
                _ranges.Add(new Range(lineNumber));
            }
        }

        public ModelTokensChangedEvent Build()
        {
            if (this._ranges.Count == 0)
            {
                return null;
            }
            return new ModelTokensChangedEvent(_ranges, _model);
        }
    }
}