namespace ClivoxApp.Models.Shared;

public interface IAggregateRoot
{
    public Guid Id { get; set; }
    public int Version { get; set; } // Set automatically by Marten
}
