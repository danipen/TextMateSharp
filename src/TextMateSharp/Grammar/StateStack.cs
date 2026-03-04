using System;
using System.Text;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Grammars
{
    public interface IStateStack
    {
        int Depth { get; }
        RuleId RuleId { get; }
        string EndRule { get; }
    }

    public class StateStack : IStateStack, IEquatable<StateStack>
    {
        public static StateStack NULL = new StateStack(
            null,
            RuleId.NO_RULE,
            0,
            0,
            false,
            null,
            null,
            null);

        public StateStack Parent { get; private set; }
        public int Depth { get; private set; }
        public RuleId RuleId { get; private set; }
        public string EndRule { get; private set; }
        public AttributedScopeStack NameScopesList { get; private set; }
        public AttributedScopeStack ContentNameScopesList { get; private set; }
        public bool BeginRuleCapturedEOL { get; private set; }

        private int _enterPos;
        private int _anchorPos;

        // Precomputed hash code — uses parent's cached hash to avoid O(n) recursion.
        // Safe as long as hash-participating fields (Depth, RuleId, EndRule, Parent,
        // ContentNameScopesList) are not mutated after construction
        private readonly int _hashCode;

        public StateStack(
            StateStack parent,
            RuleId ruleId,
            int enterPos,
            int anchorPos,
            bool beginRuleCapturedEOL,
            string endRule,
            AttributedScopeStack nameScopesList,
            AttributedScopeStack contentNameScopesList)
        {
            Parent = parent;
            Depth = (this.Parent != null ? this.Parent.Depth + 1 : 1);
            RuleId = ruleId;
            BeginRuleCapturedEOL = beginRuleCapturedEOL;
            EndRule = endRule;
            NameScopesList = nameScopesList;
            ContentNameScopesList = contentNameScopesList;

            _enterPos = enterPos;
            _anchorPos = anchorPos;

            _hashCode = ComputeHashCode(parent, ruleId, endRule, Depth, contentNameScopesList);
        }

        /// <summary>
        /// A structural equals check. Does not take into account <c>ContentNameScopesList</c>.
        /// The consideration for <c>ContentNameScopesList</c> is handled separately in the <c>AttributedScopeStack.Equals</c> method.
        /// Iterative to avoid StackOverflowException on deep stacks.
        /// </summary>
        private static bool StructuralEquals(StateStack a, StateStack b)
        {
            while (true)
            {
                // Use ReferenceEquals to avoid infinite recursion through operator ==
                if (ReferenceEquals(a, b))
                {
                    return true;
                }
                if (a is null || b is null)
                {
                    // End of list reached only for one
                    return false;
                }
                // Use object.Equals for null-safe value equality on RuleId and EndRule,
                // matching Java upstream's Objects.equals() semantics
                if (a.Depth != b.Depth || !Equals(a.RuleId, b.RuleId) || !Equals(a.EndRule, b.EndRule))
                {
                    return false;
                }

                // Go to previous pair
                a = a.Parent;
                b = b.Parent;
            }
        }

        /// <summary>
        /// Determines whether two StateStack instances are equal by comparing their structure and associated scope
        /// lists.
        /// </summary>
        /// <remarks>This method first checks for reference equality and null values before comparing
        /// precomputed hash codes for efficiency. If necessary, it performs a structural comparison of the StateStack
        /// instances and their associated scope lists. This method is intended for internal use to support equality
        /// operations.</remarks>
        /// <param name="a">The first StateStack instance to compare, or null.</param>
        /// <param name="b">The second StateStack instance to compare, or null.</param>
        /// <returns>true if both StateStack instances are equal; otherwise, false.</returns>
        private static bool Equals(StateStack a, StateStack b)
        {
            // Use ReferenceEquals to avoid infinite recursion through operator ==
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (a is null || b is null)
            {
                return false;
            }

            // Precomputed hash codes let us reject non-equal pairs in O(1)
            // before walking the O(n) parent chain in StructuralEquals
            if (a._hashCode != b._hashCode)
            {
                return false;
            }

            return StructuralEquals(a, b) &&
                   // Null-safe comparison via the internal static method on AttributedScopeStack
                   AttributedScopeStack.Equals(a.ContentNameScopesList, b.ContentNameScopesList);
        }

        /// <summary>
        /// Determines whether the specified StateStack instance is equal to the current instance.
        /// </summary>
        /// <param name="other">The StateStack instance to compare with the current instance.</param>
        /// <returns>true if the specified StateStack instance is equal to the current instance; otherwise, false.</returns>
        public bool Equals(StateStack other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current StateStack instance.
        /// </summary>
        /// <remarks>This method overrides Object.Equals to provide value equality specific to StateStack
        /// instances. Use this method to compare StateStack objects for logical equivalence rather than reference
        /// equality.</remarks>
        /// <param name="other">The object to compare with the current StateStack. Must be of type StateStack to be considered for equality.</param>
        /// <returns>true if the specified object is a StateStack and is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object other)
        {
            if (other is StateStack stackElement)
            {
                return Equals(this, stackElement);
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <remarks>The hash code is used in hash-based collections such as hash tables. Equal objects
        /// must return the same hash code for correct behavior in these collections.</remarks>
        /// <returns>An integer that represents the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Determines whether two StateStack instances are equal.
        /// </summary>
        /// <remarks>This operator uses the Equals method to determine equality. When implementing
        /// equality operators, it is recommended to also override the Equals(object) and GetHashCode() methods to
        /// ensure consistent behavior.</remarks>
        /// <param name="left">The first StateStack instance to compare.</param>
        /// <param name="right">The second StateStack instance to compare.</param>
        /// <returns>true if the specified StateStack instances are equal; otherwise, false.</returns>
        public static bool operator ==(StateStack left, StateStack right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two instances of the StateStack class are not equal.
        /// </summary>
        /// <remarks>This operator uses the Equals method to compare the specified instances.</remarks>
        /// <param name="left">The first StateStack instance to compare.</param>
        /// <param name="right">The second StateStack instance to compare.</param>
        /// <returns>true if the two StateStack instances are not equal; otherwise, false.</returns>
        public static bool operator !=(StateStack left, StateStack right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Computes a hash code using multiply-accumulate (factor 31) for good
        /// distribution. References <c>parent._hashCode</c> instead of calling
        /// <c>parent.GetHashCode()</c> to keep this O(1) per node rather than O(n).
        /// Builds incrementally on parent's hash.
        /// </summary>
        private static int ComputeHashCode(
            StateStack parent,
            RuleId ruleId,
            string endRule,
            int depth,
            AttributedScopeStack contentNameScopesList)
        {
            const int primeFactor = 31; // Common prime factor for multiply-accumulate hash code
            unchecked
            {
                int hash = (parent?._hashCode) ?? 0;
                hash = (hash * primeFactor) + (contentNameScopesList?.GetHashCode() ?? 0);
                hash = (hash * primeFactor) + (endRule?.GetHashCode() ?? 0);
                hash = (hash * primeFactor) + (ruleId?.GetHashCode() ?? 0);
                return (hash * primeFactor) + depth;
            }
        }

        public void Reset()
        {
            StateStack el = this;
            while (el != null)
            {
                el._enterPos = -1;
                el._anchorPos = -1;
                el = el.Parent;
            }
        }

        public StateStack Pop()
        {
            return this.Parent;
        }

        public StateStack SafePop()
        {
            if (this.Parent != null)
            {
                return this.Parent;
            }
            return this;
        }

        public StateStack Push(
            RuleId ruleId,
            int enterPos,
            int anchorPos,
            bool beginRuleCapturedEOL,
            string endRule,
            AttributedScopeStack nameScopesList,
            AttributedScopeStack contentNameScopesList)
        {
            return new StateStack(
                this,
                ruleId,
                enterPos,
                anchorPos,
                beginRuleCapturedEOL,
                endRule,
                nameScopesList,
                contentNameScopesList);
        }

        public int GetEnterPos()
        {
            return this._enterPos;
        }

        public int GetAnchorPos()
        {
            return this._anchorPos;
        }

        public Rule GetRule(IRuleRegistry grammar)
        {
            return grammar.GetRule(this.RuleId);
        }

        public override string ToString()
        {
            int depth = this.Depth;
            RuleId[] ruleIds = new RuleId[depth];
            StateStack current = this;

            for (int i = depth - 1; i >= 0; i--)
            {
                ruleIds[i] = current.RuleId;
                current = current.Parent;
            }

            const int estimatedCharsPerRuleId = 8;
            StringBuilder builder = new StringBuilder(16 + (depth * estimatedCharsPerRuleId));
            builder.Append('[');

            for (int i = 0; i < depth; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append('(');
                builder.Append(ruleIds[i]); //, TODO-${this.nameScopesList}, TODO-${this.contentNameScopesList})`;
                builder.Append(')');
            }

            builder.Append(']');
            return builder.ToString();
        }

        public StateStack WithContentNameScopesList(AttributedScopeStack contentNameScopesList)
        {
            // Null-safe comparison matching Java upstream's Objects.equals() pattern
            if (AttributedScopeStack.Equals(this.ContentNameScopesList, contentNameScopesList))
            {
                return this;
            }
            return this.Parent.Push(
                this.RuleId,
                this._enterPos,
                this._anchorPos,
                this.BeginRuleCapturedEOL,
                this.EndRule,
                this.NameScopesList,
                contentNameScopesList);
        }

        public StateStack WithEndRule(string endRule)
        {
            if (this.EndRule != null && this.EndRule.Equals(endRule))
            {
                return this;
            }
            return new StateStack(
                this.Parent,
                this.RuleId,
                this._enterPos,
                this._anchorPos,
                this.BeginRuleCapturedEOL,
                endRule,
                this.NameScopesList,
                this.ContentNameScopesList);
        }

        /// <summary>
        /// Determines whether the current state stack shares the same rule as the specified state stack.
        /// </summary>
        /// <remarks>The comparison traverses the parent state stacks of both instances and checks for a
        /// matching rule identifier and entry position. This method can be used to determine if two state stacks are
        /// associated with the same parsing rule in a grammar.</remarks>
        /// <param name="other">The state stack to compare with the current instance. This parameter cannot be null.</param>
        /// <returns>true if the current state stack and the specified state stack share the same rule; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the other parameter is null.</exception>
        public bool HasSameRuleAs(StateStack other)
        {
            if (other is null) throw new ArgumentNullException(nameof(other));

            StateStack el = this;
            while (el is not null && el._enterPos == other._enterPos)
            {
                if (el.RuleId == other.RuleId)
                {
                    return true;
                }
                el = el.Parent;
            }
            return false;
        }
    }
}