using OrderFlow.BuildingBlocks;
using OrderFlow.Ordering.Domain.Common;

namespace OrderFlow.Ordering.Domain.Orders;

public sealed class OrderLine : Entity<OrderLineId>
{
    public const int MinQuantity = 1;
    public const int MaxQuantity = 20;

    internal OrderLine(ProductId productId, Money unitPrice, int quantity) : base(OrderLineId.New())
    {
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public ProductId ProductId { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);
}