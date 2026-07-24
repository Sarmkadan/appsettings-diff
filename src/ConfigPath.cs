using System;

namespace AppsettingsDiff;

/// <summary>
/// Represents a configuration key path in a normalized format.
/// Uses colon-separated notation for nested keys (e.g., "Section:Subsection:Key").
/// </summary>
public readonly struct ConfigPath : IEquatable<ConfigPath>, IComparable<ConfigPath>, IComparable
{
    private readonly string _path;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigPath"/> struct.
    /// </summary>
    /// <param name="path">The configuration key path.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is <see langword="null"/>.</exception>
    public ConfigPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _path = path;
    }

    /// <summary>
    /// Gets the string representation of the configuration path.
    /// </summary>
    public string Path => _path ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether this path is empty or null.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(_path);

    /// <summary>
    /// Creates a <see cref="ConfigPath"/> from a string path.
    /// </summary>
    /// <param name="path">The configuration key path.</param>
    /// <returns>A new <see cref="ConfigPath"/> instance.</returns>
    public static ConfigPath FromString(string path) => new ConfigPath(path);

    /// <summary>
    /// Implicitly converts a string to a <see cref="ConfigPath"/>.
    /// </summary>
    /// <param name="path">The configuration key path.</param>
    /// <returns>A new <see cref="ConfigPath"/> instance.</returns>
    public static implicit operator ConfigPath(string path) => new ConfigPath(path);

    /// <summary>
    /// Implicitly converts a <see cref="ConfigPath"/> to a string.
    /// </summary>
    /// <param name="path">The configuration path.</param>
    /// <returns>The string representation of the path.</returns>
    public static implicit operator string(ConfigPath path) => path.Path;

    /// <summary>
    /// Returns the string representation of the configuration path.
    /// </summary>
    /// <returns>The path string.</returns>
    public override string ToString() => Path;

    /// <summary>
    /// Determines whether this instance and a specified object have the same value.
    /// </summary>
    /// <param name="obj">The object to compare to this instance.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="ConfigPath"/> and equals the value of this instance; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is ConfigPath other && Equals(other);

    /// <summary>
    /// Determines whether this instance and another instance have the same value.
    /// </summary>
    /// <param name="other">The instance to compare to this instance.</param>
    /// <returns><see langword="true"/> if the value of this instance equals the value of the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ConfigPath other) => string.Equals(_path, other._path, StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => _path?.GetHashCode() ?? 0;

    /// <summary>
    /// Compares this instance to a specified <see cref="ConfigPath"/> and returns an indication of their relative values.
    /// </summary>
    /// <param name="other">An instance to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and <paramref name="other"/>.
    /// Returns less than zero if this instance is less than <paramref name="other"/>, zero if this instance is equal to <paramref name="other"/>,
    /// and greater than zero if this instance is greater than <paramref name="other"/>.
    /// </returns>
    public int CompareTo(ConfigPath other) => string.Compare(_path, other._path, StringComparison.Ordinal);

    /// <summary>
    /// Compares this instance to a specified object and returns an indication of their relative values.
    /// </summary>
    /// <param name="obj">An object to compare, or null.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and <paramref name="obj"/>.
    /// Returns less than zero if this instance is less than <paramref name="obj"/>, zero if this instance is equal to <paramref name="obj"/>,
    /// and greater than zero if this instance is greater than <paramref name="obj"/>.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="obj"/> is not a <see cref="ConfigPath"/>.</exception>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is ConfigPath other) return CompareTo(other);
        throw new ArgumentException("Object must be of type ConfigPath", nameof(obj));
    }

    /// <summary>
    /// Determines whether two <see cref="ConfigPath"/> instances have the same value.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> have the same value; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(ConfigPath left, ConfigPath right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="ConfigPath"/> instances have different values.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> have different values; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(ConfigPath left, ConfigPath right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the left <see cref="ConfigPath"/> is less than the right <see cref="ConfigPath"/>.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <(ConfigPath left, ConfigPath right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left <see cref="ConfigPath"/> is greater than the right <see cref="ConfigPath"/>.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >(ConfigPath left, ConfigPath right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left <see cref="ConfigPath"/> is less than or equal to the right <see cref="ConfigPath"/>.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator <=(ConfigPath left, ConfigPath right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left <see cref="ConfigPath"/> is greater than or equal to the right <see cref="ConfigPath"/>.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    public static bool operator >=(ConfigPath left, ConfigPath right) => left.CompareTo(right) >= 0;
}