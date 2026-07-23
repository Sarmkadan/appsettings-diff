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
    private static readonly string[] SupportedExtensions = [".json", ".yaml", ".yml", ".env"];

    /// <summary>
    /// Runs the CLI.
    ///
    /// Exit codes:
    /// 0 - Success: No differences or violations found
    /// 1 - Failure: Differences or violations found according to --fail-on
    /// 2 - Error: Bad arguments, missing files, or other errors
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        var baseArgument = new Argument<FileInfo>("base", "The base JSON/YAML file (use - to read from stdin)").ExistingOnly();
        var targetArgument = new Argument<FileInfo>("target", "The target JSON/YAML file (use - to read from stdin)").ExistingOnly();

        var dirOption = new Option<DirectoryInfo>("--dir", "The directory containing configuration files").ExistingOnly();
        var envsOption = new Option<string[]>("--envs", "The environments to compare (comma-separated, e.g. Production,Staging)") { AllowMultipleArgumentsPerToken = true };

        var formatOption = new Option<string?>("--format", "Output format (json, markdown, html, jsonpatch, summary-json)");
        var showSecretsOption = new Option<bool>("--show-secrets", "Show sensitive keys");
        var maskSensitiveOption = new Option<bool>("--mask-sensitive", "Mask sensitive values with *** instead of showing [REDACTED]");
        var ignoreOption = new Option<string[]>("--ignore", "Glob patterns of keys to ignore") { AllowMultipleArgumentsPerToken = true };
        var sensitivePatternsOption = new Option<FileInfo?>("--sensitive-patterns", "File containing additional sensitive key patterns (one per line, # comments allowed)");
        var failOnOption = new Option<FailOn>("--fail-on", "Which categories cause a non-zero exit code (missing|any|schema-violation)");
        var schemaOption = new Option<FileInfo?>("--schema", "JSON schema file for validation").ExistingOnly();
        var maxDepthOption = new Option<int?>("--max-depth", "Maximum depth to compare nested structures (0 = no limit)");
        var pathOption = new Option<string?>("--path", "Only compare keys under the given key-path prefix (e.g. Logging:LogLevel)");
        var noColorOption = new Option<bool>("--no-color", "Disable ANSI color output");

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

        rootCommand.SetHandler((InvocationContext context) =>
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0] == "--help" || args[0] == "-h")))
            {
                ShowHelp(rootCommand);
                context.ExitCode = 0;
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
            NoColor: context.ParseResult.GetValueForOption(noColorOption));

        return await rootCommand.InvokeAsync(args);
    }

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
        console.WriteLine("  appsettings-diff dir [OPTIONS] --dir <DIRECTORY> --envs <ENV1,ENV2,...>");
        console.WriteLine();
        console.WriteLine("EXIT CODES:");
        console.WriteLine("  0  Success: No differences or violations found");
        console.WriteLine("  1  Failure: Differences or violations found according to --fail-on");
        console.WriteLine("  2  Error: Bad arguments, missing files, or other errors");
        console.WriteLine();
        console.WriteLine("OPTIONS:");
        WriteOptionDescriptions(console, rootCommand);
    }

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

    private sealed record OutputOptions(string? Format, bool ShowSecrets, bool MaskSensitive, string[] IgnorePatterns, FileInfo? SensitivePatternsFile, FailOn FailOn, FileInfo? SchemaFile, int? MaxDepth, string? PathPrefix, bool NoColor);

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
            return 2;
        }
    }

    private static int RunFileDiff(FileInfo baseFile, FileInfo targetFile, OutputOptions options)
    {
        ArgumentNullException.ThrowIfNull(baseFile);
        ArgumentNullException.ThrowIfNull(targetFile);

        var baseline = ToFlatConfig(LoadConfigFile(baseFile.FullName));
        var target = ToFlatConfig(LoadConfigFile(targetFile.FullName));

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
        var differOptions = new ConfigDifferOptions { MaxDepth = options.MaxDepth, PathPrefix = options.PathPrefix };
        var result = differ.Diff(baseline, target, options.IgnorePatterns, baseFile.FullName, targetFile.FullName, differOptions);

        var schemaViolations = new List<SchemaViolation>();
        if (options.SchemaFile != null && options.SchemaFile.Exists)
        {
            var schema = ConfigSchema.LoadFromJson(options.SchemaFile.FullName);
            var validator = new SchemaValidator();
            schemaViolations.AddRange(validator.Validate(target.Values, schema));
        }

        WriteResult(result, schemaViolations, detector, options);

        return ShouldFail(result, schemaViolations, options.FailOn) ? 1 : 0;
    }

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

    private static int RunDirectoryDiff(DirectoryInfo? dir, string[]? envs, OutputOptions options)
    {
        if (dir is null)
            throw new ArgumentException("The --dir option is required for directory mode.");

        var environments = (envs ?? [])
            .SelectMany(e => e.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();

        if (environments.Length < 2)
            throw new ArgumentException("At least two environments must be specified via --envs (e.g. --envs Production,Staging).");

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
        var differOptions = new ConfigDifferOptions { MaxDepth = options.MaxDepth, PathPrefix = options.PathPrefix };

        var baselineEnv = environments[0];
        var baseline = ToFlatConfig(LoadEnvironmentConfig(dir, baselineEnv));

        var anyFail = false;
        foreach (var env in environments.Skip(1))
        {
            var target = ToFlatConfig(LoadEnvironmentConfig(dir, env));
            var result = differ.Diff(baseline, target, options.IgnorePatterns, baselineEnv, env, differOptions);

            var schemaViolations = new List<SchemaViolation>();
            if (options.SchemaFile != null && options.SchemaFile.Exists)
            {
                var schema = ConfigSchema.LoadFromJson(options.SchemaFile.FullName);
                var validator = new SchemaValidator();
                schemaViolations.AddRange(validator.Validate(target.Values, schema));
            }

            WriteResult(result, schemaViolations, detector, options);
            if (ShouldFail(result, schemaViolations, options.FailOn))
            {
                anyFail = true;
            }
        }

        return anyFail ? 1 : 0;
    }

    private static void WriteResult(DiffResult result, List<SchemaViolation> schemaViolations, SensitiveKeyDetector detector, OutputOptions options)
    {
        var writer = DiffReportWriterFactory.Create(options.Format, detector, options.ShowSecrets, options.MaskSensitive);

        if (options.Format == "json")
            Console.WriteLine(writer.ToJson(result));
        else if (options.Format == "markdown")
            writer.WriteMarkdown(result, Console.Out);
        else if (options.Format == "html")
            writer.WriteHtml(result, Console.Out);
        else if (options.Format == "jsonpatch")
            Console.WriteLine(writer.ToJsonPatch(result));
        else if (options.Format == "summary-json")
        {
            var summary = new
            {
                added = result.Entries
                    .Where(e => e.Kind == DiffKind.Added)
                    .Select(e => e.Key)
                    .ToArray(),
                removed = result.Entries
                    .Where(e => e.Kind == DiffKind.Removed)
                    .Select(e => e.Key)
                    .ToArray(),
                changed = result.Entries
                    .Where(e => e.Kind == DiffKind.Changed)
                    .Select(e => e.Key)
                    .ToArray()
            };
            var json = JsonSerializer.Serialize(summary);
            Console.WriteLine(json);
        }
        else
            writer.WriteConsole(result, options.NoColor);

        if (schemaViolations.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("SCHEMA VIOLATIONS:");
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
    private static Dictionary<string, string> LoadConfigFile(string path)
    {
        var extension = Path.GetExtension(path);

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
    private static Dictionary<string, string> LoadEnvironmentConfig(DirectoryInfo dir, string environment)
    {
        var envFile = FindConfigFile(dir, $"appsettings.{environment}")
            ?? throw new FileNotFoundException($"No appsettings.{environment}.(json|yaml|yml) file found in '{dir.FullName}'.");

        var sharedFile = FindConfigFile(dir, "appsettings");
        var effective = sharedFile is not null
            ? new Dictionary<string, string>(LoadConfigFile(sharedFile), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in LoadConfigFile(envFile))
            effective[key] = value;

        return effective;
    }

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

    private static FlatConfig ToFlatConfig(Dictionary<string, string> values)
    {
        var config = new FlatConfig();
        foreach (var (key, value) in values)
            config.Values[key] = value;

        return config;
    }
}
