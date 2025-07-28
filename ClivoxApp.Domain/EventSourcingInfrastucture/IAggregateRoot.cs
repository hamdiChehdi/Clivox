namespace ClivoxApp.EventSourcingInfrastucture;

public interface IAggregateRoot
{
    public Guid Id { get; set; }
    public int Version { get; set; } // Set automatically by Marten
}
