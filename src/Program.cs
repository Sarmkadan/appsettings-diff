using System.CommandLine;
using System.CommandLine.Invocation;
using AppsettingsDiff;

namespace AppsettingsDiff;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var baseOption = new Argument<FileInfo>("base", "The base JSON/YAML file").ExistingOnly();
        var targetOption = new Argument<FileInfo>("target", "The target JSON/YAML file").ExistingOnly();

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
        diffCommand.AddArgument(baseOption);
        diffCommand.AddArgument(targetOption);
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

        // The root command itself can also act as the default for the first mode if desired,
        // but the prompt suggests `appsettings-diff <base> <target>` directly.
        // Let's add the options to the RootCommand to allow `appsettings-diff <base> <target>`.
        rootCommand.AddArgument(baseOption);
        rootCommand.AddArgument(targetOption);
        rootCommand.AddOption(dirOption);
        rootCommand.AddOption(envsOption);
        rootCommand.AddOption(jsonOption);
        rootCommand.AddOption(markdownOption);
        rootCommand.AddOption(showSecretsOption);
        rootCommand.AddOption(ignoreOption);
        rootCommand.AddOption(failOnDiffOption);

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var baseFile = context.ParseResult.GetValueForArgument(baseOption);
            var targetFile = context.ParseResult.GetValueForArgument(targetOption);
            var dir = context.ParseResult.GetValueForOption(dirOption);
            var envs = context.ParseResult.GetValueForOption(envsOption);
            var json = context.ParseResult.GetValueForOption(jsonOption);
            var markdown = context.ParseResult.GetValueForOption(markdownOption);
            var showSecrets = context.ParseResult.GetValueForOption(showSecretsOption);
            var ignore = context.ParseResult.GetValueForOption(ignoreOption);
            var failOnDiff = context.ParseResult.GetValueForOption(failOnDiffOption);

            // Implementation logic here
            Console.WriteLine("Running diff...");
            await Task.CompletedTask;
        });

        return await rootCommand.InvokeAsync(args);
    }
}
