using TextMateSharp.Grammars;

namespace TextMateSharp.Themes;

/// <summary>
/// Represents the final resolved styles for a token.
/// </summary>
public readonly record struct ResolvedStyles(
	int ForegroundColor,
	int BackgroundColor,
	FontStyle FontStyle
)
{
	public const int NoColor = -1;
	
	public static ResolvedStyles Create(IToken token, Theme theme)
	{
		var foreground = NoColor;
		var background = NoColor;
		var fontStyle = FontStyle.NotSet;

		foreach (var themeRule in theme.Match(token.Scopes))
		{
			if (foreground == -1 && themeRule.foreground > 0)
			{
				foreground = themeRule.foreground;
			}
			if (background == -1 && themeRule.background > 0)
			{
				background = themeRule.background;
			}
			if (fontStyle == FontStyle.NotSet && themeRule.fontStyle > 0)
			{
				fontStyle = themeRule.fontStyle;
			}
		}
		
		return new ResolvedStyles(foreground, background, fontStyle);
	}

	/// <summary>
	/// Gets whether the resolved styles are blank.
	/// </summary>
	public bool IsEmpty => 
		BackgroundColor == NoColor && 
	    ForegroundColor == NoColor &&
	    FontStyle is FontStyle.NotSet;
}