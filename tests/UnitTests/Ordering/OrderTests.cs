using OrderFlow.BuildingBlocks;
using OrderFlow.Ordering.Domain.Common;
using OrderFlow.Ordering.Domain.Orders;
using static OrderFlow.Ordering.Domain.Orders.OrderStatus;

namespace OrderFlow.UnitTests.Ordering;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly CustomerId Customer = new(Guid.NewGuid());
    private static readonly StoreId Store = new(Guid.NewGuid());

    // La especificación de la máquina de estados, escrita de forma independiente al dominio.
    private static readonly HashSet<(OrderStatus From, OrderStatus To)> ValidTransitions =
    [
        (Pending, Confirmed), (Pending, Rejected), (Pending, Cancelled),
        (Confirmed, Preparing), (Confirmed, Cancelled),
        (Preparing, ReadyForPickup),
        (ReadyForPickup, Completed),
    ];

    // ---------- Place ----------

    [Fact]
    public void Place_WithValidLines_CreatesPendingOrderWithComputedTotal()
    {
        var result = Order.Place(Customer, Store, [Line(price: 10m, qty: 2), Line(price: 5.5m, qty: 3)], Now);

        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.Equal(Pending, order.Status);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(36.5m, order.Total.Amount);
        Assert.Equal("EUR", order.Total.Currency);
        Assert.Equal(Now, order.CreatedAt);
    }

    [Fact]
    public void Place_WithValidLines_RaisesOrderPlaced()
    {
        var order = Order.Place(Customer, Store, [Line(qty: 4)], Now).Value;

        var placed = Assert.IsType<OrderPlaced>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, placed.OrderId);
        Assert.Equal(Store, placed.StoreId);
        Assert.Equal(4, Assert.Single(placed.Lines).Quantity);
    }

    [Fact]
    public void Place_WithNoLines_ReturnsNoLinesError()
    {
        var result = Order.Place(Customer, Store, [], Now);

        Assert.Equal(OrderErrors.NoLines, result.Error);
    }

    [Fact]
    public void Place_WithMaxLines_Succeeds()
    {
        var lines = Enumerable.Range(0, Order.MaxLines).Select(_ => Line()).ToList();

        Assert.True(Order.Place(Customer, Store, lines, Now).IsSuccess);
    }

    [Fact]
    public void Place_WithMoreThanMaxLines_ReturnsTooManyLinesError()
    {
        var lines = Enumerable.Range(0, Order.MaxLines + 1).Select(_ => Line()).ToList();

        Assert.Equal(OrderErrors.TooManyLines, Order.Place(Customer, Store, lines, Now).Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(21)]
    public void Place_WithQuantityOutOfRange_ReturnsInvalidQuantityError(int quantity)
    {
        var result = Order.Place(Customer, Store, [Line(qty: quantity)], Now);

        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void Place_WithQuantityAtBoundary_Succeeds(int quantity)
    {
        Assert.True(Order.Place(Customer, Store, [Line(qty: quantity)], Now).IsSuccess);
    }

    [Fact]
    public void Place_WithMixedCurrencies_ReturnsMixedCurrenciesError()
    {
        var result = Order.Place(Customer, Store, [Line(currency: "EUR"), Line(currency: "USD")], Now);

        Assert.Equal(OrderErrors.MixedCurrencies, result.Error);
    }

    // ---------- Máquina de estados ----------

    public static TheoryData<OrderStatus, OrderStatus> AllTransitions()
    {
        var data = new TheoryData<OrderStatus, OrderStatus>();
        foreach (var from in Enum.GetValues<OrderStatus>())
            foreach (var to in Enum.GetValues<OrderStatus>())
                if (from != to && to != Pending) // a Pending no se vuelve con ningún comando
                    data.Add(from, to);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllTransitions))]
    public void Transition_FollowsStateMachine(OrderStatus from, OrderStatus to)
    {
        var order = OrderIn(from);

        var result = Apply(order, to);

        if (ValidTransitions.Contains((from, to)))
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(to, order.Status);
            Assert.Contains(order.DomainEvents, e => e is OrderStatusChanged c && c.From == from && c.To == to);
        }
        else
        {
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Conflict, result.Error.Type);
            Assert.Equal(from, order.Status);
            Assert.Empty(order.DomainEvents);
        }
    }

    // ---------- Cancelación y rechazo ----------

    [Fact]
    public void Cancel_WhenConfirmed_RaisesOrderCancelledWithStockReserved()
    {
        var order = OrderIn(Confirmed);

        order.Cancel(Now);

        var cancelled = Assert.Single(order.DomainEvents.OfType<OrderCancelled>());
        Assert.True(cancelled.StockWasReserved);
    }

    [Fact]
    public void Cancel_WhenPending_RaisesOrderCancelledWithoutStockReserved()
    {
        var order = OrderIn(Pending);

        order.Cancel(Now);

        Assert.False(Assert.Single(order.DomainEvents.OfType<OrderCancelled>()).StockWasReserved);
    }

    [Fact]
    public void Reject_WhenPending_StoresReason()
    {
        var order = OrderIn(Pending);

        order.Reject("Sin stock", Now);

        Assert.Equal("Sin stock", order.RejectionReason);
    }

    // ---------- Helpers ----------

    private static NewOrderLine Line(decimal price = 10m, int qty = 1, string currency = "EUR") =>
        new(new ProductId(Guid.NewGuid()), Money.Create(price, currency).Value, qty);

    private static Order OrderIn(OrderStatus status)
    {
        var order = Order.Place(Customer, Store, [Line()], Now).Value;

        switch (status)
        {
            case Pending: break;
            case Confirmed: order.Confirm(Now); break;
            case Rejected: order.Reject("Sin stock", Now); break;
            case Cancelled: order.Cancel(Now); break;
            case Preparing: order.Confirm(Now); order.StartPreparation(Now); break;
            case ReadyForPickup: order.Confirm(Now); order.StartPreparation(Now); order.MarkReady(Now); break;
            case Completed: order.Confirm(Now); order.StartPreparation(Now); order.MarkReady(Now); order.CompletePickup(Now); break;
            default: throw new ArgumentOutOfRangeException(nameof(status));
        }

        Assert.Equal(status, order.Status);
        order.ClearDomainEvents();
        return order;
    }

    private static Result Apply(Order order, OrderStatus target) => target switch
    {
        Confirmed => order.Confirm(Now),
        Rejected => order.Reject("Sin stock", Now),
        Preparing => order.StartPreparation(Now),
        ReadyForPickup => order.MarkReady(Now),
        Completed => order.CompletePickup(Now),
        Cancelled => order.Cancel(Now),
        _ => throw new ArgumentOutOfRangeException(nameof(target)),
    };
}