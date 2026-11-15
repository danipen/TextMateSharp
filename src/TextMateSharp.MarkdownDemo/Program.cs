using Markdig;
using TextMateSharp.Grammars;
using TextMateSharp.Markdown;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddRazorPages();
services.AddSingleton(new MarkdownPipelineBuilder()
	.UseAdvancedExtensions()
	.UseEmojiAndSmiley()
	.UseTextMateSharp(ThemeName.DarkPlus)
	.Build()
);

var app = builder.Build();

app.UseRouting();
app.MapStaticAssets();
app.MapRazorPages()
	.WithStaticAssets();

app.Run();