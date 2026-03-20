using Shouldly;

namespace CommandFlow.UnitTests;

public class UnitTests
{
    [Fact]
    public void Unit_Value_IsSingleton()
    {
        // Arrange
        var a = Unit.Value;
        var b = Unit.Value;

        // Act & Assert
        a.ShouldBe(b);
    }

    [Fact]
    public void Unit_Equals_ReturnsTrue()
    {
        // Arrange
        var a = new Unit();
        var b = new Unit();

        // Act
        var result = a.Equals(b);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Unit_EqualsObject_ReturnsTrueForUnit()
    {
        // Arrange
        object a = new Unit();
        var b = new Unit();

        // Act
        var result = b.Equals(a);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Unit_EqualsObject_ReturnsFalseForNonUnit()
    {
        // Arrange
        var a = new Unit();

        // Act
        var result = a.Equals("not a unit");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Unit_GetHashCode_IsZero()
    {
        // Arrange
        var unit = new Unit();

        // Act
        var hash = unit.GetHashCode();

        // Assert
        hash.ShouldBe(0);
    }

    [Fact]
    public void Unit_ToString_ReturnsParens()
    {
        // Arrange
        var unit = new Unit();

        // Act
        var text = unit.ToString();

        // Assert
        text.ShouldBe("()");
    }

    [Fact]
    public void Unit_EqualityOperator_ReturnsTrue()
    {
        // Arrange & Act
        var result = Unit.Value == new Unit();

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void Unit_InequalityOperator_ReturnsFalse()
    {
        // Arrange & Act
        var result = Unit.Value != new Unit();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Unit_CompareTo_ReturnsZero()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var comparison = unit.CompareTo(new Unit());

        // Assert
        comparison.ShouldBe(0);
    }

    [Fact]
    public async Task Unit_Task_ReturnsCompletedTaskWithValue()
    {
        // Act
        var result = await Unit.Task;

        // Assert
        result.ShouldBe(Unit.Value);
    }
}
