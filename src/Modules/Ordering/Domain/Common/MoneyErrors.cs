using OrderFlow.BuildingBlocks;

namespace OrderFlow.Ordering.Domain.Common;

public static class MoneyErrors
{
    public static readonly Error NegativeAmount =
        Error.Validation("Money.NegativeAmount", "El importe no puede ser negativo.");

    public static readonly Error InvalidCurrency =
        Error.Validation("Money.InvalidCurrency", "La moneda debe ser un código ISO 4217 de 3 letras mayúsculas.");
}