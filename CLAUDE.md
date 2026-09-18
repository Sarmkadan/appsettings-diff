# CLAUDE.md

.NET 10 CLI tool (`AppsettingsDiff`) that diffs `appsettings*.json` across environments, reports missing/changed/type-changed keys and redacts secrets.

## Build
- `dotnet build` - single project `appsettings-diff.csproj` (no .sln), output assembly `AppsettingsDiff`
- `dotnet run -- diff <base.json> <target.json> --format json`
- `dotnet run -- dir --dir <path> --envs Production,Staging`
- `./verify_typechange.sh` - smoke run of `diff` on type-change cases

## Tests
- `dotnet test` - xUnit; tests live in the same project (`src/*Tests.cs`), no separate test project
- `dotnet test --filter "FullyQualifiedName~ConfigDifferTests"` - single class

## Lint / format
- No analyzers or formatter config; `Nullable` and `ImplicitUsings` enabled, `LangVersion=latest`, XML docs generated (`GenerateDocumentationFile=true`) - keep public members documented.

## Layout
- `src/Program.cs` - entry point; System.CommandLine root with `diff` and `dir` subcommands, options: `--format`, `--show-secrets`, `--mask-sensitive`, `--ignore`, `--sensitive-patterns`, `--fail-on`, `--schema`, `--max-depth`, `--path`, `--no-color`, `--verbose`
- `src/` - flat, one type per file (~50 files): readers (`JsonConfigReader`, `DotEnvReader`, `EnvVarOverlay`), diff core (`ConfigDiffer*`, `ConfigPath`, `KeyPath*`, `MergeResult*`), secrets (`SensitiveKeyDetector`, `RedactionPolicy`, `PlaceholderDetector`, `TimingSafeComparer`), report writers (`*DiffReportWriter`, `DiffReportWriterRegistry`, `DiffReportWriterFactory`), schema (`SchemaValidator*`)
- `docs/` - per-class markdown notes
- `build/` - published output (committed); `bin/`, `obj/` ignored

## Conventions
- Namespace `AppsettingsDiff`; file name == type name
- Report writers implement `IDiffReportWriter` and register in `DiffReportWriterRegistry` (drives `--format` list)
- Extension methods go in `<Type>Extensions.cs` / `<Type>JsonExtensions.cs` / `<Type>FluentExtensions.cs`
- Tests named `<Type>Tests.cs`, xUnit `[Fact]`/`[Theory]`
- Sensitive values never printed by default; new key patterns go through `SensitiveKeyDetector`
- Commit style: `type: message` (chore/docs/feat/fix)
