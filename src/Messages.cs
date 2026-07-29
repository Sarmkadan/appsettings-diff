namespace AppsettingsDiff;

public static class Messages
{
    // SchemaValidator messages
    public const string FailedToLoadSchema = "Failed to load schema from JSON";
    public static string RequiredKeyMissing(string key) => $"Required key '{key}' is missing";
    public const string ValueMustBeInteger = "Value must be a valid integer";
    public const string ValueMustBeBoolean = "Value must be a valid boolean";
    public const string ValueMustBeDouble = "Value must be a valid double";
    public const string ValueMustBeDateTime = "Value must be a valid DateTime";
    public const string ValueMustBeUrl = "Value must be a valid URL";
    public const string ValueMustBeGuid = "Value must be a valid GUID";
    public static string UnknownTypeHint(string typeHint) => $"Unknown type hint '{typeHint}'";
    public const string ConnectionStringCredentialsDetected = "Configuration value contains a connection string with credentials (detected Password= or pwd= pattern)";
    public static string CasingConflict(string key) => $"Key '{key}' differs only by casing from another key in config. Keys are case-insensitive and should have unique casing.";
    public static string UnknownKeyPresent(string key) => $"Unknown key '{key}' is present in config but not defined in schema";

    // Program messages
    public const string DirectoryOptionRequired = "The --dir option is required for directory mode.";
    public const string InsufficientEnvironments = "At least two environments must be specified via --envs (e.g. --envs Production,Staging).";
    public static string FileNotFound(string environment, string dir) => $"No appsettings.{environment}.(json|yaml|yml) file found in '{dir}'.";
    public const string SchemaViolationsHeader = "SCHEMA VIOLATIONS:";
}
