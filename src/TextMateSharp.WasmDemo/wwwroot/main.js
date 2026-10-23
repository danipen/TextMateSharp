// The page: a transparent textarea over a <pre> that .NET fills with highlighted spans on every edit.
import { dotnet } from './_framework/dotnet.js';

const code = document.getElementById('code');
const view = document.getElementById('view');
const status = document.getElementById('status');
const language = document.getElementById('language');
const theme = document.getElementById('theme');

const runtime = await dotnet.create();
await runtime.runMain();
const { Highlighter } = (await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName)).TextMateSharp.WasmDemo;

for (const line of Highlighter.Languages().split('\n')) {
  const [id, name, scope] = line.split('\t');
  language.add(new Option(name, scope));
  language.options[language.options.length - 1].dataset.id = id;
}
for (const name of Highlighter.Themes().split('\n')) {
  theme.add(new Option(name, name));
}

function render() {
  const [info, html] = Highlighter.Highlight(language.value, code.value).split('\u0001');
  view.innerHTML = html;
  status.textContent = `${info} · .NET on browser-wasm, GregsEx regex engine (no native code)`;
}

async function loadSample() {
  const id = language.selectedOptions[0].dataset.id;
  const response = await fetch(`samples/${id}.txt`);
  code.value = response.ok ? await response.text() : '';
  render();
}

function applyTheme() {
  const [background, foreground] = Highlighter.SetTheme(theme.value).split(';');
  document.documentElement.style.setProperty('--bg', background);
  document.documentElement.style.setProperty('--fg', foreground);
  render();
}

code.addEventListener('input', render);
code.addEventListener('scroll', () => { view.scrollTop = code.scrollTop; view.scrollLeft = code.scrollLeft; });
code.addEventListener('keydown', event => {
  if (event.key === 'Tab') {
    event.preventDefault();
    document.execCommand('insertText', false, '    ');
  }
});
language.addEventListener('change', loadSample);
theme.addEventListener('change', applyTheme);

language.value = 'source.cs';
theme.value = 'DarkPlus';
applyTheme();
await loadSample();
