using OrderFlow.Ordering.Domain.Common;

namespace OrderFlow.Ordering.Domain.Orders;

public sealed record NewOrderLine(ProductId ProductId, Money UnitPrice, int Quantity);