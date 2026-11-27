using OrderFlow.Ordering.Domain.Common;

namespace OrderFlow.UnitTests.Ordering;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var result = Money.Create(10.5m, "EUR");

        Assert.True(result.IsSuccess);
        Assert.Equal(10.5m, result.Value.Amount);
        Assert.Equal("EUR", result.Value.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_Fails()
    {
        var result = Money.Create(-1m, "EUR");

        Assert.Equal(MoneyErrors.NegativeAmount, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("eur")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Create_WithInvalidCurrency_Fails(string currency)
    {
        var result = Money.Create(10m, currency);

        Assert.Equal(MoneyErrors.InvalidCurrency, result.Error);
    }

    [Fact]
    public void Create_RoundsToTwoDecimals()
    {
        Assert.Equal(10.13m, Money.Create(10.125m, "EUR").Value.Amount);
    }

    [Fact]
    public void Add_WithSameCurrency_SumsAmounts()
    {
        var total = Money.Create(10m, "EUR").Value.Add(Money.Create(5.5m, "EUR").Value);

        Assert.Equal(15.5m, total.Amount);
    }

    [Fact]
    public void Add_WithDifferentCurrency_Throws()
    {
        var eur = Money.Create(10m, "EUR").Value;
        var usd = Money.Create(10m, "USD").Value;

        Assert.Throws<InvalidOperationException>(() => eur.Add(usd));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(Money.Create(10m, "EUR").Value, Money.Create(10m, "EUR").Value);
    }
}