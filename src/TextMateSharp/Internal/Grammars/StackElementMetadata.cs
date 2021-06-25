using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public static class StackElementMetadata
    {

        public static string ToBinaryStr(int metadata)
        {
            /*
			 * let r = metadata.toString(2); while (r.length < 32) { r = '0' + r; }
			 * return r;
			 */
            // TODO!!!
            return null;
        }

        public static int GetLanguageId(int metadata)
        {
            uint unitValue = (uint)metadata;
            return (int)(unitValue & MetadataConsts.LANGUAGEID_MASK) >> MetadataConsts.LANGUAGEID_OFFSET;
        }

        public static int GetTokenType(int metadata)
        {
            uint unitValue = (uint)metadata;
            return (int)(unitValue & MetadataConsts.TOKEN_TYPE_MASK) >> MetadataConsts.TOKEN_TYPE_OFFSET;
        }

        public static int GetFontStyle(int metadata)
        {
            uint unitValue = (uint)metadata;
            return (int)(unitValue & MetadataConsts.FONT_STYLE_MASK) >> MetadataConsts.FONT_STYLE_OFFSET;
        }

        public static int GetForeground(int metadata)
        {

            uint unitValue = (uint)metadata;
            return (int)(unitValue & MetadataConsts.FOREGROUND_MASK) >> MetadataConsts.FOREGROUND_OFFSET;
        }

        public static int GetBackground(int metadata)
        {
            uint unitValue = (uint)metadata;
            return (int)(unitValue & MetadataConsts.BACKGROUND_MASK) >> MetadataConsts.BACKGROUND_OFFSET;
        }

        public static int Set(int metadata, int languageId, int tokenType, int fontStyle, int foreground, int background)
        {
            languageId = languageId == 0 ? StackElementMetadata.GetLanguageId(metadata) : languageId;
            tokenType = tokenType == StandardTokenType.Other ? StackElementMetadata.GetTokenType(metadata) : tokenType;
            fontStyle = fontStyle == FontStyle.NotSet ? StackElementMetadata.GetFontStyle(metadata) : fontStyle;
            foreground = foreground == 0 ? StackElementMetadata.GetForeground(metadata) : foreground;
            background = background == 0 ? StackElementMetadata.GetBackground(metadata) : background;

            return ((languageId << MetadataConsts.LANGUAGEID_OFFSET) | (tokenType << MetadataConsts.TOKEN_TYPE_OFFSET)
                    | (fontStyle << MetadataConsts.FONT_STYLE_OFFSET) | (foreground << MetadataConsts.FOREGROUND_OFFSET)
                    | (background << MetadataConsts.BACKGROUND_OFFSET)) >> 0;
        }
    }
}