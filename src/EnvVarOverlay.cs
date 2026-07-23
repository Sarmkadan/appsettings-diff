namespace AppsettingsDiff;

/// <summary>
/// Накладывает переменные окружения поверх FlatConfig по правилам ASP.NET Core.
/// Обрабатывает специальные префиксы (ASPNETCORE_, DOTNET_) и замену '__' на ':'
/// </summary>
public static class EnvVarOverlay
{
    /// <summary>
    /// Считывает переменные окружения с указанным префиксом.
    /// </summary>
    /// <param name="prefix">Префикс для фильтрации переменных окружения (null - все переменные).</param>
    /// <returns>Словарь переменных окружения.</returns>
    public static Dictionary<string, string> ReadFromEnvironment(string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                if (prefix == null || key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Нормализует переменные окружения: удаляет префиксы ASPNETCORE_ и DOTNET_, заменяет '__' на ':'.
    /// </summary>
    /// <param name="envVars">Исходные переменные окружения.</param>
    /// <returns>Нормализованные переменные.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="envVars"/> равен <see langword="null"/>.</exception>
    public static Dictionary<string, string> Normalize(IDictionary<string, string> envVars)
    {
        ArgumentNullException.ThrowIfNull(envVars);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in envVars)
        {
            string key = entry.Key;
            string value = entry.Value;

            // Удаление префиксов ASPNETCORE_ и DOTNET_
            if (key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase))
            {
                key = key["ASPNETCORE_".Length..];
            }
            else if (key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
            {
                key = key["DOTNET_".Length..];
            }

            // Замена '__' на ':'
            key = key.Replace("__", ":", StringComparison.Ordinal);

            // Удаление повторяющихся двоеточий
            key = key.Replace("::", ":", StringComparison.Ordinal);

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Накладывает переменные окружения поверх конфигурации.
    /// </summary>
    /// <param name="config">Исходная конфигурация.</param>
    /// <param name="envVars">Переменные окружения для наложения.</param>
    /// <param name="overriddenKeys">Список ключей, которые были перекрыты (выходной параметр).</param>
    /// <returns>Новая конфигурация с применёнными переменными окружения.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="config"/> или <paramref name="envVars"/> равен <see langword="null"/>.</exception>
    public static Dictionary<string, string> Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, out List<string> overriddenKeys)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(envVars);

        // Filter and strip the custom prefix (if any)
        var prefixed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in envVars)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (!string.IsNullOrEmpty(key))
            {
                if (key.Contains("__", StringComparison.Ordinal))
                {
                    key = key.Replace("__", ":", StringComparison.Ordinal);
                }

                prefixed[key] = value;
            }
        }

        // Apply the existing normalization (ASP.NET Core prefixes and '__' handling)
        var normalized = Normalize(prefixed);

        overriddenKeys = [];
        var result = new Dictionary<string, string>(config, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in normalized)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (result.TryGetValue(key, out _))
            {
                overriddenKeys.Add(key);
            }

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Накладывает переменные окружения поверх конфигурации, используя опциональный префикс.
    /// </summary>
    public static Dictionary<string, string> Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, string? prefix, out List<string> overriddenKeys)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(envVars);

        // Filter and strip the custom prefix (if any)
        var prefixed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in envVars)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (!string.IsNullOrEmpty(prefix))
            {
                if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Skip variables that do not match the custom prefix
                    continue;
                }

                // Strip the custom prefix
                key = key[prefix.Length..];
            }

            if (key.Contains("__", StringComparison.Ordinal))
            {
                key = key.Replace("__", ":", StringComparison.Ordinal);
            }

            prefixed[key] = value;
        }

        // Apply the existing normalization (ASP.NET Core prefixes and '__' handling)
        var normalized = Normalize(prefixed);

        overriddenKeys = [];
        var result = new Dictionary<string, string>(config, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in normalized)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (result.TryGetValue(key, out _))
            {
                overriddenKeys.Add(key);
            }

            result[key] = value;
        }

        return result;
    }
}
