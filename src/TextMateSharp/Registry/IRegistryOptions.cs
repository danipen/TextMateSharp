using System.Collections.Generic;
using System.IO;

using TextMateSharp.Themes;

namespace TextMateSharp.Registry
{
    public interface IRegistryOptions
    {
        string GetFilePath(string scopeName);

        StreamReader GetInputStream(string scopeName);

        ICollection<string> GetInjections(string scopeName);

        IRawTheme GetTheme();
    }

    public class DefaultLocator : IRegistryOptions
    {
        public string GetFilePath(string scopeName)
        {
            return null;
        }

        public ICollection<string> GetInjections(string scopeName)
        {
            return null;
        }

        public StreamReader GetInputStream(string scopeName)
        {
            return null;
        }

        public IRawTheme GetTheme()
        {
            return null;
        }
    }
}