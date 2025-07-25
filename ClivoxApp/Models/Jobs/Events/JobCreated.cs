using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients.Events;

public record JobCreated(string Description, decimal Cost) : DomainEvent;
