// Copyright (c) 2024. All rights reserved.
#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppsettingsDiff
{
    public static class YamlConfigReader
    {
        public static Dictionary<string, string> ReadFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("YAML file not found", path);

            return Parse(File.ReadAllText(path));
        }

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

        private static int GetIndentLevel(string line)
        {
            int indent = 0;
            while (indent < line.Length && line[indent] == ' ')
                indent++;
            return indent / 2; // Assume 2 spaces per indent level
        }

        private static string BuildKey(Stack<string> stack)
        {
            return string.Join(":", stack.Select(s => s.Replace(":", "\\:")));
        }
    }
}
