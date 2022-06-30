using System.Collections.Generic;

namespace TextMateSharp.Internal.Types
{
    public interface IRawRule
    {
        int? GetId();

        void SetId(int id);

        string GetInclude();

        void SetInclude(string include);

        string GetName();

        void SetName(string name);

        string GetContentName();

        string GetMatch();

        IRawCaptures GetCaptures();

        string GetBegin();

        IRawCaptures GetBeginCaptures();

        void SetBeginCaptures(IRawCaptures beginCaptures);

        string GetEnd();

        string GetWhile();

        IRawCaptures GetEndCaptures();

        IRawCaptures GetWhileCaptures();

        ICollection<IRawRule> GetPatterns();

        void SetPatterns(ICollection<IRawRule> patterns);

        IRawRepository GetRepository();

        void SetRepository(IRawRepository repository);

        bool IsApplyEndPatternLast();
    }
}