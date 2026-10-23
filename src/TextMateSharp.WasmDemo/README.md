# TextMateSharp WebAssembly demo

A browser editor that highlights code with TextMateSharp running on .NET WebAssembly (`browser-wasm`): every grammar
and theme bundled in TextMateSharp.Grammars, selectable from the drop-downs, re-highlighted as you type. Hover a token
to see its scopes.

```bash
dotnet run --project src/TextMateSharp.WasmDemo
```

`dotnet run` serves the app with the .NET SDK's WebAssembly dev server and prints its URL; no Node.js or wasm workload
is needed. `dotnet publish` writes a static site to `bin/Release/net10.0/publish/wwwroot`.

How it works: `Program.cs` exports `Highlighter` methods to JavaScript (`[JSExport]`); `wwwroot/main.js` lays a
transparent `<textarea>` over a `<pre>` and fills the `<pre>` with the spans `Highlighter.Highlight` returns, colored
by the theme. `wwwroot/samples` holds a starting snippet per language id; other languages start empty.

This needs a regex engine without native code: TextMateSharp's Onigwrap binding has no `browser-wasm` library
(`System.DllNotFoundException: libonigwrap`), while GregsEx's Oniguruma flavor is managed.
