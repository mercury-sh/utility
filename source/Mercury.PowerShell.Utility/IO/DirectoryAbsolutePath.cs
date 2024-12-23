// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using Mercury.PowerShell.Utility.Collections.Extensions;
using Mercury.PowerShell.Utility.IO.Flags;

namespace Mercury.PowerShell.Utility.IO;

/// <summary>
///   Represents a directory path from the file system.
/// </summary>
public sealed class DirectoryAbsolutePath {
  private readonly AbsolutePath _source;

  internal DirectoryAbsolutePath(AbsolutePath source)
    => _source = source;

  /// <summary>
  ///   Returns all files below the directory.
  /// </summary>
  /// <param name="pattern">The pattern to match.</param>
  /// <param name="depth">The depth to search.</param>
  /// <param name="attributes">The attributes to match.</param>
  public IEnumerable<AbsolutePath> GetFiles(string pattern = "*", int depth = 1, FileAttributes attributes = 0) {
    switch (depth) {
      case < 0:
        throw new ArgumentOutOfRangeException(nameof(depth), "The depth must be greater than or equal to zero.");
      case 0:
        return [];
      default: {
        var files = Directory.EnumerateFiles(_source, pattern, SearchOption.TopDirectoryOnly)
          .Where(x => (File.GetAttributes(x) & attributes) == attributes)
          .OrderBy(x => x)
          .Select(AbsolutePath.NewPath);

        return files.Concat(GetDirectories(depth: depth - 1)
          .SelectMany(path => new DirectoryAbsolutePath(path).GetFiles(pattern, attributes: attributes)));
      }
    }
  }

  /// <summary>
  ///   Returns all directories below the directory.
  /// </summary>
  /// <param name="pattern">The pattern to match.</param>
  /// <param name="depth">The depth to search.</param>
  /// <param name="attributes">The attributes to match.</param>
  public IEnumerable<AbsolutePath> GetDirectories(string pattern = "*", int depth = 1, FileAttributes attributes = 0) {
    if (depth < 0) {
      throw new ArgumentOutOfRangeException(nameof(depth), "The depth must be greater than or equal to zero.");
    }

    var paths = new string[] { _source };
    while (paths.Length != 0 &&
           depth > 0) {
      var matchingDirectories = paths
        .SelectMany(x => Directory.EnumerateDirectories(x, pattern, SearchOption.TopDirectoryOnly))
        .Where(value => (File.GetAttributes(value) & attributes) == attributes)
        .OrderBy(value => value)
        .Select(AbsolutePath.NewPath)
        .ToList();

      foreach (var matchingDirectory in matchingDirectories) {
        yield return matchingDirectory;
      }

      depth--;
      paths = paths.SelectMany(value => Directory.GetDirectories(value, "*", SearchOption.TopDirectoryOnly)).ToArray();
    }
  }

  /// <summary>
  ///   Creates the directory.
  /// </summary>
  /// <returns>The path itself.</returns>
  public AbsolutePath Create() {
    Directory.CreateDirectory(_source._fsPath);

    return _source;
  }

  /// <summary>
  ///   Cleans the directory and recreates it.
  /// </summary>
  /// <returns>The path itself.</returns>
  public AbsolutePath CleanAndRecreate() {
    Remove();
    Create();

    return _source;
  }

  /// <summary>
  ///   Deletes the directory recursively when existent.
  /// </summary>
  public void Remove() {
    ArgumentNullException.ThrowIfNull(_source);

    if (!_source.Directory.Exists()) {
      return;
    }

    var files = Directory.GetFiles(_source, "*", SearchOption.AllDirectories);

    foreach (var file in files) {
      File.SetAttributes(file, FileAttributes.Normal);
    }

    Directory.Delete(_source, true);
  }

  /// <summary>
  ///   Indicates whether the directory exists.
  /// </summary>
  [Pure]
  public bool Exists()
    => Directory.Exists(_source);

  /// <summary>
  ///   Indicates whether the directory contains a file (<c>*</c> as wildcard) using <see cref="SearchOption.TopDirectoryOnly" />.
  /// </summary>
  /// <param name="pattern">The pattern to match.</param>
  /// <param name="options">The search options.</param>
  [Pure]
  public bool ContainsFile(string pattern, SearchOption options = SearchOption.TopDirectoryOnly) {
    ArgumentNullException.ThrowIfNull(_source, nameof(_source));

    return Exists() &&
           _source.Directory.ToDirectoryInfo().GetFiles(pattern, options).Length != 0;
  }

  /// <summary>
  ///   Indicates whether the directory contains a directory (<c>*</c> as wildcard) using <see cref="SearchOption.TopDirectoryOnly" />.
  /// </summary>
  /// <param name="pattern">The pattern to match.</param>
  /// <param name="options">The search options.</param>
  [Pure]
  public bool ContainsDirectory(string pattern, SearchOption options = SearchOption.TopDirectoryOnly) {
    ArgumentNullException.ThrowIfNull(_source, nameof(_source));

    return Exists() &&
           _source.Directory.ToDirectoryInfo().GetDirectories(pattern, options).Length != 0;
  }

  /// <summary>
  ///   Converts the directory path to a corresponding <see cref="DirectoryInfo" />.
  /// </summary>
  [Pure]
  public DirectoryInfo ToDirectoryInfo()
    => new(_source);

  /// <summary>
  ///   Calculates the MD5 hash of a directory using <see cref="SearchOption.AllDirectories" />.
  /// </summary>
  /// <param name="includeFile">The condition to include files in the hash.</param>
  [Pure]
  public string GetDirectoryHash(Func<AbsolutePath, bool>? includeFile = null) {
    if (!Exists()) {
      throw new DirectoryNotFoundException($"Directory '{_source}' not found.");
    }

    includeFile ??= _ => true;
    var paths = Directory.GetFiles(_source, "*", SearchOption.AllDirectories)
      .Select(x => (AbsolutePath)x)
      .Where(includeFile);

    return GetFileSetHash(paths, _source);
  }

  /// <summary>
  ///   Calculates the MD5 hash for a set of files in relation to a base directory.
  /// </summary>
  /// <param name="paths">The paths to calculate the hash.</param>
  /// <param name="baseDirectory">The base directory to calculate the relative paths.</param>
  [Pure]
  public static string GetFileSetHash(IEnumerable<AbsolutePath> paths, AbsolutePath baseDirectory) {
    using var md5 = MD5.Create();

    foreach (var path in paths.Distinct().OrderBy(x => x.ToString())) {
      if (!path.File.Exists()) {
        throw new FileNotFoundException($"File '{path}' not found.");
      }

      var relativePath = Prelude.GetRelativePath(baseDirectory, path);
      var unixNormalizedPath = Prelude.NormalizePath(relativePath, '/');
      var pathBytes = Encoding.UTF8.GetBytes(unixNormalizedPath);
      md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

      var contentBytes = File.ReadAllBytes(path);
      md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
    }

    md5.TransformFinalBlock([], 0, 0);

    if (md5.Hash is null) {
      throw new InvalidOperationException("The MD5 hash could not be calculated.");
    }

    return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
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
  ///   Moves the directory to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="createDirectories">Indicates whether to create directories.</param>
  /// <param name="deleteRemainingFiles">Indicates whether to delete remaining files.</param>
  /// <returns>The target directory.</returns>
  private AbsolutePath Move(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, bool createDirectories = true,
  bool deleteRemainingFiles = false) {
    return HandleDirectory(_source, target, policy, createDirectories, () => {
      _source.Directory.GetDirectories().ForEach(path => path.Directory.Move(target / Prelude.GetRelativePath(_source, path), policy));
      _source.Directory.GetFiles().ForEach(path => path.File.Move(target / Prelude.GetRelativePath(_source, path), policy));

      if (!_source.Directory.ToDirectoryInfo().EnumerateFileSystemInfos().Any() || deleteRemainingFiles) {
        _source.Directory.Remove();
      }
    });
  }

  /// <summary>
  ///   Copies the directory to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="excludeDirectory">The condition to exclude directories.</param>
  /// <param name="excludeFile">The condition to exclude files.</param>
  /// <param name="createDirectories">Indicates whether to create directories.</param>
  /// <returns>The target directory.</returns>
  private AbsolutePath Copy(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, Func<AbsolutePath, bool>? excludeDirectory = null,
  Func<AbsolutePath, bool>? excludeFile = null, bool createDirectories = true) {
    return HandleDirectory(_source, target, policy, createDirectories, () => {
      _source.Directory.GetDirectories().WhereNot(excludeDirectory)
        .ForEach(path => path.Directory.Copy(target / Prelude.GetRelativePath(_source, path), policy, excludeDirectory, excludeFile));
      _source.Directory.GetFiles().WhereNot(excludeFile)
        .ForEach(path => path.File.Copy(target / Prelude.GetRelativePath(_source, path), policy));
    });
  }

  /// <summary>
  ///   Moves the directory to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="createDirectories">Indicates whether to create directories.</param>
  public AbsolutePath MoveTo(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, bool createDirectories = true)
    => _source.Directory.Move(target / _source.Name, policy, createDirectories);

  /// <summary>
  ///   Copies the directory to another directory.
  /// </summary>
  /// <param name="target">The target directory.</param>
  /// <param name="policy">The policy to apply.</param>
  /// <param name="excludeDirectory">The condition to exclude directories.</param>
  /// <param name="excludeFile">The condition to exclude files.</param>
  /// <param name="createDirectories">Indicates whether to create directories.</param>
  public AbsolutePath CopyTo(AbsolutePath target, ExistsPolicy policy = ExistsPolicy.FAIL, Func<AbsolutePath, bool>? excludeDirectory = null,
  Func<AbsolutePath, bool>? excludeFile = null, bool createDirectories = true)
    => _source.Directory.Copy(target / _source.Name, policy, excludeDirectory, excludeFile, createDirectories);

  /// <summary>
  ///   Renames the directory.
  /// </summary>
  /// <param name="name">The new name.</param>
  /// <param name="policy">The policy to apply.</param>
  public AbsolutePath Rename(string name, ExistsPolicy policy = ExistsPolicy.FAIL)
    => _source.Directory.Move(_source.Parent! / name, policy);

  /// <summary>
  ///   Renames the directory.
  /// </summary>
  /// <param name="name">The new name.</param>
  /// <param name="policy">The policy to apply.</param>
  public AbsolutePath Rename(Func<AbsolutePath, string> name, ExistsPolicy policy = ExistsPolicy.FAIL)
    => _source.Directory.Rename(name.Invoke(_source), policy);

  private AbsolutePath HandleDirectory(AbsolutePath source, AbsolutePath target, ExistsPolicy policy, bool createDirectories, Action action) {
    if (!Exists()) {
      throw new DirectoryNotFoundException($"Directory '{source}' not found");
    }

    if (_source.Directory.ContainsDirectory(target)) {
      throw new InvalidOperationException($"Target directory '{target}' is a subdirectory of source directory '{source}'");
    }

    if (target.Directory.Exists() ||
        (!policy.HasFlag(ExistsPolicy.DIRECTORY_MERGE) && policy.HasFlag(ExistsPolicy.DIRECTORY_FAIL))) {
      throw new InvalidOperationException("Policy disallows merging directories");
    }

    if (createDirectories) {
      target.Directory.Create();
    }

    action.Invoke();
    return target;
  }
}
