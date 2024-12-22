// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using Mercury.PowerShell.Utility.DataAnnotations;

namespace Mercury.PowerShell.Utility.UnitTesting.DataAnnotations;

[TestFixture]
[TestOf(typeof(GreaterOrEqualToAttribute))]
public class GreaterOrEqualToAttributeTest {
  [Test]
  public void AttributeUsage_IsNotValid_WhenValueIsNotInteger() {
    // Arrange
    var attribute = new GreaterOrEqualToAttribute(0);

    // Act
    var isValid = attribute.IsValid("0");

    // Assert
    Assert.That(isValid, Is.False);
  }

  [Test]
  public void AttributeUsage_IsNotValid_WhenValueIsLessThanMinimum() {
    // Arrange
    var attribute = new GreaterOrEqualToAttribute(1);

    // Act
    var isValid = attribute.IsValid(0);

    // Assert
    Assert.That(isValid, Is.False);
  }

  [Test]
  public void AttributeUsage_IsValid_WhenValueIsEqualToMinimum() {
    // Arrange
    var attribute = new GreaterOrEqualToAttribute(1);

    // Act
    var isValid = attribute.IsValid(1);

    // Assert
    Assert.That(isValid, Is.True);
  }

  [Test]
  public void AttributeUsage_IsValid_WhenValueIsGreaterThanMinimum() {
    // Arrange
    var attribute = new GreaterOrEqualToAttribute(1);

    // Act
    var isValid = attribute.IsValid(2);

    // Assert
    Assert.That(isValid, Is.True);
  }
}
