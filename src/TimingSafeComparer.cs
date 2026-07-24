using System;
using System.Security.Cryptography;

namespace AppsettingsDiff;

/// <summary>
/// Provides timing-safe comparison methods to prevent timing attacks when comparing sensitive values.
/// </summary>
public static class TimingSafeComparer
{
    /// <summary>
    /// Compares two strings in a timing-safe manner.
    /// </summary>
    /// <param name="a">The first string to compare.</param>
    /// <param name="b">The second string to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the strings are equal; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method handles null values gracefully and uses constant-time comparison
    /// for non-null values to prevent timing attacks.
    /// </remarks>
    public static bool FixedTimeEquals(string? a, string? b)
    {
        if (a is null || b is null)
        {
            return a == b; // Both null = equal, one null = not equal
        }

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(a),
            System.Text.Encoding.UTF8.GetBytes(b)
        );
    }

    /// <summary>
    /// Compares two strings in a timing-safe manner with a specific string comparison.
    /// </summary>
    /// <param name="a">The first string to compare.</param>
    /// <param name="b">The second string to compare.</param>
    /// <param name="comparison">The string comparison type to use for case-insensitive comparisons.</param>
    /// <returns>
    /// <see langword="true"/> if the strings are equal; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method handles null values gracefully and uses constant-time comparison
    /// for non-null values to prevent timing attacks. The comparison type is only used
    /// for case-insensitive comparisons to ensure consistent behavior.
    /// </remarks>
    public static bool FixedTimeEquals(string? a, string? b, StringComparison comparison)
    {
        // For case-sensitive comparisons, use the standard FixedTimeEquals
        if (comparison == StringComparison.Ordinal)
        {
            return FixedTimeEquals(a, b);
        }

        // For case-insensitive comparisons, we need to normalize both strings
        // to the same case before comparing with fixed-time comparison
        if (a is null || b is null)
        {
            return string.Equals(a, b, comparison);
        }

        // Normalize both strings to lowercase for case-insensitive comparison
        // This ensures consistent timing regardless of input case
        var aNormalized = a;
        var bNormalized = b;

        if (comparison == StringComparison.OrdinalIgnoreCase)
        {
            aNormalized = a.ToLowerInvariant();
            bNormalized = b.ToLowerInvariant();
        }
        else if (comparison == StringComparison.InvariantCultureIgnoreCase)
        {
            aNormalized = a.ToLowerInvariant();
            bNormalized = b.ToLowerInvariant();
        }
        else if (comparison == StringComparison.CurrentCultureIgnoreCase)
        {
            aNormalized = a.ToLowerInvariant();
            bNormalized = b.ToLowerInvariant();
        }

        return FixedTimeEquals(aNormalized, bNormalized);
    }

    /// <summary>
    /// Compares two ReadOnlySpan&lt;byte&gt; values in a timing-safe manner.
    /// </summary>
    /// <param name="a">The first span to compare.</param>
    /// <param name="b">The second span to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the spans are equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>
    /// Compares two byte arrays in a timing-safe manner.
    /// </summary>
    /// <param name="a">The first byte array to compare.</param>
    /// <param name="b">The second byte array to compare.</param>
    /// <returns>
    /// <see langword="true"/> if the arrays are equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool FixedTimeEquals(byte[]? a, byte[]? b)
    {
        if (a is null || b is null)
        {
            return a == b; // Both null = equal, one null = not equal
        }

        if (a.Length != b.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
