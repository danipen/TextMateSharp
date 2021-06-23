using System.Collections.Generic;

namespace TextMateSharp.Internal.Types
{
    public interface IRawRule
    {
        int GetId();

        void SetId(int id);

        string GetInclude();

        void SetInclude(string include);

        string GetName();

        void SetName(string name);

        string GetContentName();

        void SetContentName(string name);

        string GetMatch();

        void SetMatch(string match);

        IRawCaptures GetCaptures();

        void SetCaptures(IRawCaptures captures);

        string GetBegin();

        void SetBegin(string begin);

        IRawCaptures GetBeginCaptures();

        void SetBeginCaptures(IRawCaptures beginCaptures);

        string GetEnd();

        void SetEnd(string end);

        string GetWhile();

        IRawCaptures GetEndCaptures();

        void SetEndCaptures(IRawCaptures endCaptures);

        IRawCaptures GetWhileCaptures();

        ICollection<IRawRule> GetPatterns();

        void SetPatterns(ICollection<IRawRule> patterns);

        IRawRepository GetRepository();

        void SetRepository(IRawRepository repository);

        bool IsApplyEndPatternLast();

        void SetApplyEndPatternLast(bool applyEndPatternLast);
    }
}