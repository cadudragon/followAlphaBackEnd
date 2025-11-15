using FluentAssertions;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Tests.Domain.ValueObjects;

public class QuantityTests
{
    [Fact]
    public void Create_WithValidValue_ShouldCreateQuantity()
    {
        // Act
        var quantity = Quantity.Create(100.5m);

        // Assert
        quantity.Value.Should().Be(100.5m);
    }

    [Fact]
    public void Create_WithNegativeValue_ShouldThrowException()
    {
        // Act
        Action act = () => Quantity.Create(-10m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity cannot be negative*");
    }

    [Fact]
    public void Zero_ShouldCreateZeroQuantity()
    {
        // Act
        var quantity = Quantity.Zero;

        // Assert
        quantity.Value.Should().Be(0);
    }

    [Fact]
    public void Add_ShouldAddValues()
    {
        // Arrange
        var q1 = Quantity.Create(100m);
        var q2 = Quantity.Create(50m);

        // Act
        var result = q1.Add(q2);

        // Assert
        result.Value.Should().Be(150m);
    }

    [Fact]
    public void Subtract_WithSufficientValue_ShouldSubtract()
    {
        // Arrange
        var q1 = Quantity.Create(100m);
        var q2 = Quantity.Create(30m);

        // Act
        var result = q1.Subtract(q2);

        // Assert
        result.Value.Should().Be(70m);
    }

    [Fact]
    public void CompareTo_WithLargerValue_ShouldReturnNegative()
    {
        // Arrange
        var q1 = Quantity.Create(50m);
        var q2 = Quantity.Create(100m);

        // Act & Assert
        (q1 < q2).Should().BeTrue();
        (q1 <= q2).Should().BeTrue();
        (q2 > q1).Should().BeTrue();
        (q2 >= q1).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var q1 = Quantity.Create(100m);
        var q2 = Quantity.Create(100m);

        // Act & Assert
        q1.Equals(q2).Should().BeTrue();
        (q1 == q2).Should().BeTrue();
    }
}
