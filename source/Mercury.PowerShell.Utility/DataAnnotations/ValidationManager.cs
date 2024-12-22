// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel.DataAnnotations;

namespace Mercury.PowerShell.Utility.DataAnnotations;

/// <summary>
///   Helper class for <see cref="ValidationAttribute" /> validation.
/// </summary>
public static class ValidationManager {
  /// <summary>
  ///   Validates the instance or throws an exception if the validation fails.
  /// </summary>
  /// <param name="instance">The instance to validate.</param>
  /// <exception cref="AggregateException">Thrown when the validation fails.</exception>
  public static void ValidateOrThrow(object instance) {
    var context = new ValidationContext(instance);
    var validationResult = new List<ValidationResult>();

    if (!Validator.TryValidateObject(instance, context, validationResult, true)) {
      throw new AggregateException(validationResult.Select(result => new ValidationException(result.ErrorMessage)));
    }
  }
}
