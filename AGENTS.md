# AGENTS.md

## Cursor Cloud specific instructions

This is a .NET class library project (TextMateSharp) — no external services, databases, or Docker required.

### Prerequisites
- .NET 8.0 SDK (test/demo/benchmark projects target `net8.0`; core libraries target `netstandard2.0`)

### Key commands
| Action | Command | Notes |
|---|---|---|
| Restore | `dotnet restore` | From repo root |
| Build | `dotnet build` | Builds all 6 projects in `TextMateSharp.sln` |
| Test | `dotnet test` | Runs NUnit tests in `TextMateSharp.Tests` and `TextMateSharp.Grammars.Tests` (726 tests) |
| Demo | `cd src/TextMateSharp.Demo && dotnet run -- ./testdata/samplefiles/sample.cs` | Syntax-highlights a sample C# file |
| Benchmark | `cd src/TextMateSharp.Benchmarks && dotnet run -c Release` | BenchmarkDotNet performance tests |

### Non-obvious notes
- The CI workflow (`.github/workflows/dotnet.yml`) installs .NET 6.0.x, but `net8.0` is the actual TFM for test/demo/benchmark projects. You need .NET 8.0 SDK.
- The `Onigwrap` native library version is pinned in `build/Directory.Build.props`. If builds fail with native-interop errors, check that file.
- No linter is configured (no `.editorconfig` enforcement, no `dotnet format` CI step). Code style is not enforced via tooling.
- Sample test data files for the demo app are in `src/TextMateSharp.Demo/testdata/samplefiles/`.
