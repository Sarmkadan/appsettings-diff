using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;

namespace AppsettingsDiff;

/// <summary>
/// Command-line entry point for the appsettings diff tool.
/// </summary>
public static class Program
{
    private static readonly string[] SupportedExtensions =[".json", ".yaml", ".yml"];

    /// <summary>
    /// Runs the CLI.
    ///
    /// Exit codes:
    /// 0 - Success: No differences found or successful execution
    /// 1 - Differences found: When --fail-on-diff is set and differences exist
    /// 2 - Error: Bad arguments, missing files, or other errors
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        var baseArgument = new Argument<FileInfo>("base", "The base JSON/YAML file (use - to read from stdin)").ExistingOnly();
        var targetArgument = new Argument<FileInfo>("target", "The target JSON/YAML file (use - to read from stdin)").ExistingOnly();

        var dirOption = new Option<DirectoryInfo>("--dir", "The directory containing configuration files").ExistingOnly();
        var envsOption = new Option<string[]>("--envs", "The environments to compare (comma-separated, e.g. Production,Staging)") { AllowMultipleArgumentsPerToken = true };

        var formatOption = new Option<string?>("--format", "Output format (json, markdown, html, jsonpatch)");
        var showSecretsOption = new Option<bool>("--show-secrets", "Show sensitive keys");
        var ignoreOption = new Option<string[]>("--ignore", "Glob patterns of keys to ignore") { AllowMultipleArgumentsPerToken = true };
        var sensitivePatternsOption = new Option<FileInfo?>("--sensitive-patterns", "File containing additional sensitive key patterns (one per line, # comments allowed)");
        var failOnDiffOption = new Option<bool>("--fail-on-diff", "Exit with code 1 if differences are found");
        var failOnChangesOption = new Option<bool>("--fail-on-changes", "Exit with code 2 if changes (additions/removals/modifications) are found");

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
        diffCommand.AddOption(ignoreOption);
        diffCommand.AddOption(sensitivePatternsOption);
        diffCommand.AddOption(failOnDiffOption);
        diffCommand.AddOption(failOnChangesOption);

        // Mode 2: --dir --envs
        var dirCommand = new Command("dir", "Compare configuration files in a directory")
        {
            Description = "Compare configuration files across multiple environments in a directory"
        };
        dirCommand.AddOption(dirOption);
        dirCommand.AddOption(envsOption);
        dirCommand.AddOption(formatOption);
        dirCommand.AddOption(showSecretsOption);
        dirCommand.AddOption(ignoreOption);
        dirCommand.AddOption(sensitivePatternsOption);
        dirCommand.AddOption(failOnDiffOption);
        dirCommand.AddOption(failOnChangesOption);

        rootCommand.AddCommand(diffCommand);
        rootCommand.AddCommand(dirCommand);

        // The bare invocation `appsettings-diff <base> <target>` behaves like `diff`.
        rootCommand.AddArgument(baseArgument);
        rootCommand.AddArgument(targetArgument);
        rootCommand.AddOption(formatOption);
        rootCommand.AddOption(showSecretsOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(sensitivePatternsOption);
        rootCommand.AddOption(failOnDiffOption);
        rootCommand.AddOption(failOnChangesOption);

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
            IgnorePatterns: context.ParseResult.GetValueForOption(ignoreOption) ?? [],
            SensitivePatternsFile: context.ParseResult.GetValueForOption(sensitivePatternsOption),
            FailOnDiff: context.ParseResult.GetValueForOption(failOnDiffOption),
            FailOnChanges: context.ParseResult.GetValueForOption(failOnChangesOption));

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
        console.WriteLine("  0  Success: No differences found or successful execution");
        console.WriteLine("  1  Differences found: When --fail-on-diff is set and differences exist");
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

    private sealed record OutputOptions(string? Format, bool ShowSecrets, string[] IgnorePatterns, FileInfo? SensitivePatternsFile, bool FailOnDiff, bool FailOnChanges);

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
        var result = differ.Diff(baseline, target, options.IgnorePatterns, baseFile.FullName, targetFile.FullName);

        WriteResult(result, detector, options);

        return options.FailOnDiff && result.HasDifferences ? 1 : options.FailOnChanges && (result.CountOf(DiffKind.Added) > 0 || result.CountOf(DiffKind.Removed) > 0 || result.CountOf(DiffKind.Changed) > 0) ? 2 : 0;
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

        var baselineEnv = environments[0];
        var baseline = ToFlatConfig(LoadEnvironmentConfig(dir, baselineEnv));

        var anyDifferences = false;
        var anyChanges = false;
        foreach (var env in environments.Skip(1))
        {
            var target = ToFlatConfig(LoadEnvironmentConfig(dir, env));
            var result = differ.Diff(baseline, target, options.IgnorePatterns, baselineEnv, env);

            WriteResult(result, detector, options);
            anyDifferences |= result.HasDifferences;
            anyChanges |= result.CountOf(DiffKind.Added) > 0 || result.CountOf(DiffKind.Removed) > 0 || result.CountOf(DiffKind.Changed) > 0;
        }

        return options.FailOnDiff && anyDifferences ? 1 : options.FailOnChanges && anyChanges ? 2 : 0;
    }

    private static void WriteResult(DiffResult result, SensitiveKeyDetector detector, OutputOptions options)
    {
        var writer = new DiffReportWriter(detector, options.ShowSecrets);

        if (options.Format == "json")
            Console.WriteLine(writer.ToJson(result));
        else if (options.Format == "markdown")
            writer.WriteMarkdown(result, Console.Out);
        else if (options.Format == "html")
            writer.WriteHtml(result, Console.Out);
    else if (options.Format == "jsonpatch")
        Console.WriteLine(writer.ToJsonPatch(result));
    else
        writer.WriteConsoleSummary(result);
    }

    /// <summary>
    /// Loads a configuration file (JSON or YAML) into a flat key-value dictionary
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
