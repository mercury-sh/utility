// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Mercury.PowerShell.Utility.IO.Converters;

[ExcludeFromCodeCoverage]
internal sealed class AbsolutePathTypeConverter : TypeConverter {
  /// <inheritdoc />
  public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

  /// <inheritdoc />
  public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    => value switch {
      string stringValue => Prelude.HasPathRoot(stringValue)
        ? (AbsolutePath?)stringValue
        : Environment.CurrentDirectory + stringValue,
      null => null,
      var _ => base.ConvertFrom(context, culture, value)
    };
}
