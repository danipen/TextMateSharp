using System;

namespace TextMateSharp.Internal.Rules
{
    public class RuleId
    {
        public static RuleId NO_RULE = new RuleId(0);

        /**
         * This is a special constant to indicate that the end regexp matched.
         */
        public static RuleId END_RULE = new RuleId(-1);

        /**
         * This is a special constant to indicate that the while regexp matched.
         */
        public static RuleId WHILE_RULE = new RuleId(-2);

        public static RuleId Of(int id)
        {
            if (id < 0)
                throw new ArgumentException("[id] must be > 0");
            return new RuleId(id);
        }

        public int Id;

        private RuleId(int id)
        {
            this.Id = id;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (typeof(RuleId) != obj.GetType())
                return false;
            var other = (RuleId)obj;
            return Id == other.Id;
        }

        public bool NotEquals(RuleId otherRule)
        {
            return Id != otherRule.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
