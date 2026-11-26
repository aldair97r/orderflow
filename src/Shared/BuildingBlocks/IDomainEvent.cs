namespace OrderFlow.BuildingBlocks;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}