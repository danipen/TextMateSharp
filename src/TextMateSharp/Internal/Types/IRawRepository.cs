namespace TextMateSharp.Internal.Types
{
    public interface IRawRepository
    {
        // IRawRule GetRule(string name);

        IRawRule GetProp(string name);

        IRawRule GetBase();

        IRawRule GetSelf();

        void SetSelf(IRawRule raw);

        void SetBase(IRawRule ruleBase);

    }
}