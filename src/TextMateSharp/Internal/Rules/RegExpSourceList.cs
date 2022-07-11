using System.Collections.Generic;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Rules
{
    public class RegExpSourceList
    {
        private class RegExpSourceListAnchorCache
        {

            public CompiledRule A0_G0;
            public CompiledRule A0_G1;
            public CompiledRule A1_G0;
            public CompiledRule A1_G1;

        }

        private List<RegExpSource> _items;
        private bool _hasAnchors;
        private CompiledRule _cached;
        private RegExpSourceListAnchorCache _anchorCache;

        public RegExpSourceList()
        {
            this._items = new List<RegExpSource>();
            this._hasAnchors = false;
            this._cached = null;
            this._anchorCache = new RegExpSourceListAnchorCache();
        }

        public void Push(RegExpSource item)
        {
            this._items.Add(item);
            this._hasAnchors = this._hasAnchors ? this._hasAnchors : item.HasAnchor();
        }

        public void UnShift(RegExpSource item)
        {
            this._items.Insert(0, item);
            this._hasAnchors = this._hasAnchors ? this._hasAnchors : item.HasAnchor();
        }

        public int Length()
        {
            return this._items.Count;
        }

        public void SetSource(int index, string newSource)
        {
            RegExpSource r = this._items[index];
            if (!r.GetSource().Equals(newSource))
            {
                // bust the cache
                this._cached = null;
                this._anchorCache.A0_G0 = null;
                this._anchorCache.A0_G1 = null;
                this._anchorCache.A1_G0 = null;
                this._anchorCache.A1_G1 = null;

                r.SetSource(newSource);
            }
        }

        public CompiledRule Compile(bool allowA, bool allowG)
        {
            if (!this._hasAnchors)
            {
                if (this._cached == null)
                {
                    List<string> regexps = new List<string>();
                    foreach (RegExpSource regExpSource in _items)
                    {
                        regexps.Add(regExpSource.GetSource());
                    }
                    this._cached = new CompiledRule(CreateOnigScanner(regexps.ToArray()), GetRules());
                }
                return this._cached;
            }

            if (this._anchorCache.A0_G0 == null)
            {
                this._anchorCache.A0_G0 = (allowA == false && allowG == false) ? this.ResolveAnchors(allowA, allowG)
                        : null;
            }
            if (this._anchorCache.A0_G1 == null)
            {
                this._anchorCache.A0_G1 = (allowA == false && allowG == true) ? this.ResolveAnchors(allowA, allowG)
                        : null;
            }
            if (this._anchorCache.A1_G0 == null)
            {
                this._anchorCache.A1_G0 = (allowA == true && allowG == false) ? this.ResolveAnchors(allowA, allowG)
                        : null;
            }
            if (this._anchorCache.A1_G1 == null)
            {
                this._anchorCache.A1_G1 = (allowA == true && allowG == true) ? this.ResolveAnchors(allowA, allowG)
                        : null;
            }
            if (allowA)
            {
                if (allowG)
                    return this._anchorCache.A1_G1;

                return this._anchorCache.A1_G0;
            }

            if (allowG)
                return this._anchorCache.A0_G1;

            return this._anchorCache.A0_G0;
        }

        private CompiledRule ResolveAnchors(bool allowA, bool allowG)
        {
            List<string> regexps = new List<string>();
            foreach (RegExpSource regExpSource in _items)
            {
                regexps.Add(regExpSource.ResolveAnchors(allowA, allowG));
            }
            return new CompiledRule(CreateOnigScanner(regexps.ToArray()), GetRules());
        }

        private OnigScanner CreateOnigScanner(string[] regexps)
        {
            return new OnigScanner(regexps);
        }

        private IList<RuleId> GetRules()
        {
            List<RuleId> ruleIds = new List<RuleId>();
            foreach (RegExpSource item in this._items)
            {
                ruleIds.Add(item.GetRuleId());
            }
            return ruleIds.ToArray();
        }

    }
}