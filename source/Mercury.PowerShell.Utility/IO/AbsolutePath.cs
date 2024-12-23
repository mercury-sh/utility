// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mercury.PowerShell.Utility.IO.Converters;
using static Mercury.PowerShell.Utility.IO.Prelude;

namespace Mercury.PowerShell.Utility.IO;

/// <summary>
///   Represents an absolute path from the file system.
/// </summary>
/// <remarks>
///   Heavily inspired by
///   <see href="https://github.com/nuke-build/nuke/blob/develop/source/Nuke.Utilities/IO/AbsolutePath.cs">AbsolutePath</see> from
///   <see href="https://nuke.build/">Nuke.Build</see>
/// </remarks>
[Serializable]
[TypeConverter(typeof(AbsolutePathTypeConverter))]
[DebuggerDisplay("Path = {ToString(),nq}")]
public sealed class AbsolutePath : IEquatable<AbsolutePath> {
  internal readonly string _fsPath;

  private AbsolutePath(string path)
    => _fsPath = NormalizePath(path);

  /// <summary>
  ///   Methods for file operations.
  /// </summary>
  public FileAbsolutePath File => new(this);

  /// <summary>
  ///   Methods for directory operations.
  /// </summary>
  public DirectoryAbsolutePath Directory => new(this);

  /// <summary>
  ///   Returns the name of the file or directory.
  /// </summary>
  public string Name => Path.GetFileName(_fsPath);

  /// <summary>
  ///   Returns the parent path (directory).
  /// </summary>
  public AbsolutePath? Parent => !IsWindowsRoot(_fsPath.TrimEnd(WINDOWS_SEPARATOR)) &&
                                 !IsUnixLikeRoot(_fsPath)
    ? Path.GetDirectoryName(this)
    : null;

  /// <inheritdoc />
  public bool Equals(AbsolutePath? other) {
    var stringComparison = HasWindowsRoot(_fsPath)
      ? StringComparison.OrdinalIgnoreCase
      : StringComparison.Ordinal;
    return string.Equals(_fsPath, other?._fsPath, stringComparison);
  }

  /// <summary>
  ///   Creates a new instance of <see cref="AbsolutePath" />.
  /// </summary>
  /// <param name="path">The path to use.</param>
  /// <returns>The new instance of <see cref="AbsolutePath" />.</returns>
  public static AbsolutePath NewPath(string path)
    => new(path);

  /// <summary>
  ///   Converts a string to a <see cref="_fsPath" />.
  /// </summary>
  /// <param name="path">The path to convert.</param>
  /// <returns>The <see cref="_fsPath" />.</returns>
  /// <exception cref="NotSupportedException">If the <paramref name="path" /> is not rooted.</exception>
  [return: NotNullIfNotNull(nameof(path))]
  public static implicit operator AbsolutePath?(string? path) {
    if (path is null) {
      return null;
    }

    if (!HasPathRoot(path)) {
      throw new NotSupportedException($"Path '{path}' must be rooted");
    }

    return new AbsolutePath(path);
  }

  /// <summary>
  ///   Converts a <see cref="_fsPath" /> to a string.
  /// </summary>
  /// <param name="path">The path to convert.</param>
  /// <returns>The string.</returns>
  [return: NotNullIfNotNull(nameof(path))]
  public static implicit operator string?(AbsolutePath? path)
    => path?.ToString();

  /// <summary>
  ///   Combines two paths.
  /// </summary>
  /// <param name="left">The left path.</param>
  /// <param name="right">The right path.</param>
  /// <returns>The combined path.</returns>
  public static AbsolutePath operator /(AbsolutePath left, string? right)
    => right is null ? left : new AbsolutePath(Combine(left, right));

  /// <summary>
  ///   Concatenates two paths.
  /// </summary>
  /// <param name="left">The left path.</param>
  /// <param name="right">The right path.</param>
  /// <returns>The concatenated path.</returns>
  public static AbsolutePath operator +(AbsolutePath left, string? right)
    => new(left.ToString() + right);

  /// <summary>
  ///   Checks if two paths are equal.
  /// </summary>
  /// <param name="a">The first path.</param>
  /// <param name="b">The second path.</param>
  /// <returns><see langword="true" /> if the paths are equal; otherwise, <see langword="false" />.</returns>
  public static bool operator ==(AbsolutePath a, AbsolutePath b)
    => EqualityComparer<AbsolutePath>.Default.Equals(a, b);

  /// <summary>
  ///   Checks if two paths are different.
  /// </summary>
  /// <param name="a">The first path.</param>
  /// <param name="b">The second path.</param>
  /// <returns><see langword="true" /> if the paths are different; otherwise, <see langword="false" />.</returns>
  public static bool operator !=(AbsolutePath a, AbsolutePath b)
    => !EqualityComparer<AbsolutePath>.Default.Equals(a, b);

  /// <inheritdoc />
  public override bool Equals(object? obj) {
    if (Equals(null, obj)) {
      return false;
    }

    if (Equals(this, obj)) {
      return true;
    }

    return obj.GetType() == GetType() &&
           Equals((AbsolutePath)obj);
  }

  /// <inheritdoc />
  public override int GetHashCode()
    => _fsPath.GetHashCode();
}
