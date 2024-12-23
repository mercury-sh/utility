// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

namespace Mercury.PowerShell.Utility.IO.Flags;

/// <summary>
///   Flags for handling existing files and directories.
/// </summary>
[Flags]
public enum ExistsPolicy {
  DIRECTORY_FAIL = 1,
  DIRECTORY_MERGE = 2,
  FILE_FAIL = 4,
  FILE_SKIP = 8,
  FILE_OVERWRITE = 16,
  FILE_OVERWRITE_IF_NEWER = 32,

  FAIL = DIRECTORY_FAIL | FILE_FAIL,
  MERGE_AND_SKIP = DIRECTORY_MERGE | FILE_SKIP,
  MERGE_AND_OVERWRITE = DIRECTORY_MERGE | FILE_OVERWRITE,
  MERGE_AND_OVERWRITE_IF_NEWER = DIRECTORY_MERGE | FILE_OVERWRITE_IF_NEWER
}
