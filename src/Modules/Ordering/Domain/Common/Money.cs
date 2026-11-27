using OrderFlow.BuildingBlocks;

namespace OrderFlow.Ordering.Domain.Common;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return MoneyErrors.NegativeAmount;

        if (currency is not { Length: 3 } || !currency.All(char.IsAsciiLetterUpper))
            return MoneyErrors.InvalidCurrency;

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency);
    }

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);
}