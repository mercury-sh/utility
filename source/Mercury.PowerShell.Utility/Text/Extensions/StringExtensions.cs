// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace Mercury.PowerShell.Utility.Text.Extensions;

/// <summary>
///   Extensions methods for <see cref="string" />.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class StringExtensions {
  /// <summary>
  ///   Joins all strings with a given separator.
  /// </summary>
  [Pure]
  public static string Join(this IEnumerable<string> enumerable, string separator)
    => string.Join(separator, enumerable);

  /// <summary>
  ///   Joins all strings with a new-line.
  /// </summary>
  [Pure]
  public static string JoinNewLine(this IEnumerable<string> values, OSPlatform? osPlatform = null) {
    var newLine = !osPlatform.HasValue
      ? Environment.NewLine
      : osPlatform.Value == OSPlatform.Windows
        ? "\r\n"
        : "\n";

    return values.Join(newLine);
  }

  /// <summary>
  ///   Indicates whether a string equals another string under <see cref="StringComparison.OrdinalIgnoreCase" /> comparison.
  /// </summary>
  [Pure]
  public static bool EqualsOrdinalIgnoreCase(this string str, string other)
    => str.Equals(other, StringComparison.OrdinalIgnoreCase);

  /// <summary>
  ///   Joins all strings with a given separator.
  /// </summary>
  [Pure]
  public static string Join(this IEnumerable<string> enumerable, char separator)
    => enumerable.Join(separator.ToString());

  /// <summary>
  ///   Indicates whether a string ends with any other string under <see cref="StringComparison.OrdinalIgnoreCase" /> comparison.
  /// </summary>
  [Pure]
  public static bool EndsWithAnyOrdinalIgnoreCase(this string? str, params string[] others)
    => str.EndsWithAnyOrdinalIgnoreCase(others.AsEnumerable());

  /// <summary>
  ///   Indicates whether a string ends with any other string under <see cref="StringComparison.OrdinalIgnoreCase" /> comparison.
  /// </summary>
  [Pure]
  public static bool EndsWithAnyOrdinalIgnoreCase(this string? str, IEnumerable<string> others)
    => others.Any(str.EndsWithOrdinalIgnoreCase);

  /// <summary>
  ///   Indicates whether a string ends with another string under <see cref="StringComparison.OrdinalIgnoreCase" /> comparison.
  /// </summary>
  [Pure]
  public static bool EndsWithOrdinalIgnoreCase(this string? str, string other)
    => str != null && str.EndsWith(other, StringComparison.OrdinalIgnoreCase);
}
