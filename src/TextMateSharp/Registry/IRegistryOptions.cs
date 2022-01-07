using System.Collections.Generic;
using System.IO;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Registry
{
    public interface IRegistryOptions
    {
        public IThemeResolver ThemeResolver { get; set; }
        public IGrammarResolver GrammarResolver { get; set; }
        ICollection<string> GetInjections(string scopeName);
        IRawTheme GetTheme();
    }

    public interface IThemeResolver
    {
        IRawTheme GetTheme(string scopeName);
    }
    public interface IGrammarResolver
    {
        IRawGrammar GetGrammar(string scopeName);

    }
    public class DefaultLocator : IRegistryOptions
    {
        public IThemeResolver ThemeResolver { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public IGrammarResolver GrammarResolver { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public string GetFilePath(string scopeName)
        {
            return null;
        }

        public ICollection<string> GetInjections(string scopeName)
        {
            return null;
        }

        public Stream GetInputStream(string scopeName)
        {
            return null;
        }

        public IRawTheme GetTheme()
        {
            return null;
        }
    }
}