using FluentAssertions;
using TrackFi.Domain.Enums;
using TrackFi.Domain.ValueObjects;

namespace TrackFi.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmount_ShouldCreateMoney()
    {
        // Act
        var money = Money.Create(100.50m, Currency.USD);

        // Assert
        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowException()
    {
        // Act
        Action act = () => Money.Create(-10m, Currency.USD);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public void Zero_ShouldCreateZeroMoney()
    {
        // Act
        var money = Money.Zero(Currency.EUR);

        // Assert
        money.Amount.Should().Be(0);
        money.Currency.Should().Be(Currency.EUR);
    }

    [Fact]
    public void Add_WithSameCurrency_ShouldAddAmounts()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(50m, Currency.USD);

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Fact]
    public void Add_WithDifferentCurrency_ShouldThrowException()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(50m, Currency.EUR);

        // Act
        Action act = () => money1.Add(money2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add EUR to USD*");
    }

    [Fact]
    public void Subtract_WithSufficientAmount_ShouldSubtract()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(30m, Currency.USD);

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Subtract_ResultingInNegative_ShouldThrowException()
    {
        // Arrange
        var money1 = Money.Create(30m, Currency.USD);
        var money2 = Money.Create(100m, Currency.USD);

        // Act
        Action act = () => money1.Subtract(money2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Result would be negative*");
    }

    [Fact]
    public void Multiply_ByPositiveFactor_ShouldMultiply()
    {
        // Arrange
        var money = Money.Create(50m, Currency.USD);

        // Act
        var result = money.Multiply(2.5m);

        // Assert
        result.Amount.Should().Be(125m);
    }

    [Fact]
    public void Multiply_ByNegativeFactor_ShouldThrowException()
    {
        // Arrange
        var money = Money.Create(50m, Currency.USD);

        // Act
        Action act = () => money.Multiply(-2m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Factor cannot be negative*");
    }

    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(100m, Currency.USD);

        // Act & Assert
        money1.Equals(money2).Should().BeTrue();
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentAmount_ShouldReturnFalse()
    {
        // Arrange
        var money1 = Money.Create(100m, Currency.USD);
        var money2 = Money.Create(50m, Currency.USD);

        // Act & Assert
        money1.Equals(money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = Money.Create(1234.56m, Currency.USD);

        // Act
        var result = money.ToString();

        // Assert
        result.Should().Contain("1,234.56");
        result.Should().Contain("USD");
    }
}
