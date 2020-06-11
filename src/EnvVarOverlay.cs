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
    /// Нормализует переменные окружения: заменяет '__' на ':', удаляет указанный префикс.
    /// </summary>
    /// <param name="envVars">Исходные переменные окружения.</param>
    /// <returns>Нормализованные переменные.</returns>
    public static Dictionary<string, string> Normalize(IDictionary<string, string> envVars)
    {
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
    public static Dictionary<string, string> Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, out List<string> overriddenKeys)
    {
        overriddenKeys = [];
        var result = new Dictionary<string, string>(config, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in envVars)
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
    /// Накладывает переменные окружения поверх конфигурации.
    /// </summary>
    /// <param name="config">Исходная конфигурация.</param>
    /// <param name="envVars">Переменные окружения для наложения.</param>
    /// <returns>Новая конфигурация с применёнными переменными окружения.</returns>
    public static Dictionary<string, string> Apply(Dictionary<string, string> config, IDictionary<string, string> envVars)
    {
        return Apply(config, envVars, out _);
    }
}