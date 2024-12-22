// Copyright (c) Bruno Sales <me@baliestri.dev>. Licensed under the MIT License.
// See the LICENSE file in the repository root for full license text.

using Mercury.PowerShell.Utility.DataAnnotations;

namespace Mercury.PowerShell.Utility.UnitTesting.DataAnnotations;

[TestFixture]
[TestOf(typeof(TimeSpanRangeAttribute))]
public class TimeSpanRangeAttributeTest {
  [Test]
  public void AttributeUsage_IsNotValid_WhenValueIsNotTimeSpan() {
    // Arrange
    var attribute = new TimeSpanRangeAttribute(0, 0);

    // Act
    var isValid = attribute.IsValid(0);

    // Assert
    Assert.That(isValid, Is.False);
  }

  [Test]
  public void AttributeUsage_IsNotValid_WhenValueIsBelowMinimum() {
    // Arrange
    var attribute = new TimeSpanRangeAttribute(1, 10);

    // Act
    var isValid = attribute.IsValid(TimeSpan.Zero);

    // Assert
    Assert.That(isValid, Is.False);
  }

  [Test]
  public void AttributeUsage_IsNotValid_WhenValueIsAboveMaximum() {
    // Arrange
    var attribute = new TimeSpanRangeAttribute(1, 10);

    // Act
    var isValid = attribute.IsValid(TimeSpan.FromDays(11));

    // Assert
    Assert.That(isValid, Is.False);
  }

  [Test]
  public void AttributeUsage_IsValid_WhenValueIsWithinRange() {
    // Arrange
    var attribute = new TimeSpanRangeAttribute(1, 10);

    // Act
    var isValid = attribute.IsValid(new TimeSpan(5));

    // Assert
    Assert.That(isValid, Is.True);
  }
}
