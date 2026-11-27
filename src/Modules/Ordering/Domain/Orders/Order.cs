using OrderFlow.BuildingBlocks;
using OrderFlow.Ordering.Domain.Common;

namespace OrderFlow.Ordering.Domain.Orders;

public sealed class Order : AggregateRoot<OrderId>
{
    public const int MaxLines = 50;

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Rejected, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.ReadyForPickup],
        [OrderStatus.ReadyForPickup] = [OrderStatus.Completed],
    };

    private readonly List<OrderLine> _lines = [];

    private Order(OrderId id, CustomerId customerId, StoreId storeId, DateTimeOffset createdAt) : base(id)
    {
        CustomerId = customerId;
        StoreId = storeId;
        CreatedAt = createdAt;
        Status = OrderStatus.Pending;
    }

    public CustomerId CustomerId { get; private set; }
    public StoreId StoreId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    // El total siempre se calcula; nunca se recibe del cliente (invariante 2).
    public Money Total => _lines.Select(l => l.LineTotal).Aggregate((a, b) => a.Add(b));

    public static Result<Order> Place(
        CustomerId customerId,
        StoreId storeId,
        IReadOnlyCollection<NewOrderLine> lines,
        DateTimeOffset now)
    {
        if (lines.Count == 0)
            return OrderErrors.NoLines;

        if (lines.Count > MaxLines)
            return OrderErrors.TooManyLines;

        if (lines.Any(l => l.Quantity is < OrderLine.MinQuantity or > OrderLine.MaxQuantity))
            return OrderErrors.InvalidQuantity;

        if (lines.Select(l => l.UnitPrice.Currency).Distinct().Count() > 1)
            return OrderErrors.MixedCurrencies;

        var order = new Order(OrderId.New(), customerId, storeId, now);

        foreach (var line in lines)
            order._lines.Add(new OrderLine(line.ProductId, line.UnitPrice, line.Quantity));

        order.Raise(new OrderPlaced(
            order.Id,
            customerId,
            storeId,
            [.. lines.Select(l => new OrderPlacedLine(l.ProductId, l.Quantity))],
            now));

        return order;
    }

    public Result Confirm(DateTimeOffset now) => TransitionTo(OrderStatus.Confirmed, now);

    public Result Reject(string reason, DateTimeOffset now)
    {
        var result = TransitionTo(OrderStatus.Rejected, now);
        if (result.IsSuccess)
            RejectionReason = reason;
        return result;
    }

    public Result StartPreparation(DateTimeOffset now) => TransitionTo(OrderStatus.Preparing, now);

    public Result MarkReady(DateTimeOffset now) => TransitionTo(OrderStatus.ReadyForPickup, now);

    public Result CompletePickup(DateTimeOffset now) => TransitionTo(OrderStatus.Completed, now);

    public Result Cancel(DateTimeOffset now)
    {
        var stockWasReserved = Status == OrderStatus.Confirmed;

        var result = TransitionTo(OrderStatus.Cancelled, now);
        if (result.IsSuccess)
            Raise(new OrderCancelled(Id, stockWasReserved, now)); // Inventory liberará la reserva (saga)

        return result;
    }

    private Result TransitionTo(OrderStatus target, DateTimeOffset now)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(target))
            return OrderErrors.InvalidTransition(Status, target);

        var from = Status;
        Status = target;
        Raise(new OrderStatusChanged(Id, from, target, now));

        return Result.Success();
    }
}