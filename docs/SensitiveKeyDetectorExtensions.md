# SensitiveKeyDetectorExtensions

The `SensitiveKeyDetectorExtensions` class provides a set of static extension methods and utility functions designed to analyze configuration keys for potential security risks. It enables developers to programmatically identify sensitive data patterns, such as database credentials, API keys, and other high-risk configuration settings, facilitating automated redaction, auditing, or enhanced logging protocols within the `appsettings-diff` ecosystem.

## API

### `IsSensitiveKey`
Determines if a specific configuration key is explicitly classified as sensitive based on known dangerous patterns.
*   **Parameters**: `string key` – The configuration key to evaluate.
*   **Returns**: `bool` – `true` if the key matches a definitive sensitive pattern; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `key` is null.

### `IsPotentiallySensitive`
Evaluates whether a configuration key exhibits characteristics that suggest it might contain sensitive data, even if it does not match a strict allowlist of known sensitive keys.
*   **Parameters**: `string key` – The configuration key to evaluate.
*   **Returns**: `bool` – `true` if the key contains substrings or structures commonly associated with secrets; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `key` is null.

### `IsDatabaseCredential`
Specifically checks if the provided key corresponds to database authentication details (e.g., connection strings, usernames, passwords).
*   **Parameters**: `string key` – The configuration key to evaluate.
*   **Returns**: `bool` – `true` if the key is identified as a database credential; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `key` is null.

### `IsApiCredential`
Specifically checks if the provided key corresponds to external API authentication details (e.g., API keys, tokens, secrets).
*   **Parameters**: `string key` – The configuration key to evaluate.
*   **Returns**: `bool` – `true` if the key is identified as an API credential; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `key` is null.

### `GetSensitivityLevel`
Calculates a numeric severity score representing the sensitivity of the given configuration key.
*   **Parameters**: `string key` – The configuration key to evaluate.
*   **Returns**: `int` – A non-negative integer where higher values indicate greater sensitivity.
*   **Throws**: `ArgumentNullException` if `key` is null.

### `RequiresExtraCaution`
Determines if the configuration key necessitates additional security handling measures beyond standard redaction, typically due to composite risk factors.
*   **Parameters**: `string key` – The configuration key to evaluate.
*   **Returns**: `bool` – `true` if extra caution is required; otherwise, `false`.
*   **Throws**: `ArgumentNullException` if `key` is null.

## Usage

### Example 1: Conditional Redaction in Logging
This example demonstrates how to use the detection methods to scrub sensitive values before writing them to a log file.

```csharp
using AppSettingsDiff.Extensions;

public void LogConfigurationSetting(string key, string value)
{
    if (SensitiveKeyDetectorExtensions.IsSensitiveKey(key))
    {
        Console.WriteLine($"{key}: [REDACTED]");
        return;
    }

    if (SensitiveKeyDetectorExtensions.IsPotentiallySensitive(key))
    {
        Console.WriteLine($"{key}: [MASKED_PARTIAL]");
        // Apply partial masking logic here
        return;
    }

    Console.WriteLine($"{key}: {value}");
}
```

### Example 2: Risk-Based Audit Scoring
This example utilizes the sensitivity level and specific credential checks to generate a risk score for a configuration section.

```csharp
using AppSettingsDiff.Extensions;
using System.Collections.Generic;

public int CalculateConfigRiskScore(Dictionary<string, string> configItems)
{
    int totalRisk = 0;

    foreach (var item in configItems)
    {
        string key = item.Key;
        
        // Base score from general sensitivity analysis
        totalRisk += SensitiveKeyDetectorExtensions.GetSensitivityLevel(key);

        // Add penalty for specific high-value targets
        if (SensitiveKeyDetectorExtensions.IsDatabaseCredential(key))
        {
            totalRisk += 10;
        }
        
        if (SensitiveKeyDetectorExtensions.IsApiCredential(key) && 
            SensitiveKeyDetectorExtensions.RequiresExtraCaution(key))
        {
            totalRisk += 15;
        }
    }

    return totalRisk;
}
```

## Notes

*   **Null Handling**: All methods in this class strictly validate input arguments. Passing a `null` key to any method will result in an `ArgumentNullException`. Callers must ensure keys are non-null prior to invocation.
*   **Pattern Matching Logic**: The distinction between `IsSensitiveKey` and `IsPotentiallySensitive` relies on internal pattern matching. `IsSensitiveKey` returns `true` only for explicit matches, whereas `IsPotentiallySensitive` may return `true` for keys containing ambiguous substrings (e.g., "Key", "Secret", "Token") that warrant review but are not definitively classified.
*   **Thread Safety**: As this class consists entirely of stateless static methods with no shared mutable state, all members are thread-safe and can be called concurrently from multiple threads without external synchronization.
*   **Sensitivity Levels**: The integer returned by `GetSensitivityLevel` is relative. A return value of `0` indicates no detected sensitivity, while increasing values correlate with the likelihood and severity of the data being a security critical secret.
