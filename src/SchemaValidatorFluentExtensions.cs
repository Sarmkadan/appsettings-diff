using System;
using System.Collections.Generic;
using System.Linq;

namespace AppsettingsDiff
{
    /// <summary>
    /// Provides fluent extension methods for <see cref="SchemaValidator"/>.
    /// </summary>
    public static class SchemaValidatorFluentExtensions
    {
        /// <summary>
        /// Validates a collection of configurations against a schema.
        /// </summary>
        /// <param name="validator">The schema validator.</param>
        /// <param name="configs">A collection of configurations to validate.</param>
        /// <param name="schema">The schema to validate against.</param>
        /// <returns>A flat list of all schema violations found in all configurations.</returns>
        public static IEnumerable<SchemaViolation> ValidateMany(
            this SchemaValidator validator, 
            IEnumerable<Dictionary<string, string>> configs, 
            ConfigSchema schema)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(configs);
            ArgumentNullException.ThrowIfNull(schema);

            return configs.SelectMany(config => validator.Validate(config, schema));
        }

        /// <summary>
        /// Returns the first violation from the collection, or null if there are no violations.
        /// </summary>
        /// <param name="violations">The collection of schema violations.</param>
        /// <returns>The first violation, or null if the collection is empty.</returns>
        public static SchemaViolation? FirstErrorOrNull(this IEnumerable<SchemaViolation> violations)
        {
            ArgumentNullException.ThrowIfNull(violations);

            return violations.FirstOrDefault();
        }
    }
}
