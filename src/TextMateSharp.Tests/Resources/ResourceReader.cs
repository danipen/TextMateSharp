﻿using System.IO;
using System.Reflection;

namespace TextMateSharp.Tests.Resources
{
    class ResourceReader
    {
        const string Prefix = "TextMateSharp.Tests.Resources.";

        public static Stream OpenStream(string name)
        {
            var result = typeof(ResourceReader).GetTypeInfo().Assembly.GetManifestResourceStream(Prefix + name);

            if (result == null)
                throw new FileNotFoundException("The resource file '" + name + "' was not found.");

            return result;
        }
    }
}
