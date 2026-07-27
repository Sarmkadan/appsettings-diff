using System;
using System.Collections.Generic;
using System.Linq;

namespace AppsettingsDiff
{
    public static class FlatConfigExtensions
    {
        /// <summary>
        /// Gets a subset of the configuration where keys start with the given prefix.
        /// </summary>
        /// <param name="config">The configuration to filter.</param>
        /// <param name="prefix">The prefix to filter keys by.</param>
        /// <returns>A new FlatConfig containing only keys that start with the prefix.</returns>
        public static FlatConfig GetSection(this FlatConfig config, string prefix)
        {
            var section = new FlatConfig();
            foreach (var kvp in config.Values)
            {
                if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    section.Values.Add(kvp.Key, kvp.Value);
                }
            }
            return section;
        }

        /// <summary>
        /// Returns a list of keys in the configuration.
        /// </summary>
        /// <param name="config">The configuration to get keys from.</param>
        /// <returns>A list of keys in the configuration.</returns>
        public static List<string> KeysOnly(this FlatConfig config)
        {
            return config.Values.Keys.ToList();
        }

        /// <summary>
        /// Converts the configuration to a sorted dictionary.
        /// </summary>
        /// <param name="config">The configuration to convert.</param>
        /// <returns>A sorted dictionary representation of the configuration.</returns>
        public static SortedDictionary<string, string> ToSortedDictionary(this FlatConfig config)
        {
            return new SortedDictionary<string, string>(config.Values);
        }
    }
}
