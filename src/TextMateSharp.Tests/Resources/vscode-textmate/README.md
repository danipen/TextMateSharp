# vscode-textmate tokenization suites

`first-mate/` and `suite1/` are `test-cases/` of microsoft/vscode-textmate at commit
392b23a4a5ecda7fe4f481d721f113695b14159b (MIT, see LICENSE.md), run by `VsCodeTextMateSuiteTests` the way
vscode-textmate's `src/tests/tokenization.test.ts` runs them. TextMateSharp reads JSON grammars only, so the XML
plist grammars (`*.plist`, `*.tmLanguage`) were converted with Python's `plistlib` to `<name>.json` next to their
original name; the test maps a grammar path to that file.
