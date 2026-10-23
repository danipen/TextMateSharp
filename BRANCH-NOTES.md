# Local branch `gregsex` (never pushed)

TextMateSharp (github.com/danipen/TextMateSharp at e0db45b) with GregsEx's Oniguruma flavor in place of Onigwrap, and
allocation and caching changes in the tokenizer. Targets net10.0 (GregsEx is .NET 10, NativeAOT compatible).
GregsEx comes from `../gregsex/.claude/worktrees/friendly-zhukovsky-03b73e` (`GregsExProject`), or from a package with
`-p:GregsExVersion=<version>`.

Every change keeps the tokens of all 2,800 docs-heavy fences byte-identical to upstream with Onigwrap
(`TextMateSharp.Benchmarks --dump`, SHA-256 `42477AF3...695474`) and the tests green: 818 TextMateSharp tests (6
vscode-textmate cases skipped as known TextMateSharp differences, failing identically with Onigwrap) and 16
TextMateSharp.Grammars tests.

## Commits

| Commit | Change |
| --- | --- |
| 0be2252 | `Internal/Oniguruma/OnigScanner.cs`: Onigwrap's `OnigScanner` semantics over `GregsEx.OnigurumaScanner` (earliest match, first pattern on a tie, per-regex result cache for string-backed text, non-participating groups as (0, 0), Onigwrap's `\xHH` rewrite, invalid regexes throwing when reached). |
| c452308 | vscode-textmate's tokenization suites (first-mate, suite1, whileTests) as `VsCodeTextMateSuiteTests`; docs-heavy benchmark. |
| a82c3b2 | Oniguruma-sized compile limits (objective-c's grammar has a 282k-unit pattern). |
| 426127d | T1: the newline TextMateSharp appends goes into a string, not a `char[]`: the per-regex cache only trusts string-backed text, so lines passed without `\n` (Douze) never used it. |
| 479d237 | T2: capture indices are one struct array per match instead of an array and an object per group. |
| c26b84f | T3: `AttributedScopeStack` caches its children (validated against the grammar's cached scope attributes, which a theme change replaces), and `Grammar` its root, so scope nodes and their scope-name lists are reused across lines and documents. |
| 1cf7406, 2dd04ae | T4: capture arrays are rented from pools owned per grammar and tokenizer nesting depth (capture retokenization nests), for any group count, while conditions included. |
| da44d46 | T5: one `LineTokenizer` per grammar and depth, reused for every line with its buffers; struct `LocalStackElement`, `TokenizeStringResult` and `WhileCheckResult`; a timestamp instead of a `Stopwatch` per line. |

GregsEx side, found by these suites: `OnigurumaScanner` does not reuse cached results of patterns using `\G` (a result
found from an earlier offset is not the one from a later offset when `\G` ties it to the search start). Onigwrap's
cache has that flaw; TextMateSharp never hit it with Douze's array-backed lines, and T1 would have exposed it
(vscode-textmate suite1 #8, Makefile).

## Measurements

`DocsHeavyTokenizationBenchmark`: the first 40 docs-heavy fences of each language, tokenized line by line without the
newline as Douze does, tm-grammars 1.32.22. BenchmarkDotNet 0.15.8 ShortRun with MemoryDiagnoser, Apple M4 Pro,
macOS 15.8.1, .NET 10.0.11, 2026-10-23. Upstream is e0db45b with only the benchmark project on net10.0.

```sh
TEXTMATE_DIR=<textmate.zip extracted> DOCS_FENCES=<fences.json> dotnet run -c Release --project src/TextMateSharp.Benchmarks -- --filter "*DocsHeavy*"
```

Mean time per operation (40 fences):

| Language | Upstream (Onigwrap) | GregsEx (0be2252) | T1 | T2 | T3 | T4 | T5 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| bash | 14.72 ms | 2.88 ms | 2.69 ms | 2.64 ms | 2.49 ms | 2.50 ms | 2.43 ms |
| ts | 48.06 ms | 14.06 ms | 10.66 ms | 10.36 ms | 10.03 ms | 10.19 ms | 10.14 ms |
| js | 91.92 ms | 21.10 ms | 16.37 ms | 15.57 ms | 15.22 ms | 15.30 ms | 15.86 ms |
| html | 9.21 ms | 2.60 ms | 2.17 ms | 2.06 ms | 1.90 ms | 1.93 ms | 1.88 ms |
| json | 1.99 ms | 0.69 ms | 0.71 ms | 0.67 ms | 0.53 ms | 0.53 ms | 0.51 ms |
| yaml | 15.56 ms | 2.48 ms | 2.06 ms | 2.05 ms | 1.97 ms | 1.98 ms | 1.98 ms |
| csharp | 89.03 ms | 50.85 ms | 27.28 ms | 26.89 ms | 27.13 ms | 26.93 ms | 26.64 ms |

Allocated per operation:

| Language | Upstream (Onigwrap) | GregsEx (0be2252) | T1 | T2 | T3 | T4 | T5 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| bash | 3,564 KB | 2,304 KB | 2,304 KB | 2,140 KB | 1,532 KB | 1,149 KB | 915 KB |
| ts | 8,305 KB | 4,915 KB | 4,915 KB | 3,389 KB | 2,621 KB | 1,288 KB | 978 KB |
| js | 14,510 KB | 7,660 KB | 7,660 KB | 4,905 KB | 3,864 KB | 1,576 KB | 1,259 KB |
| html | 4,813 KB | 3,512 KB | 3,512 KB | 3,000 KB | 2,386 KB | 1,733 KB | 1,506 KB |
| json | 2,222 KB | 1,628 KB | 1,618 KB | 1,556 KB | 1,011 KB | 832 KB | 570 KB |
| yaml | 3,164 KB | 1,894 KB | 1,894 KB | 1,700 KB | 1,330 KB | 981 KB | 735 KB |
| csharp | 8,008 KB | 4,454 KB | 4,454 KB | 3,942 KB | 3,136 KB | 2,340 KB | 1,704 KB |

T4 is shown with the pools for any group count (2dd04ae); its first version (1cf7406) left js and ts at 3,073 KB and
1,903 KB because their patterns exceed 64 groups. T1 to T5 change time by 0 to 7% each except T1 (csharp -46%, ts
-24%, js -22%, yaml -17%, html -17%): the remaining time is regex search, mostly C#'s grammar.

Douze (docs-heavy, 4,000 pages, NativeAOT, macOS, 3 interleaved runs): 3.31 to 4.04 s wall and 30.4 s CPU with
TextMateSharp 2.0.4, 1.22 to 1.74 s wall and 10.9 s CPU with this branch (da44d46 packaged as 2.0.5-gregsex.1);
`_site` byte-identical (4,002 files).
