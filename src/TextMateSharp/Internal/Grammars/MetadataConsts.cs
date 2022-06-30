namespace TextMateSharp.Internal.Grammars
{

    /**
	 * Helpers to manage the "collapsed" metadata of an entire StackElement stack.
	 * The following assumptions have been made:
	 *  - languageId < 256 => needs 8 bits
	 *  - unique color count < 512 => needs 9 bits
	 *
	 * The binary format is:
	 * - -------------------------------------------
	 *     3322 2222 2222 1111 1111 1100 0000 0000
	 *     1098 7654 3210 9876 5432 1098 7654 3210
	 * - -------------------------------------------
	 *     xxxx xxxx xxxx xxxx xxxx xxxx xxxx xxxx
	 *     bbbb bbbb bfff ffff ffFF FTTT LLLL LLLL
	 * - -------------------------------------------
	 *  - L = LanguageId (8 bits)
	 *  - T = StandardTokenType (2 bits)
	 *  - F = FontStyle (4 bits)
	 *  - f = foreground color (9 bits)
	 *  - b = background color (9 bits)
	 */
    public class MetadataConsts
    {
        public static uint LANGUAGEID_MASK = 0b00000000000000000000000011111111;
        public static uint TOKEN_TYPE_MASK = 0b00000000000000000000001100000000;
        public static uint FONT_STYLE_MASK = 0b00000000000000000011110000000000;
        public static uint FOREGROUND_MASK = 0b00000000011111111100000000000000;
        public static uint BACKGROUND_MASK = 0b11111111100000000000000000000000;

        public const int LANGUAGEID_OFFSET = 0;
        public const int TOKEN_TYPE_OFFSET = 8;
        public const int FONT_STYLE_OFFSET = 10;
        public const int FOREGROUND_OFFSET = 14;
        public const int BACKGROUND_OFFSET = 23;
    }
}