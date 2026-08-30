using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AppsettingsDiff;

/// <summary>
/// Categories for determining whether to fail the process.
/// </summary>
public enum FailOn
{
    /// <summary> Do not fail on any issue. </summary>
    None,
    /// <summary> Fail on missing keys. </summary>
    Missing,
    /// <summary> Fail on any difference. </summary>
    Any,
    /// <summary> Fail on schema violations. </summary>
    SchemaViolation
}

/// <summary>
/// Command-line entry point for the appsettings diff tool.
/// </summary>
public static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitDifferencesFound = 1;
    private const int ExitError = 2;
    private const string StdinSentinel = "-";
    private const string DirOptionName = "--dir";
    private const string EnvsOptionName = "--envs";
    private const string FailOnOptionName = "--fail-on";
    private const string DefaultFormat = "console";
    private static readonly string[] SupportedExtensions = [".json", ".yaml", ".yml", ".env"];

    /// <summary>
    /// Runs the CLI.
    ///
    /// Exit codes:
    /// <see cref="ExitSuccess"/> - Success: No differences or violations found
    /// <see cref="ExitDifferencesFound"/> - Failure: Differences or violations found according to <see cref="FailOnOptionName"/>
    /// <see cref="ExitError"/> - Error: Bad arguments, missing files, or other errors
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }
        catch (IOException)
        {
            // Ignore if output is redirected to a file or pipe where setting encoding is not allowed.
        }

        var baseArgument = new Argument<FileInfo>("base", $"The base JSON/YAML file (use {StdinSentinel} to read from stdin)").ExistingOnly();
        var targetArgument = new Argument<FileInfo>("target", $"The target JSON/YAML file (use {StdinSentinel} to read from stdin)").ExistingOnly();

        var dirOption = new Option<DirectoryInfo>(DirOptionName, "The directory containing configuration files").ExistingOnly();
        var envsOption = new Option<string[]>(EnvsOptionName, "The environments to compare (comma-separated, e.g. Production,Staging)") { AllowMultipleArgumentsPerToken = true };

        var formatOption = new Option<string?>("--format", "Output format (" + string.Join(", ", DiffReportWriterRegistry.GetRegisteredFormats()) + ")");
        var showSecretsOption = new Option<bool>("--show-secrets", "Show sensitive keys");
        var maskSensitiveOption = new Option<bool>("--mask-sensitive", "Mask sensitive values with *** instead of showing [REDACTED]");
        var ignoreOption = new Option<string[]>("--ignore", "Glob patterns of keys to ignore") { AllowMultipleArgumentsPerToken = true };
        var sensitivePatternsOption = new Option<FileInfo?>("--sensitive-patterns", "File containing additional sensitive key patterns (one per line, # comments allowed)");
        var failOnOption = new Option<FailOn>(FailOnOptionName, "Which categories cause a non-zero exit code (missing|any|schema-violation)");
        var schemaOption = new Option<FileInfo?>("--schema", "JSON schema file for validation").ExistingOnly();
        var maxDepthOption = new Option<int?>("--max-depth", "Maximum depth to compare nested structures (0 = no limit)");
        var pathOption = new Option<string?>("--path", "Only compare keys under the given key-path prefix (e.g. Logging:LogLevel)");
        var noColorOption = new Option<bool>("--no-color", "Disable ANSI color output");
        var verboseOption = new Option<bool>("--verbose", "Write diagnostic information to standard error");

        var rootCommand = new RootCommand("Appsettings Diff Tool")
        {
            Description = "Compare configuration files (JSON/YAML) and detect differences"
        };

        // Mode 1: <base> <target>
        var diffCommand = new Command("diff", "Compare two configuration files")
        {
            Description = "Compare base and target configuration files"
        };
        diffCommand.AddArgument(baseArgument);
        diffCommand.AddArgument(targetArgument);
        diffCommand.AddOption(formatOption);
        diffCommand.AddOption(showSecretsOption);
        diffCommand.AddOption(maskSensitiveOption);
        diffCommand.AddOption(ignoreOption);
        diffCommand.AddOption(sensitivePatternsOption);
        diffCommand.AddOption(failOnOption);
        diffCommand.AddOption(schemaOption);
        diffCommand.AddOption(maxDepthOption);
        diffCommand.AddOption(pathOption);
        diffCommand.AddOption(noColorOption);
        diffCommand.AddOption(verboseOption);

        // Mode 2: --dir --envs
        var dirCommand = new Command("dir", "Compare configuration files in a directory")
        {
            Description = "Compare configuration files across multiple environments in a directory"
        };
        dirCommand.AddOption(dirOption);
        dirCommand.AddOption(envsOption);
        dirCommand.AddOption(formatOption);
        dirCommand.AddOption(showSecretsOption);
        dirCommand.AddOption(maskSensitiveOption);
        dirCommand.AddOption(ignoreOption);
        dirCommand.AddOption(sensitivePatternsOption);
        dirCommand.AddOption(failOnOption);
        dirCommand.AddOption(schemaOption);
        dirCommand.AddOption(maxDepthOption);
        dirCommand.AddOption(pathOption);
        dirCommand.AddOption(noColorOption);
        dirCommand.AddOption(verboseOption);

        rootCommand.AddCommand(diffCommand);
        rootCommand.AddCommand(dirCommand);

        // The bare invocation `appsettings-diff <base> <target>` behaves like `diff`.
        rootCommand.AddArgument(baseArgument);
        rootCommand.AddArgument(targetArgument);
        rootCommand.AddOption(formatOption);
        rootCommand.AddOption(showSecretsOption);
        rootCommand.AddOption(maskSensitiveOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(sensitivePatternsOption);
        rootCommand.AddOption(failOnOption);
        rootCommand.AddOption(schemaOption);
        rootCommand.AddOption(maxDepthOption);
        rootCommand.AddOption(pathOption);
        rootCommand.AddOption(noColorOption);
        rootCommand.AddOption(verboseOption);

        rootCommand.SetHandler((InvocationContext context) =>
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0] == "--help" || args[0] == "-h")))
            {
                ShowHelp(rootCommand);
                context.ExitCode = ExitSuccess;
                return;
            }
            HandleDiff(context);
        });

        void HandleDiff(InvocationContext context)
        {
            var baseFile = context.ParseResult.GetValueForArgument(baseArgument);
            var targetFile = context.ParseResult.GetValueForArgument(targetArgument);
            var options = ReadOutputOptions(context);

            context.ExitCode = Execute(context, () => RunFileDiff(baseFile, targetFile, options));
        }

        OutputOptions ReadOutputOptions(InvocationContext context) => new(
            Format: context.ParseResult.GetValueForOption(formatOption),
            ShowSecrets: context.ParseResult.GetValueForOption(showSecretsOption),
            MaskSensitive: context.ParseResult.GetValueForOption(maskSensitiveOption),
            IgnorePatterns: context.ParseResult.GetValueForOption(ignoreOption) ?? [],
            SensitivePatternsFile: context.ParseResult.GetValueForOption(sensitivePatternsOption),
            FailOn: context.ParseResult.GetValueForOption(failOnOption),
            SchemaFile: context.ParseResult.GetValueForOption(schemaOption),
            MaxDepth: context.ParseResult.GetValueForOption(maxDepthOption),
            PathPrefix: context.ParseResult.GetValueForOption(pathOption),
            NoColor: context.ParseResult.GetValueForOption(noColorOption),
            Verbose: context.ParseResult.GetValueForOption(verboseOption));

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Writes help information to the console.
    /// </summary>
    /// <param name="rootCommand">The root command containing help information.</param>
    private static void ShowHelp(RootCommand rootCommand)
    {
        var console = Console.Out;
        console.WriteLine("Appsettings Diff Tool");
        console.WriteLine();
        console.WriteLine("Compare configuration files (JSON/YAML) and detect differences.");
        console.WriteLine();
        console.WriteLine("USAGE:");
        console.WriteLine("  appsettings-diff [OPTIONS] <base> <target>");
        console.WriteLine("  appsettings-diff diff [OPTIONS] <base> <target>");
        console.WriteLine($"  appsettings-diff dir [OPTIONS] {DirOptionName} <DIRECTORY> {EnvsOptionName} <ENV1,ENV2,...>");
        console.WriteLine();
        console.WriteLine("EXIT CODES:");
        console.WriteLine($"  {ExitSuccess}  Success: No differences or violations found");
        console.WriteLine($"  {ExitDifferencesFound}  Failure: Differences or violations found according to {FailOnOptionName}");
        console.WriteLine($"  {ExitError}  Error: Bad arguments, missing files, or other errors");
        console.WriteLine();
        console.WriteLine("OPTIONS:");
        WriteOptionDescriptions(console, rootCommand);
    }

    /// <summary>
    /// Writes option and command descriptions to the console.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
    /// <param name="command">The <see cref="RootCommand"/> to extract information from.</param>
    private static void WriteOptionDescriptions(TextWriter writer, RootCommand command)
    {
        foreach (var option in command.Options)
        {
            writer.WriteLine($"  --{option.Name}{(option.Aliases.Count > 1 ? " (" + string.Join(", -", option.Aliases.Skip(1)) + ")" : "")}");
            writer.WriteLine($"    {option.Description}");
        }

        foreach (var subcommand in command.Subcommands)
        {
            writer.WriteLine($"  {subcommand.Name} - {subcommand.Description}");
        }

        foreach (var argument in command.Arguments)
        {
            writer.WriteLine($"  <{argument.Name}> - {argument.Description}");
        }
    }

    private sealed record OutputOptions(string? Format, bool ShowSecrets, bool MaskSensitive, string[] IgnorePatterns, FileInfo? SensitivePatternsFile, FailOn FailOn, FileInfo? SchemaFile, int? MaxDepth, string? PathPrefix, bool NoColor, bool Verbose);

    /// <summary>
    /// Executes the specified action and handles expected exceptions by printing error messages to the error console.
    /// </summary>
    /// <param name="context">The <see cref="InvocationContext"/> for the current command invocation.</param>
    /// <param name="action">The action to execute, which should return an exit code.</param>
    /// <returns>The exit code returned by the action, or <see cref="ExitError"/> if an exception occurred.</returns>
    private static int Execute(InvocationContext context, Func<int> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException
            or NotSupportedException or InvalidOperationException
            or System.Text.Json.JsonException or ArgumentException)
        {
            context.Console.Error.Write($"Error: {ex.Message}{Environment.NewLine}");
            return ExitError;
        }
    }

    /// <summary>
    /// Runs a diff between two configuration files.
    /// </summary>
    /// <param name="baseFile">The base configuration file.</param>
    /// <param name="targetFile">The target configuration file.</param>
    /// <param name="options">The <see cref="OutputOptions"/> for the diff operation.</param>
    /// <returns>The exit code for the operation.</returns>
    private static int RunFileDiff(FileInfo baseFile, FileInfo targetFile, OutputOptions options)
    {
        ArgumentNullException.ThrowIfNull(baseFile);
        ArgumentNullException.ThrowIfNull(targetFile);

        WriteDiagnostic(options, "resolved", ("base", baseFile.FullName), ("target", targetFile.FullName));
        WriteDiagnostic(options, "formats", ("base", GetFileFormat(baseFile.FullName)), ("target", GetFileFormat(targetFile.FullName)));

        var baseline = ToFlatConfig(LoadConfigFile(baseFile.FullName));
        var target = ToFlatConfig(LoadConfigFile(targetFile.FullName));
        WriteDiagnostic(options, "keys", ("base", baseline.Values.Count), ("target", target.Values.Count));
        WriteOutputDiagnostics(options);

        SensitiveKeyDetector detector;
        if (options.SensitivePatternsFile != null && options.SensitivePatternsFile.Exists)
        {
            detector = SensitiveKeyDetector.LoadWithCustomPatterns(options.SensitivePatternsFile.FullName);
        }
        else
        {
            detector = new SensitiveKeyDetector();
        }

        var differ = new ConfigDiffer(detector);
        var differOptions = new ConfigDiffOptions { MaxDepth = options.MaxDepth, PathPrefix = options.PathPrefix, IgnorePaths = options.IgnorePatterns, CaseSensitiveKeys = false, UnorderedArrays = false };
        var result = differ.Diff(baseline, target, null, baseFile.FullName, targetFile.FullName, differOptions);

        var schemaViolations = new List<SchemaViolation>();
        if (options.SchemaFile != null && options.SchemaFile.Exists)
        {
            var schema = ConfigSchema.LoadFromJson(options.SchemaFile.FullName);
            var validator = new SchemaValidator();
            schemaViolations.AddRange(validator.Validate(target.Values, schema));
        }

        WriteResult(result, schemaViolations, detector, options);

        var exitCode = ShouldFail(result, schemaViolations, options.FailOn) ? ExitDifferencesFound : ExitSuccess;
        WriteFinalDiagnostic(options, result, exitCode);
        return exitCode;
    }

    /// <summary>
    /// Determines if the diff operation should fail based on the specified failure criteria.
    /// </summary>
    /// <param name="result">The <see cref="DiffResult"/> of the operation.</param>
    /// <param name="schemaViolations">The list of <see cref="SchemaViolation"/>s found.</param>
    /// <param name="failOn">The <see cref="FailOn"/> criteria.</param>
    /// <returns>True if the operation should fail; otherwise, false.</returns>
    private static bool ShouldFail(DiffResult result, List<SchemaViolation> schemaViolations, FailOn failOn)
    {
        if (failOn == FailOn.None) return false;

        if (failOn == FailOn.Any)
        {
            return result.HasDifferences || schemaViolations.Count > 0;
        }

        if (failOn == FailOn.Missing)
        {
            return result.CountOf(DiffKind.Removed) > 0 || schemaViolations.Any(v => v.IsMissing);
        }

        if (failOn == FailOn.SchemaViolation)
        {
            return schemaViolations.Count > 0;
        }

        return false;
    }

    /// <summary>
    /// Runs a diff across configuration files in a directory for multiple environments.
    /// </summary>
    /// <param name="dir">The directory containing configuration files.</param>
    /// <param name="envs">The environments to compare.</param>
    /// <param name="options">The <see cref="OutputOptions"/> for the diff operation.</param>
    /// <returns>The exit code for the operation.</returns>
    private static int RunDirectoryDiff(DirectoryInfo? dir, string[]? envs, OutputOptions options)
    {
        if (dir is null)
            throw new ArgumentException(Messages.DirectoryOptionRequired);

        var environments = (envs ?? [])
            .SelectMany(e => e.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();

        if (environments.Length < 2)
            throw new ArgumentException(Messages.InsufficientEnvironments);

        WriteDiagnostic(options, "resolved", ("directory", dir.FullName), ("environments", environments));
        WriteOutputDiagnostics(options);

        SensitiveKeyDetector detector;
        if (options.SensitivePatternsFile != null && options.SensitivePatternsFile.Exists)
        {
            detector = SensitiveKeyDetector.LoadWithCustomPatterns(options.SensitivePatternsFile.FullName);
        }
        else
        {
            detector = new SensitiveKeyDetector();
        }

        var differ = new ConfigDiffer(detector);
        var differOptions = new ConfigDiffOptions { MaxDepth = options.MaxDepth, PathPrefix = options.PathPrefix, IgnorePaths = options.IgnorePatterns, CaseSensitiveKeys = false, UnorderedArrays = false };

        var baselineEnv = environments[0];
        var baseline = ToFlatConfig(LoadEnvironmentConfig(dir, baselineEnv, options, "base"));

        var anyFail = false;
        var totalAdded = 0;
        var totalRemoved = 0;
        var totalChanged = 0;
        foreach (var env in environments.Skip(1))
        {
            var target = ToFlatConfig(LoadEnvironmentConfig(dir, env, options, "target"));
            WriteDiagnostic(options, "keys", ("base", baseline.Values.Count), ("target", target.Values.Count));
            var result = differ.Diff(baseline, target, options.IgnorePatterns, baselineEnv, env, differOptions);

            var schemaViolations = new List<SchemaViolation>();
            if (options.SchemaFile != null && options.SchemaFile.Exists)
            {
                var schema = ConfigSchema.LoadFromJson(options.SchemaFile.FullName);
                var validator = new SchemaValidator();
                schemaViolations.AddRange(validator.Validate(target.Values, schema));
            }

            WriteResult(result, schemaViolations, detector, options);
            var comparisonExitCode = ShouldFail(result, schemaViolations, options.FailOn) ? ExitDifferencesFound : ExitSuccess;
            totalAdded += result.CountOf(DiffKind.Added);
            totalRemoved += result.CountOf(DiffKind.Removed);
            totalChanged += result.CountOf(DiffKind.Changed);
            if (comparisonExitCode != ExitSuccess)
            {
                anyFail = true;
            }
        }

        var exitCode = anyFail ? ExitDifferencesFound : ExitSuccess;
        WriteDiagnostic(options, "result", ("added", totalAdded), ("removed", totalRemoved),
            ("changed", totalChanged), ("exitCode", exitCode));
        return exitCode;
    }

    /// <summary>
    /// Writes the diff result and schema violations to the output.
    /// </summary>
    /// <param name="result">The <see cref="DiffResult"/> to write.</param>
    /// <param name="schemaViolations">The list of <see cref="SchemaViolation"/>s to write.</param>
    /// <param name="detector">The <see cref="SensitiveKeyDetector"/> used for redaction.</param>
    /// <param name="options">The <see cref="OutputOptions"/>.</param>
    private static void WriteResult(DiffResult result, List<SchemaViolation> schemaViolations, SensitiveKeyDetector detector, OutputOptions options)
    {
        var writer = DiffReportWriterFactory.Create(options.Format, detector, options.ShowSecrets, options.MaskSensitive);
        var formatWriter = DiffReportWriterRegistry.GetFormatWriter(options.Format);

        if (formatWriter != null)
        {
            formatWriter(writer, result, schemaViolations, options.NoColor);
        }
        else
        {
            // Fallback to console output if format writer not found
            writer.WriteConsole(result, options.NoColor);
        }

        if (schemaViolations.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine(Messages.SchemaViolationsHeader);
            foreach (var v in schemaViolations)
            {
                Console.WriteLine($"- {v.Key}: {v.Message}");
            }
        }
    }


    /// <summary>
    /// Loads a configuration file (JSON, YAML, or .env) into a flat key-value dictionary
    /// using the same "Section:Key" convention as ASP.NET Core configuration.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    /// <returns>A dictionary containing the configuration keys and values.</returns>
    private static Dictionary<string, string> LoadConfigFile(string path)
    {
        var extension = Path.GetExtension(path);

        // Read the file content first to detect empty or whitespace‑only files.
        // If the file is empty, treat it as an empty configuration and emit a warning.
        string fileContent = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            Console.Error.WriteLine($"Warning: configuration file '{path}' is empty or whitespace only; treating as empty configuration.");
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        if (extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".yml", StringComparison.OrdinalIgnoreCase))
        {
            return YamlConfigReader.ReadFile(path);
        }

        if (extension.Equals(".env", StringComparison.OrdinalIgnoreCase))
        {
            return DotEnvReader.ReadFile(path);
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.GetFullPath(path), optional: false, reloadOnChange: false)
            .Build();

        try
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in configuration.AsEnumerable())
            {
                if (value is not null)
                    result[key] = value;
            }

            return result;
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Builds the effective configuration for an environment: the shared appsettings file
    /// (if present) overlaid with the environment-specific appsettings.{env} file.
    /// </summary>
    /// <param name="dir">The <see cref="DirectoryInfo"/> containing configuration files.</param>
    /// <param name="environment">The name of the environment.</param>
    /// <param name="options">The output options controlling diagnostics.</param>
    /// <param name="side">The comparison side being loaded.</param>
    /// <returns>A dictionary containing the effective configuration.</returns>
    private static Dictionary<string, string> LoadEnvironmentConfig(DirectoryInfo dir, string environment, OutputOptions options, string side)
    {
        var envFile = FindConfigFile(dir, $"appsettings.{environment}")
            ?? throw new FileNotFoundException(Messages.FileNotFound(environment, dir.FullName));

        var sharedFile = FindConfigFile(dir, "appsettings");
        WriteDiagnostic(options, "resolved", ("side", side), ("environment", environment),
            ("shared", sharedFile ?? "none"), ("environmentFile", envFile));
        WriteDiagnostic(options, "formats", ("side", side),
            ("shared", sharedFile is null ? "none" : GetFileFormat(sharedFile)),
            ("environment", GetFileFormat(envFile)));
        var effective = sharedFile is not null
            ? new Dictionary<string, string>(LoadConfigFile(sharedFile), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in LoadConfigFile(envFile))
            effective[key] = value;

        return effective;
    }

    private static string GetFileFormat(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".yaml" or ".yml" => "yaml",
            ".env" => "env",
            _ => "json"
        };
    }

    private static void WriteOutputDiagnostics(OutputOptions options)
    {
        WriteDiagnostic(options, "report", ("format", string.IsNullOrWhiteSpace(options.Format) ? DefaultFormat : options.Format));
        WriteDiagnostic(options, "ignore", ("count", options.IgnorePatterns.Length), ("patterns", options.IgnorePatterns));
    }

    private static void WriteFinalDiagnostic(OutputOptions options, DiffResult result, int exitCode)
    {
        WriteDiagnostic(options, "result", ("added", result.CountOf(DiffKind.Added)),
            ("removed", result.CountOf(DiffKind.Removed)), ("changed", result.CountOf(DiffKind.Changed)),
            ("exitCode", exitCode));
    }

    private static void WriteDiagnostic(OutputOptions options, string step, params (string Key, object? Value)[] values)
    {
        if (!options.Verbose)
            return;

        var fields = values.Select(value => $"{value.Key}={FormatDiagnosticValue(value.Value)}");
        Console.Error.WriteLine($"[appsettings-diff] step={step} {string.Join(' ', fields)}");
    }

    private static string FormatDiagnosticValue(object? value)
    {
        return value switch
        {
            null => "null",
            string text when text.Length > 0 && text.All(c => !char.IsWhiteSpace(c) && c != '=' && c != '"') => text,
            string text => JsonSerializer.Serialize(text),
            string[] items => JsonSerializer.Serialize(items),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null"
        };
    }

    /// <summary>
    /// Finds a configuration file by name in the specified directory, checking supported extensions.
    /// </summary>
    /// <param name="dir">The <see cref="DirectoryInfo"/> to search in.</param>
    /// <param name="baseName">The base name of the configuration file.</param>
    /// <returns>The path to the file if found; otherwise, null.</returns>
    private static string? FindConfigFile(DirectoryInfo dir, string baseName)
    {
        foreach (var extension in SupportedExtensions)
        {
            var candidate = Path.Combine(dir.FullName, baseName + extension);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Converts a flat key-value dictionary to a <see cref="FlatConfig"/>.
    /// </summary>
    /// <param name="values">The dictionary of configuration values.</param>
    /// <returns>A <see cref="FlatConfig"/> instance.</returns>
    private static FlatConfig ToFlatConfig(Dictionary<string, string> values)
    {
        var config = new FlatConfig();
        foreach (var (key, value) in values)
            config.Values[key] = value;

        return config;
    }
}
