namespace ClivoxApp.EventSourcingInfrastucture;

public abstract record DomainEvent
{
    public DomainEventMetadata Metadata { get; set; } = new();

    public void InitMetadata(Guid aggregateId, string? createdBy)
    {
        Metadata.AggregateRootId = aggregateId;
        Metadata.OccuredOn = DateTime.UtcNow;
        Metadata.EventName = GetType().Name;
    }
}

public class DomainEventMetadata
{
    public Guid AggregateRootId { get; set; }
    public string EventName { get; set; } = default!;
    public DateTime OccuredOn { get; set; }
}
