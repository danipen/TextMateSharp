using Onigwrap;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public sealed class RegExpSourceList
    {
        private sealed class RegExpSourceListAnchorCache
        {

            internal CompiledRule A0_G0;
            internal CompiledRule A0_G1;
            internal CompiledRule A1_G0;
            internal CompiledRule A1_G1;

        }

        private readonly List<RegExpSource> _items = new List<RegExpSource>();
        private bool _hasAnchors;
        private CompiledRule _cached;
        private readonly RegExpSourceListAnchorCache _anchorCache = new RegExpSourceListAnchorCache();

        internal void Push(RegExpSource item)
        {
            this._items.Add(item);
            this._hasAnchors = this._hasAnchors ? this._hasAnchors : item.HasAnchor();
        }

        internal void UnShift(RegExpSource item)
        {
            this._items.Insert(0, item);
            this._hasAnchors = this._hasAnchors ? this._hasAnchors : item.HasAnchor();
        }

        internal int Length()
        {
            return this._items.Count;
        }

        internal void SetSource(int index, string newSource)
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

        internal CompiledRule Compile(bool allowA, bool allowG)
        {
            if (!this._hasAnchors)
            {
                if (this._cached == null)
                {
                    List<string> regexps = new List<string>(_items.Count);
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
            List<string> regexps = new List<string>(_items.Count);
            foreach (RegExpSource regExpSource in _items)
            {
                regexps.Add(regExpSource.ResolveAnchors(allowA, allowG));
            }
            return new CompiledRule(CreateOnigScanner(regexps.ToArray()), GetRules());
        }

        private static OnigScanner CreateOnigScanner(string[] regexps)
        {
            return new OnigScanner(regexps);
        }

        private IList<RuleId> GetRules()
        {
            List<RuleId> ruleIds = new List<RuleId>(_items.Count);
            foreach (RegExpSource item in this._items)
            {
                ruleIds.Add(item.GetRuleId());
            }
            return ruleIds.ToArray();
        }

    }
}