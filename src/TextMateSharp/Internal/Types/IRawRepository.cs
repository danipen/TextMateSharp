namespace TextMateSharp.Internal.Types
{
    public interface IRawRepository
    {
        // IRawRule GetRule(string name);
        IRawRepository Merge(params IRawRepository[] sources);

        IRawRule GetProp(string name);

        IRawRule GetBase();

        IRawRule GetSelf();

        void SetSelf(IRawRule raw);

        void SetBase(IRawRule ruleBase);

    }
}