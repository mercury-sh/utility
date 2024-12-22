// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using Mercury.PowerShell.Utility.DataAnnotations;

namespace Mercury.PowerShell.Utility.UnitTesting.DataAnnotations;

[TestFixture]
[TestOf(typeof(ValidationManager))]
public class ValidationManagerTest {
  private sealed class TestClass {
    [GreaterOrEqualTo(1)]
    public int Value { get; set; }
  }

  [Test]
  public void ValidateOrThrow_ThrowsValidationException_WhenValidationFails() {
    // Arrange
    var instance = new TestClass { Value = 0 };

    // Act & Assert
    Assert.Throws<AggregateException>(() => ValidationManager.ValidateOrThrow(instance));
  }

  [Test]
  public void ValidateOrThrow_DoesNotThrow_WhenValidationSucceeds() {
    // Arrange
    var instance = new TestClass { Value = 1 };

    // Act & Assert
    Assert.DoesNotThrow(() => ValidationManager.ValidateOrThrow(instance));
  }
}
