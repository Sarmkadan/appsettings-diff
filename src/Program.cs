using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;

namespace AppsettingsDiff;

/// <summary>
/// Command-line entry point for the appsettings diff tool.
/// </summary>
public static class Program
{
    private static readonly string[] SupportedExtensions = [".json", ".yaml", ".yml"];

    /// <summary>
    /// Runs the CLI. Exit codes: 0 on success, 1 when --fail-on-diff is set and differences exist, 2 on errors.
    /// </summary>
    /// <param name="args">Raw command-line arguments.</param>
    public static async Task<int> Main(string[] args)
    {
        var baseArgument = new Argument<FileInfo>("base", "The base JSON/YAML file").ExistingOnly();
        var targetArgument = new Argument<FileInfo>("target", "The target JSON/YAML file").ExistingOnly();

        var dirOption = new Option<DirectoryInfo>("--dir", "The directory containing configuration files").ExistingOnly();
        var envsOption = new Option<string[]>("--envs", "The environments to compare (comma-separated, e.g. Production,Staging)") { AllowMultipleArgumentsPerToken = true };

        var jsonOption = new Option<bool>("--json", "Output in JSON format");
        var markdownOption = new Option<bool>("--markdown", "Output in Markdown format");
        var showSecretsOption = new Option<bool>("--show-secrets", "Show sensitive keys");
        var ignoreOption = new Option<string[]>("--ignore", "Glob patterns of keys to ignore") { AllowMultipleArgumentsPerToken = true };
        var failOnDiffOption = new Option<bool>("--fail-on-diff", "Exit with 1 if differences are found");

        var rootCommand = new RootCommand("Appsettings Diff Tool");

        // Mode 1: <base> <target>
        var diffCommand = new Command("diff", "Compare two configuration files");
        diffCommand.AddArgument(baseArgument);
        diffCommand.AddArgument(targetArgument);
        diffCommand.AddOption(jsonOption);
        diffCommand.AddOption(markdownOption);
        diffCommand.AddOption(showSecretsOption);
        diffCommand.AddOption(ignoreOption);
        diffCommand.AddOption(failOnDiffOption);

        // Mode 2: --dir --envs
        var dirCommand = new Command("dir", "Compare configuration files in a directory");
        dirCommand.AddOption(dirOption);
        dirCommand.AddOption(envsOption);
        dirCommand.AddOption(jsonOption);
        dirCommand.AddOption(markdownOption);
        dirCommand.AddOption(showSecretsOption);
        dirCommand.AddOption(ignoreOption);
        dirCommand.AddOption(failOnDiffOption);

        rootCommand.AddCommand(diffCommand);
        rootCommand.AddCommand(dirCommand);

        // The bare invocation `appsettings-diff <base> <target>` behaves like `diff`.
        rootCommand.AddArgument(baseArgument);
        rootCommand.AddArgument(targetArgument);
        rootCommand.AddOption(jsonOption);
        rootCommand.AddOption(markdownOption);
        rootCommand.AddOption(showSecretsOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(failOnDiffOption);

        void HandleDiff(InvocationContext context)
        {
            var baseFile = context.ParseResult.GetValueForArgument(baseArgument);
            var targetFile = context.ParseResult.GetValueForArgument(targetArgument);
            var options = ReadOutputOptions(context);

            context.ExitCode = Execute(context, () => RunFileDiff(baseFile, targetFile, options));
        }

        void HandleDir(InvocationContext context)
        {
            var dir = context.ParseResult.GetValueForOption(dirOption);
            var envs = context.ParseResult.GetValueForOption(envsOption);
            var options = ReadOutputOptions(context);

            context.ExitCode = Execute(context, () => RunDirectoryDiff(dir, envs, options));
        }

        OutputOptions ReadOutputOptions(InvocationContext context) => new(
            Json: context.ParseResult.GetValueForOption(jsonOption),
            Markdown: context.ParseResult.GetValueForOption(markdownOption),
            ShowSecrets: context.ParseResult.GetValueForOption(showSecretsOption),
            IgnorePatterns: context.ParseResult.GetValueForOption(ignoreOption) ?? [],
            FailOnDiff: context.ParseResult.GetValueForOption(failOnDiffOption));

        rootCommand.SetHandler(HandleDiff);
        diffCommand.SetHandler(HandleDiff);
        dirCommand.SetHandler(HandleDir);

        return await rootCommand.InvokeAsync(args);
    }

    private sealed record OutputOptions(bool Json, bool Markdown, bool ShowSecrets, string[] IgnorePatterns, bool FailOnDiff);

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

        var detector = new SensitiveKeyDetector();
        var differ = new ConfigDiffer(detector);
        var result = differ.Diff(baseline, target, options.IgnorePatterns, baseFile.FullName, targetFile.FullName);

        WriteResult(result, detector, options);

        return options.FailOnDiff && result.HasDifferences ? 1 : 0;
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

        var detector = new SensitiveKeyDetector();
        var differ = new ConfigDiffer(detector);

        var baselineEnv = environments[0];
        var baseline = ToFlatConfig(LoadEnvironmentConfig(dir, baselineEnv));

        var anyDifferences = false;
        foreach (var env in environments.Skip(1))
        {
            var target = ToFlatConfig(LoadEnvironmentConfig(dir, env));
            var result = differ.Diff(baseline, target, options.IgnorePatterns, baselineEnv, env);

            WriteResult(result, detector, options);
            anyDifferences |= result.HasDifferences;
        }

        return options.FailOnDiff && anyDifferences ? 1 : 0;
    }

    private static void WriteResult(DiffResult result, SensitiveKeyDetector detector, OutputOptions options)
    {
        var writer = new DiffReportWriter(detector, options.ShowSecrets);

        if (options.Json)
            Console.WriteLine(writer.ToJson(result));
        else if (options.Markdown)
            Console.WriteLine(writer.ToMarkdown(result));
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
