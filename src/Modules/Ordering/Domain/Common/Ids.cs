namespace OrderFlow.Ordering.Domain.Common;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}

public readonly record struct OrderLineId(Guid Value)
{
    public static OrderLineId New() => new(Guid.CreateVersion7());
}

public readonly record struct CustomerId(Guid Value);

public readonly record struct StoreId(Guid Value);

public readonly record struct ProductId(Guid Value);