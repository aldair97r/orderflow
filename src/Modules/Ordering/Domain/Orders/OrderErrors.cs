using OrderFlow.BuildingBlocks;

namespace OrderFlow.Ordering.Domain.Orders;

public static class OrderErrors
{
    public static readonly Error NoLines =
        Error.Validation("Order.NoLines", "Un pedido debe tener al menos 1 línea.");

    public static readonly Error TooManyLines =
        Error.Validation("Order.TooManyLines", $"Un pedido no puede tener más de {Order.MaxLines} líneas.");

    public static readonly Error InvalidQuantity =
        Error.Validation("Order.InvalidQuantity",
            $"La cantidad por línea debe estar entre {OrderLine.MinQuantity} y {OrderLine.MaxQuantity}.");

    public static readonly Error MixedCurrencies =
        Error.Validation("Order.MixedCurrencies", "Todas las líneas deben usar la misma moneda.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        Error.Conflict("Order.InvalidTransition", $"No se puede pasar un pedido de {from} a {to}.");
}