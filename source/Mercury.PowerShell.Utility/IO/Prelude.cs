// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Mercury.PowerShell.Utility.Text.Extensions;

namespace Mercury.PowerShell.Utility.IO;

/// <summary>
///   Provides a set of utilities for path manipulation.
/// </summary>
/// <remarks>
///   Heavily inspired by
///   <see href="https://github.com/nuke-build/nuke/blob/develop/source/Nuke.Utilities/IO/PathConstruction.cs">PathConstruction</see> from
///   <see href="https://nuke.build/">Nuke.Build</see>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class Prelude {
  public const char WINDOWS_SEPARATOR = '\\';
  private const char UNIX_LIKE_SEPARATOR = '/';

  private static readonly char[] _separators = [WINDOWS_SEPARATOR, UNIX_LIKE_SEPARATOR];
  private static readonly char _directorySeparator = Path.DirectorySeparatorChar;

  private static bool IsSameDirectory(string? pathPart)
    => pathPart is ['.'];

  private static bool IsUpwardsDirectory(string? pathPart)
    => pathPart is ['.', '.'];

  public static bool IsWindowsRoot(string? root)
    => root?.Length == 2 &&
       char.IsLetter(root[0]) &&
       root[1] == ':';

  public static bool IsUnixLikeRoot(string? root)
    => root is [UNIX_LIKE_SEPARATOR];

  private static string GetHeadPart(string? value, int count)
    => new((value ?? string.Empty).Take(count).ToArray());

  public static bool HasPathRoot(string? path)
    => GetPathRoot(path) is not null;

  private static bool HasUnixLikeRoot(string? path)
    => IsUnixLikeRoot(GetHeadPart(path, 1));

  public static bool HasWindowsRoot(string? path)
    => IsWindowsRoot(GetHeadPart(path, 2));

  private static string? GetPathRoot(string? path) {
    if (string.IsNullOrEmpty(path)) {
      return null;
    }

    if (HasUnixLikeRoot(path)) {
      return GetHeadPart(path, 1);
    }

    return HasWindowsRoot(path)
      ? GetHeadPart(path, 2)
      : null;
  }

  public static string NormalizePath(string? path, char? separator = null) {
    AssertSeparatorChoice(path, separator);

    path ??= string.Empty;
    separator ??= GetSeparator(path);
    var root = GetPathRoot(path);

    var tail = root == null
      ? path
      : path[root.Length..];
    var tailParts = tail.Split(_separators, StringSplitOptions.RemoveEmptyEntries).ToList();

    for (var i = 0; i < tailParts.Count;) {
      var part = tailParts[i];
      if (IsUpwardsDirectory(part)) {
        if (tailParts.Take(i).All(IsUpwardsDirectory)) {
          if (!(i > 0 || root == null)) {
            throw new NotSupportedException($"Cannot normalize '{path}' beyond path root");
          }

          i++;
          continue;
        }

        tailParts.RemoveAt(i);
        tailParts.RemoveAt(i - 1);
        i--;
        continue;
      }

      if (IsSameDirectory(part)) {
        tailParts.RemoveAt(i);
        continue;
      }

      i++;
    }

    return Combine(root, string.Join(separator.Value, tailParts.ToArray()), separator);
  }

  public static string Combine(string? left, string right, char? separator = null) {
    left = Trim(left);
    right = Trim(right);

    if (HasPathRoot(right)) {
      throw new NotSupportedException("Second path must not be rooted");
    }

    if (string.IsNullOrWhiteSpace(left)) {
      return right;
    }

    if (string.IsNullOrWhiteSpace(right)) {
      return !IsWindowsRoot(left) ? left : $@"{left}\";
    }

    AssertSeparatorChoice(left, separator);
    separator ??= GetSeparator(left);

    if (IsWindowsRoot(left)) {
      return $@"{left}\{right}";
    }

    return IsUnixLikeRoot(left)
      ? $"{left}{right}"
      : $"{left}{separator}{right}";
  }

  private static char GetSeparator(string? path) {
    var root = GetPathRoot(path);
    if (root == null) {
      return _directorySeparator;
    }

    if (IsWindowsRoot(root)) {
      return WINDOWS_SEPARATOR;
    }

    return IsUnixLikeRoot(root)
      ? UNIX_LIKE_SEPARATOR
      : _directorySeparator;
  }

  private static string Trim(string? path) {
    if (string.IsNullOrEmpty(path)) {
      return string.Empty;
    }

    return IsUnixLikeRoot(path)
      ? path
      : path.TrimEnd(_separators);
  }

  private static void AssertSeparatorChoice(string? path, char? separator) {
    if (separator == null) {
      return;
    }

    var root = GetPathRoot(path);
    if (string.IsNullOrEmpty(root)) {
      return;
    }

    if (!(!IsWindowsRoot(root) ||
          separator == WINDOWS_SEPARATOR)) {
      throw new NotSupportedException($"For Windows-rooted paths the separator must be '{WINDOWS_SEPARATOR}'");
    }

    if (!(!IsUnixLikeRoot(root) ||
          separator == UNIX_LIKE_SEPARATOR)) {
      throw new NotSupportedException($"For Unix-rooted paths the separator must be '{UNIX_LIKE_SEPARATOR}'");
    }
  }

  [Pure]
  public static string GetRelativePath(string basePath, string destinationPath) {
    basePath = NormalizePath(basePath);
    destinationPath = NormalizePath(destinationPath);

    var separator = GetSeparator(basePath);

    if (separator != GetSeparator(destinationPath)) {
      throw new NotSupportedException("Separators do not match");
    }

    if (IsWindowsRoot(basePath) ||
        Path.GetPathRoot(basePath) != Path.GetPathRoot(destinationPath)) {
      throw new NotSupportedException("Roots do not match");
    }

    var baseParts = basePath.Split([separator], StringSplitOptions.RemoveEmptyEntries);
    var destinationParts = destinationPath.Split([separator], StringSplitOptions.RemoveEmptyEntries);

    var sameParts = baseParts.Zip(destinationParts, (a, b) => new { Base = a, Destination = b })
      .TakeWhile(x => x.Base.EqualsOrdinalIgnoreCase(x.Destination)).Count();
    return Enumerable.Repeat("..", baseParts.Length - sameParts).ToList()
      .Concat(destinationParts.Skip(sameParts).ToList()).Join(separator);
  }
}
