using System;
using System.Collections.Generic;
using System.Text;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public static class StackElementMetadata
    {
        public static string ToBinaryStr(int metadata)
        {
            List<char> builder = new List<char>(Convert.ToString((uint)metadata, 2));

            while (builder.Count < 32)
                builder.Insert(0, '0');

            return new string(builder.ToArray());
        }

        public static int GetLanguageId(int metadata)
        {
            uint uintValue = (uint)metadata;
            return (int)((uintValue & MetadataConsts.LANGUAGEID_MASK) >> MetadataConsts.LANGUAGEID_OFFSET);
        }

        public static int GetTokenType(int metadata)
        {
            uint uintValue = (uint)metadata;
            return (int)((uintValue & MetadataConsts.TOKEN_TYPE_MASK) >> MetadataConsts.TOKEN_TYPE_OFFSET);
        }

        static bool ContainsBalancedBrackets(int metadata)
        {
            uint uintValue = (uint)metadata;
            return (uintValue & MetadataConsts.BALANCED_BRACKETS_MASK) != 0;
        }

        public static int GetFontStyle(int metadata)
        {
            uint uintValue = (uint)metadata;
            return (int)((uintValue & MetadataConsts.FONT_STYLE_MASK) >> MetadataConsts.FONT_STYLE_OFFSET);
        }

        public static int GetForeground(int metadata)
        {
            uint uintValue = (uint)metadata;
            return (int)((uintValue & MetadataConsts.FOREGROUND_MASK) >> MetadataConsts.FOREGROUND_OFFSET);
        }

        public static int GetBackground(int metadata)
        {
            ulong unitValue = (ulong)metadata;
            return (int)((unitValue & MetadataConsts.BACKGROUND_MASK) >> MetadataConsts.BACKGROUND_OFFSET);
        }

        public static int Set(
            int metadata,
            int languageId,
            int tokenType,
            bool? containsBalancedBrackets,
            int fontStyle,
            int foreground,
            int background)
        {
            languageId = languageId == 0 ? StackElementMetadata.GetLanguageId(metadata) : languageId;
            tokenType = tokenType == StandardTokenType.Other ? StackElementMetadata.GetTokenType(metadata) : tokenType;
            var containsBalancedBracketsBit = (containsBalancedBrackets == null ? ContainsBalancedBrackets(metadata)
                : containsBalancedBrackets.Value) ? 1 : 0;
            fontStyle = fontStyle == FontStyle.NotSet ? StackElementMetadata.GetFontStyle(metadata) : fontStyle;
            foreground = foreground == 0 ? StackElementMetadata.GetForeground(metadata) : foreground;
            background = background == 0 ? StackElementMetadata.GetBackground(metadata) : background;

            return ((languageId << MetadataConsts.LANGUAGEID_OFFSET) 
                    | (tokenType << MetadataConsts.TOKEN_TYPE_OFFSET)
                    | (containsBalancedBracketsBit << MetadataConsts.BALANCED_BRACKETS_OFFSET)
                    | (fontStyle << MetadataConsts.FONT_STYLE_OFFSET) 
                    | (foreground << MetadataConsts.FOREGROUND_OFFSET)
                    | (background << MetadataConsts.BACKGROUND_OFFSET)) >> 0;
        }
    }
}