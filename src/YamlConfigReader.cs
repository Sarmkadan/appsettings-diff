using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        /// <exception cref="FormatException">Thrown when the YAML file contains malformed constructs such as duplicate anchors, unresolvable or recursive aliases.</exception>
        public static Dictionary<string, string> ReadFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("YAML file not found", path);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            try
            {
                return Parse(reader);
            }
            catch (NotSupportedException ex)
            {
                // Re‑throw as a clearer format exception that includes the file name.
                throw new FormatException($"Malformed YAML in file '{path}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a YAML configuration string into a dictionary.
        /// Nested mappings are flattened to "Section:Key" paths and list items to "Key[index]".
        /// </summary>
        /// <param name="yamlContent">The YAML configuration string.</param>
        /// <returns>A dictionary representing the YAML configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="yamlContent"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown if the YAML uses anchors, aliases or block scalars.</exception>
        /// <exception cref="FormatException">Thrown if a non-empty line is neither a mapping nor a list item.</exception>
        public static Dictionary<string, string> Parse(string yamlContent)
        {
            if (yamlContent == null) throw new ArgumentNullException(nameof(yamlContent));
            using var reader = new StringReader(yamlContent);
            return Parse(reader);
        }

        /// <summary>
        /// Parses a YAML configuration from the specified TextReader.
        /// Nested mappings are flattened to "Section:Key" paths and list items to "Key[index]".
        /// </summary>
        /// <param name="reader">The TextReader to read the YAML configuration from.</param>
        /// <returns>A dictionary representing the YAML configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="reader"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown if the YAML uses anchors, aliases or block scalars.</exception>
        /// <exception cref="FormatException">Thrown if a non-empty line is neither a mapping nor a list item.</exception>
        public static Dictionary<string, string> Parse(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var path = new List<string>();
            int listIndex = -1;
            var keysAtEachLevel = new Stack<HashSet<string>>();
            keysAtEachLevel.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // We have the line without the line ending (because ReadLine removes it).
                // Now we process the line as in the original algorithm.

                var trimmedLine = line.TrimEnd();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.TrimStart().StartsWith('#'))
                    continue;

                int indent = GetIndentLevel(trimmedLine);
                string content = trimmedLine.TrimStart();

                if (content == "-" || content.StartsWith("- ", StringComparison.Ordinal))
                {
                    // List item: may sit at the parent key's indent or one level deeper,
                    // so keep the key at depth indent + 1 as the parent.
                    if (path.Count > indent + 1)
                        path.RemoveRange(indent + 1, path.Count - indent - 1);

                    listIndex++;
                    string parentKey = BuildKey(path);
                    string key = $"{parentKey}[{listIndex}]";
                    string value = NormalizeValue(content.Length > 1 ? content[1..].TrimStart() : string.Empty);

                    RemoveSectionEntry(result, parentKey);
                    result[key] = value;
                    listIndex = -1;
                }
                else
                {
                    // Key-value pair: drop path segments that belong to deeper or sibling branches
                    while (path.Count > indent)
                    {
                        path.RemoveAt(path.Count - 1);
                        keysAtEachLevel.Pop();
                    }

                    int colonIndex = content.IndexOf(':');
                    if (colonIndex == -1)
                        throw new FormatException($"Invalid YAML line: {line}");

                    string key = content.Substring(0, colonIndex).Trim();
                    string value = NormalizeValue(content.Substring(colonIndex + 1).Trim());

                    string fullKeyPath = BuildKey(path);
                    string newKeyPath = fullKeyPath.Length == 0 ? key : $"{fullKeyPath}:{key}";

                    // Check for duplicate key at the current level
                    if (keysAtEachLevel.Peek().Contains(key))
                    {
                        Console.Error.WriteLine($"Warning: Duplicate key '{newKeyPath}' found. Last value will be used.");
                    }
                    else
                    {
                        keysAtEachLevel.Peek().Add(key);
                    }

                    RemoveSectionEntry(result, fullKeyPath);

                    path.Add(key);
                    result[BuildKey(path)] = value;
                    listIndex = -1;
                    keysAtEachLevel.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                }
            }

            return result;
        }

        /// <summary>
        /// Strips a trailing inline comment and matching surrounding quotes from a scalar value.
        /// </summary>
        /// <param name="value">The raw scalar value.</param>
        /// <returns>The normalized scalar value.</returns>
        /// <exception cref="NotSupportedException">Thrown if the value is an anchor, alias or block scalar.</exception>
        private static string NormalizeValue(string value)
        {
            if (value.Length == 0)
                return value;

            if (value[0] is '&' or '*' or '|' or '>')
                throw new NotSupportedException("Anchors, aliases and block scalars are not supported");

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value[1..^1];
            }

            int commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
            if (commentIndex >= 0)
                value = value[..commentIndex].TrimEnd();

            return value;
        }

        /// <summary>
        /// Removes a previously recorded empty entry for a key that turned out to be a section
        /// (a parent of nested keys or list items), matching how JSON configuration is flattened.
        /// </summary>
        /// <param name="result">The dictionary containing the YAML configuration.</param>
        /// <param name="parentKey">The parent key to remove the empty entry for.</param>
        private static void RemoveSectionEntry(Dictionary<string, string> result, string parentKey)
        {
            if (parentKey.Length > 0 &&
                result.TryGetValue(parentKey, out var existing) &&
                existing.Length == 0)
            {
                result.Remove(parentKey);
            }
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
        /// Builds a full key path from an ordered list of segments (outermost first).
        /// </summary>
        /// <param name="path">The key segments, outermost first.</param>
        /// <returns>The full key path.</returns>
        private static string BuildKey(List<string> path)
        {
            return string.Join(":", path.Select(s => s.Replace(":", "\\:")));
        }
    }
}