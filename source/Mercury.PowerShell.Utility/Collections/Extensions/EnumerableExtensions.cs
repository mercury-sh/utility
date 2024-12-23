// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using System.Diagnostics.CodeAnalysis;

namespace Mercury.PowerShell.Utility.Collections.Extensions;

/// <summary>
///   Extensions methods for <see cref="IEnumerable{T}" />.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EnumerableExtensions {
  /// <summary>
  ///   Recursively traverses an object. The starting object is the first of the collection.
  /// </summary>
  public static IEnumerable<T> DescendantsAndSelf<T>(this T obj, Func<T, T> selector, Func<T, bool>? traverse = null) {
    yield return obj;

    foreach (var p in obj.Descendants(selector, traverse)) {
      yield return p;
    }
  }

  /// <summary>
  ///   Recursively traverses an object. The starting object is not part of the collection.
  /// </summary>
  public static IEnumerable<T> Descendants<T>(this T obj, Func<T, T> selector, Func<T, bool>? traverse = null) {
    if (traverse != null &&
        !traverse(obj)) {
      yield break;
    }

    var next = selector(obj);
    if (traverse == null &&
        Equals(next, default(T))) {
      yield break;
    }

    foreach (var nextOrDescendant in next.DescendantsAndSelf(selector, traverse)) {
      yield return nextOrDescendant;
    }
  }

  /// <summary>
  ///   Recursively traverses an object. The starting object is the first of the collection.
  /// </summary>
  public static IEnumerable<T> DescendantsAndSelf<T>(this T obj, Func<T, IEnumerable<T>> selector, Func<T, bool>? traverse = null) {
    yield return obj;

    foreach (var p in Descendants(obj, selector, traverse)) {
      yield return p;
    }
  }

  /// <summary>
  ///   Recursively traverses an object. The starting object is not part of the collection.
  /// </summary>
  public static IEnumerable<T> Descendants<T>(this T obj, Func<T, IEnumerable<T>> selector, Func<T, bool>? traverse = null)
    => selector(obj).Where(x => traverse == null || traverse(x)).SelectMany(child => child.DescendantsAndSelf(selector, traverse));

  /// <summary>
  ///   Executes an action for all elements.
  /// </summary>
  public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action) {
    foreach (var item in enumerable) {
      action(item);
    }
  }

  /// <summary>
  ///   Executes an action for all elements with corresponding index.
  /// </summary>
  public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
    => enumerable.Select((x, i) => new { x, i }).ForEach(x => action(x.x, x.i));

  /// <summary>
  ///   Filters the collection to elements that are not <c>null</c>.
  /// </summary>
  public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class
    => enumerable.Where(x => x != null).Select(x => x!);

  /// <summary>
  ///   Filters the collection to elements that don't meet the condition.
  /// </summary>
  public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> enumerable, Func<T, bool>? condition) where T : class
    => enumerable.Where(x => condition == null || !condition(x));
}
