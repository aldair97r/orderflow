namespace OrderFlow.Ordering.Domain.Orders;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Rejected,
    Preparing,
    ReadyForPickup,
    Completed,
    Cancelled,
}