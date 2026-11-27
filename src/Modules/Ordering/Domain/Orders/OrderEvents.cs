using OrderFlow.BuildingBlocks;
using OrderFlow.Ordering.Domain.Common;

namespace OrderFlow.Ordering.Domain.Orders;

public sealed record OrderPlacedLine(ProductId ProductId, int Quantity);

public sealed record OrderPlaced(
    OrderId OrderId,
    CustomerId CustomerId,
    StoreId StoreId,
    IReadOnlyList<OrderPlacedLine> Lines,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record OrderStatusChanged(
    OrderId OrderId,
    OrderStatus From,
    OrderStatus To,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record OrderCancelled(
    OrderId OrderId,
    bool StockWasReserved,
    DateTimeOffset OccurredAt) : IDomainEvent;