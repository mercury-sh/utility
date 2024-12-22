// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel.DataAnnotations;

namespace Mercury.PowerShell.Utility.DataAnnotations;

/// <summary>
///   Represents a validation attribute that specifies that a data field value must be greater or equal to a specified value.
/// </summary>
/// <param name="minimum">The minimum value of the data field.</param>
public sealed class GreaterOrEqualToAttribute(int minimum) : ValidationAttribute {
  /// <inheritdoc />
  public override bool IsValid(object? value) {
    if (value is not int number) {
      return false;
    }

    return number >= minimum;
  }

  /// <inheritdoc />
  public override string FormatErrorMessage(string name)
    => $"{name} must be greater or equal to {minimum}.";
}
