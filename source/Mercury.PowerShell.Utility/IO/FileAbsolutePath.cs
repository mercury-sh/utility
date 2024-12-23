// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Mercury.PowerShell.Utility.Collections.Extensions;
using Mercury.PowerShell.Utility.IO.Flags;
using Mercury.PowerShell.Utility.Text.Extensions;

namespace Mercury.PowerShell.Utility.IO;

/// <summary>
///   Represents a file path from the file system.
/// </summary>
public sealed class FileAbsolutePath {
  public static Encoding DefaultEncoding = new UTF8Encoding(false, true);
  public static OSPlatform? DefaultLineBreakType = null;
  public static bool DefaultEofLineBreak = true;
  private readonly AbsolutePath _source;

  internal FileAbsolutePath(AbsolutePath source)
    => _source = source;

  /// <summary>
  ///   Returns the name of the file without extension.
  /// </summary>
  public string SanitizedName => Path.GetFileNameWithoutExtension(_source._fsPath);

  /// <summary>
  ///   Returns the extension of the file with dot.
  /// </summary>
  public string Extension => Path.GetExtension(_source._fsPath);

  /// <summary>
  ///   Creates (touches) the file. Similar to the UNIX command, the last-write time is updated.
  /// </summary>
  /// <param name="time">The time to set as the last write time.</param>
  /// <param name="createDirectories">Whether to create the directories if they do not exist.</param>
  /// <returns>The path itself.</returns>
  public AbsolutePath Touch(DateTime? time = null, bool createDirectories = true) {
    if (createDirectories) {
      _source.Parent?.Directory.Create();
    }

    if (!File.Exists(_source)) {
      File.WriteAllBytes(_source, []);
    }

    File.SetLastWriteTime(_source, time ?? DateTime.Now);

    return _source;
  }

  /// <summary>
  ///   Removes the file when existent.
  /// </summary>
  public void Remove() {
    if (!_source.File.Exists()) {
      return;
    }

    File.SetAttributes(_source, FileAttributes.Normal);
    File.Delete(_source);
  }

  /// <summary>
  ///   Indicates whether the file exists.
  /// </summary>
  [Pure]
  public bool Exists()
    => File.Exists(_source);

  /// <summary>
  ///   Converts the file path to a corresponding <see cref="FileInfo" />.
  /// </summary>
  [Pure]
  public FileInfo ToFileInfo()
    => new(_source);

  /// <summary>
  ///   Appends all lines to a file.
  /// </summary>
  /// <param name="lines">The lines to append.</param>
  /// <param name="encoding">The encoding to use.</param>
  public AbsolutePath AppendAllLines(IEnumerable<string> lines, Encoding? encoding = null)
    => _source.File.AppendAllLines(lines.ToArray(), encoding);

  /// <summary>
  ///   Appends all lines to a file.
  /// </summary>
  /// <param name="lines">The lines to append.</param>
  /// <param name="encoding">The encoding to use.</param>
  public AbsolutePath AppendAllLines(string[] lines, Encoding? encoding = null) {
    _source.Parent?.Directory.Create();
    File.AppendAllLines(_source, lines, encoding ?? DefaultEncoding);
    return _source;
  }

  /// <summary>
  ///   Appends the string to a file.
  /// </summary>
  /// <param name="content">The content to append.</param>
  /// <param name="encoding">The encoding to use.</param>
  public AbsolutePath AppendAllText(string content, Encoding? encoding = null) {
    _source.Parent?.Directory.Create();
    File.AppendAllText(_source, content, encoding ?? DefaultEncoding);
    return _source;
  }

  /// <summary>
  ///   Writes all lines to a file.
  /// </summary>
  /// <param name="lines">The lines to write.</param>
  /// <param name="encoding">The encoding to use.</param>
  /// <param name="platform">The platform to use.</param>
  /// <param name="eofLineBreak">Whether to add a line break at the end of the file.</param>
  public AbsolutePath WriteAllLines(IEnumerable<string> lines, Encoding? encoding = null, OSPlatform? platform = null, bool? eofLineBreak = null)
    => _source.File.WriteAllLines(lines.ToArray(), encoding, platform, eofLineBreak);

  /// <summary>
  ///   Writes all lines to a file.
  /// </summary>
  /// <param name="lines">The lines to write.</param>
  /// <param name="encoding">The encoding to use.</param>
  /// <param name="platform">The platform to use.</param>
  /// <param name="eofLineBreak">Whether to add a line break at the end of the file.</param>
  public AbsolutePath WriteAllLines(string[] lines, Encoding? encoding = null, OSPlatform? platform = null, bool? eofLineBreak = null) {
    if (eofLineBreak ?? DefaultEofLineBreak) {
      lines = lines.Concat([string.Empty]).ToArray();
    }

    return _source.File.WriteAllText(lines.JoinNewLine(platform ?? DefaultLineBreakType), encoding);
  }

  /// <summary>
  ///   Writes the string to a file.
  /// </summary>
  /// <param name="content">The content to write.</param>
  /// <param name="encoding">The encoding to use.</param>
  /// <param name="eofLineBreak">Whether to add a line break at the end of the file.</param>
  public AbsolutePath WriteAllText(string content, Encoding? encoding = null, bool? eofLineBreak = null) {
    _source.Parent?.Directory.Create();

    if (eofLineBreak ?? DefaultEofLineBreak) {
      var windowsLineBreaks = content.Contains("\r\n");
      content = content.TrimEnd('\r', '\n');
      content += windowsLineBreaks ? "\r\n" : "\n";
    }

    File.WriteAllText(_source, content, encoding ?? DefaultEncoding);
    return _source;
  }

  /// <summary>
  ///   Writes all bytes to a file.
  /// </summary>
  /// <param name="bytes">The bytes to write.</param>
  public AbsolutePath WriteAllBytes(byte[] bytes) {
    _source.Parent?.Directory.Create();
    File.WriteAllBytes(_source, bytes);
    return _source;
  }

  /// <summary>
  ///   Reads the file as single string.
  /// </summary>
  /// <param name="encoding">The encoding to use.</param>
  public string ReadAllText(Encoding? encoding = null) {
    if (!Exists()) {
      throw new FileNotFoundException($"File '{_source}' not found");
    }

    return File.ReadAllText(_source, encoding ?? Encoding.UTF8);
  }

  /// <summary>
  ///   Reads the file as a collection of strings (new-line separated).
  /// </summary>
  /// <param name="encoding">The encoding to use.</param>
  public string[] ReadAllLines(Encoding? encoding = null) {
    if (!Exists()) {
      throw new FileNotFoundException($"File '{_source}' not found");
    }

    return File.ReadAllLines(_source, encoding ?? Encoding.UTF8);
  }

  /// <summary>
  ///   Reads the file as a collection of bytes.
  /// </summary>
  public byte[] ReadAllBytes() {
    if (!Exists()) {
      throw new FileNotFoundException($"File '{_source}' not found");
    }

    return File.ReadAllBytes(_source);
  }

  /// <summary>
  ///   Updates the text of a file.
  /// </summary>
  /// <param name="update">The function to update the text.</param>
  /// <param name="encoding">The encoding to use.</param>
  public AbsolutePath UpdateText(Func<string, string> update, Encoding? encoding = null) {
    var oldText = _source.File.ReadAllText(encoding);
    var newText = update.Invoke(oldText);
    return _source.File.WriteAllText(newText, encoding);
  }

  /// <summary>
  ///   Calculates the MD5 hash of a file.
  /// </summary>
  [Pure]
  public string GetHash() {
    if (!Exists()) {
      throw new FileNotFoundException($"File '{_source}' not found");
    }

    using var md5 = MD5.Create();
    using var stream = File.OpenRead(_source);
    var hash = md5.ComputeHash(stream);

    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
  }

  /// <summary>
  ///   Finds the first parent that fulfills the condition.
  /// </summary>
  /// <param name="predicate">The condition to fulfill.</param>
  [Pure]
  public AbsolutePath? FindParent(Func<AbsolutePath, bool> predicate) {
    if (!Exists()) {
      return null;
    }

    return _source
      .Descendants(path => path.Parent ?? throw new NotSupportedException("The root directory does not have a parent"))
      .FirstOrDefault(predicate);
  }

  /// <summary>
  ///   Finds the first parent (starting with self) that fulfills the condition.
  /// </summary>
  /// <param name="predicate">The condition to fulfill.</param>
  [Pure]
  public AbsolutePath? FindParentOrSelf(Func<AbsolutePath, bool> predicate) {
    if (!Exists()) {
      return null;
    }

    return _source
      .DescendantsAndSelf(path => path.Parent ?? throw new NotSupportedException("The root directory does not have a parent"))
      .FirstOrDefault(predicate);
  }

  /// <summary>
  ///   Indicates whether the path ends with an extension.
  /// </summary>
  /// <param name="extension">The extension to check.</param>
  /// <param name="alternativeExtensions">The alternative extensions to check.</param>
  public bool HasExtension(string extension, params string[] alternativeExtensions)
    => _source.ToString().EndsWithAnyOrdinalIgnoreCase(string.Concat(extension, alternativeExtensions));

  /// <summary>
  ///   Changes the extension of the path (with or without leading period).
  /// </summary>
  /// <param name="extension">The new extension.</param>
  public AbsolutePath? WithExtension(string extension) {
    if (_source.Parent is null) {
      return null;
    }

    return _source.Parent / Path.ChangeExtension(_source.Name, extension);
  }

  /// <summary>
  ///   Moves the file to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="createDirectories">Whether to create the directories if they do not exist.</param>
  /// <returns>The new path of the file.</returns>
  public AbsolutePath Move(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, bool createDirectories = true) {
    return HandleFile(_source, target, policy, createDirectories, () => {
      target.File.Remove();
      File.Move(_source, target);
    });
  }

  /// <summary>
  ///   Copies the file to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="createDirectories">Whether to create the directories if they do not exist.</param>
  /// <returns>The new path of the file.</returns>
  public AbsolutePath Copy(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, bool createDirectories = true) {
    return HandleFile(_source, target, policy, createDirectories, () => {
      File.Copy(_source, target, true);
    });
  }

  /// <summary>
  ///   Moves the file to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="createDirectories">Whether to create the directories if they do not exist.</param>
  public AbsolutePath MoveTo(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, bool createDirectories = true)
    => _source.File.Move(target / _source.Name, policy, createDirectories);

  /// <summary>
  ///   Copies the file to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="createDirectories">Whether to create the directories if they do not exist.</param>
  public AbsolutePath CopyTo(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, bool createDirectories = true)
    => _source.File.Copy(target / _source.Name, policy, createDirectories);

  /// <summary>
  ///   Renames the file without changing the extension.
  /// </summary>
  /// <param name="name">The new name of the file.</param>
  /// <param name="policy">The policy to apply.</param>
  public AbsolutePath RenameWithoutExtension(string name, ExistsPolicy policy = ExistsPolicy.FAIL)
    => _source.File.Move((_source.Parent! / name) + _source.File.Extension, policy);

  /// <summary>
  ///   Renames the file without changing the extension.
  /// </summary>
  /// <param name="name">The function to generate the new name of the file.</param>
  /// <param name="policy">The policy to apply.</param>
  public AbsolutePath RenameWithoutExtension(Func<AbsolutePath, string> name, ExistsPolicy policy = ExistsPolicy.FAIL)
    => _source.File.RenameWithoutExtension(name.Invoke(_source), policy);

  /// <summary>
  ///   Renames the file.
  /// </summary>
  /// <param name="name">The new name of the file.</param>
  /// <param name="policy">The policy to apply.</param>
  public AbsolutePath Rename(string name, ExistsPolicy policy = ExistsPolicy.FAIL)
    => _source.File.Move(_source.Parent! / name, policy);

  /// <summary>
  ///   Renames the file.
  /// </summary>
  /// <param name="name">The function to generate the new name of the file.</param>
  /// <param name="policy">The policy to apply.</param>
  public AbsolutePath Rename(Func<AbsolutePath, string> name, ExistsPolicy policy = ExistsPolicy.FAIL)
    => _source.File.Rename(name.Invoke(_source), policy);

  private static AbsolutePath HandleFile(AbsolutePath source, AbsolutePath target, ExistsPolicy policy, bool createDirectories, Action action) {
    if (File.Exists(target) &&
        !Permitted()) {
      return source;
    }

    if (createDirectories) {
      target.Parent?.Directory.Create();
    }

    action.Invoke();
    return target;

    bool Permitted() {
      const ExistsPolicy FILE_POLICIES = ExistsPolicy.FILE_FAIL |
                                         ExistsPolicy.FILE_SKIP |
                                         ExistsPolicy.FILE_OVERWRITE |
                                         ExistsPolicy.FILE_OVERWRITE_IF_NEWER;

      return (policy & FILE_POLICIES) switch {
        ExistsPolicy.FILE_FAIL => throw new Exception($"File '{target}' already exists"),
        ExistsPolicy.FILE_SKIP => false,
        ExistsPolicy.FILE_OVERWRITE => true,
        ExistsPolicy.FILE_OVERWRITE_IF_NEWER => File.GetLastWriteTimeUtc(target) < File.GetLastWriteTimeUtc(source),
        var _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Multiple file policies set")
      };
    }
  }
}
