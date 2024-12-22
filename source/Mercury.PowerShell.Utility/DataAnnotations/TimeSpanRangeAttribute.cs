// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel.DataAnnotations;

namespace Mercury.PowerShell.Utility.DataAnnotations;

/// <summary>
///   Represents a validation attribute that specifies that a data field value must be a <see cref="TimeSpan" /> within a specified range.
/// </summary>
/// <param name="minimumTicks">The minimum value of the <see cref="TimeSpan" />.</param>
/// <param name="maximumTicks">The maximum value of the <see cref="TimeSpan" />.</param>
public sealed class TimeSpanRangeAttribute(long minimumTicks, long maximumTicks) : ValidationAttribute {
  /// <inheritdoc />
  public override bool IsValid(object? value) {
    if (value is not TimeSpan timeSpan) {
      return false;
    }

    var minimumTimeSpan = new TimeSpan(minimumTicks);
    var maximumTimeSpan = new TimeSpan(maximumTicks);

    return timeSpan >= minimumTimeSpan &&
           timeSpan <= maximumTimeSpan;
  }

  /// <inheritdoc />
  public override string FormatErrorMessage(string name)
    => $"{name} must be between {minimumTicks} and {maximumTicks}.";
}
