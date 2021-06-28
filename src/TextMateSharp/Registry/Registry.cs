using System;
using System.Collections.Generic;
using System.IO;

using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

using TmTheme = TextMateSharp.Themes.Theme;

namespace TextMateSharp.Registry
{
    public class Registry
    {

        private IRegistryOptions locator;
        private SyncRegistry syncRegistry;

        public Registry() : this(new DefaultLocator())
        {
        }

        public Registry(IRegistryOptions locator)
        {
            this.locator = locator;
            this.syncRegistry = new SyncRegistry(TmTheme.CreateFromRawTheme(locator.GetTheme()));
        }

        public void SetTheme(IRawTheme theme)
        {
            this.syncRegistry.SetTheme(TmTheme.CreateFromRawTheme(theme));
        }

        public IEnumerable<string> GetColorMap()
        {
            return this.syncRegistry.GetColorMap();
        }

        public IGrammar LoadGrammar(string initialScopeName)
        {

            List<string> remainingScopeNames = new List<string>();
            remainingScopeNames.Add(initialScopeName);

            List<string> seenScopeNames = new List<string>();
            seenScopeNames.Add(initialScopeName);

            while (remainingScopeNames.Count > 0)
            {
                string scopeName = remainingScopeNames[0];
                remainingScopeNames.RemoveAt(0); // shift();

                if (this.syncRegistry.Lookup(scopeName) != null)
                {
                    continue;
                }

                string filePath = this.locator.GetFilePath(scopeName);
                if (filePath == null)
                {
                    if (scopeName.Equals(initialScopeName))
                    {
                        throw new TMException("Unknown location for grammar <" + initialScopeName + ">");
                    }
                    continue;
                }

                try
                {
                    IRawGrammar grammar = null;
                    using (Stream stream = this.locator.GetInputStream(scopeName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        grammar = GrammarReader.ReadGrammarSync(reader);
                    }

                    ICollection<string> injections = this.locator.GetInjections(scopeName);

                    ICollection<string> deps = this.syncRegistry.AddGrammar(grammar, injections);
                    foreach (string dep in deps)
                    {
                        if (!seenScopeNames.Contains(dep))
                        {
                            seenScopeNames.Add(dep);
                            remainingScopeNames.Add(dep);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (scopeName.Equals(initialScopeName))
                    {
                        throw new TMException("Unknown location for grammar <" + initialScopeName + ">", e);
                    }
                }
            }
            return this.GrammarForScopeName(initialScopeName);
        }

        public IGrammar LoadGrammarFromPathSync(
            string path,
            int initialLanguage,
            Dictionary<string, int> embeddedLanguages)
        {
            IRawGrammar rawGrammar = null;
            using (StreamReader sr = new StreamReader(path))
            {
                rawGrammar = GrammarReader.ReadGrammarSync(sr);
            }
            ICollection<string> injections = this.locator.GetInjections(rawGrammar.GetScopeName());
            this.syncRegistry.AddGrammar(rawGrammar, injections);
            return this.GrammarForScopeName(rawGrammar.GetScopeName(), initialLanguage, embeddedLanguages);
        }

        public IGrammar GrammarForScopeName(string scopeName)
        {
            return GrammarForScopeName(scopeName, 0, null);
        }

        public IGrammar GrammarForScopeName(string scopeName, int initialLanguage, Dictionary<string, int> embeddedLanguages)
        {
            return this.syncRegistry.GrammarForScopeName(scopeName, initialLanguage, embeddedLanguages);
        }

        public Theme GetTheme()
        {
            return this.syncRegistry.GetTheme();
        }

        public IRegistryOptions GetLocator()
        {
            return locator;
        }
    }
}
