// Copyright (c) 2024. All rights reserved.
#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppsettingsDiff
{
    /// <summary>
    /// Provides methods for reading YAML configuration files.
    /// </summary>
    public static class YamlConfigReader
    {
        /// <summary>
        /// Reads a YAML configuration file from the specified path.
        /// </summary>
        /// <param name="path">The path to the YAML file.</param>
        /// <returns>A dictionary representing the YAML configuration.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the YAML file is not found.</exception>
        public static Dictionary<string, string> ReadFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("YAML file not found", path);

            return Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// Parses a YAML configuration string into a dictionary.
        /// </summary>
        /// <param name="yamlContent">The YAML configuration string.</param>
        /// <returns>A dictionary representing the YAML configuration.</returns>
        public static Dictionary<string, string> Parse(string yamlContent)
        {
            var lines = yamlContent.Split('\n', StringSplitOptions.None);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<string>();
            string? currentKey = null;
            int listIndex = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd();
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    continue;

                int indent = GetIndentLevel(line);
                string content = line.TrimStart();

                // Handle anchors/multiline
                if (content.Contains("&") || content.Contains("|") || content.Contains(">"))
                    throw new NotSupportedException("Anchors and multiline strings are not supported");

                // Adjust stack based on indentation level
                while (stack.Count > indent)
                    stack.Pop();

                if (content.StartsWith('-'))
                {
                    // List item
                    listIndex++;
                    string parentKey = BuildKey(stack);
                    string key = $"{parentKey}[{listIndex}]";
                    string value = content.Substring(2).TrimStart();
                    result.Add(key, value);
                }
                else
                {
                    // Key-value pair
                    int colonIndex = content.IndexOf(':');
                    if (colonIndex == -1)
                        throw new FormatException($"Invalid YAML line: {line}");

                    string key = content.Substring(0, colonIndex).Trim();
                    string value = content.Substring(colonIndex + 1).Trim();

                    // Build full key path
                    var newStack = new Stack<string>(stack);
                    newStack.Push(key);
                    string fullKey = BuildKey(newStack);
                    
                    result[fullKey] = value;
                    stack = newStack;
                    listIndex = -1;
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates the indentation level of a YAML line.
        /// </summary>
        /// <param name="line">The YAML line.</param>
        /// <returns>The indentation level.</returns>
        private static int GetIndentLevel(string line)
        {
            int indent = 0;
            while (indent < line.Length && line[indent] == ' ')
                indent++;
            return indent / 2; // Assume 2 spaces per indent level
        }

        /// <summary>
        /// Builds a full key path from a stack of keys.
        /// </summary>
        /// <param name="stack">The stack of keys.</param>
        /// <returns>The full key path.</returns>
        private static string BuildKey(Stack<string> stack)
        {
            return string.Join(":", stack.Select(s => s.Replace(":", "\\:")));
        }
    }
}
