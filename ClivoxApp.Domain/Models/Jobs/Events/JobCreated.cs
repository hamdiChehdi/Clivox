using ClivoxApp.EventSourcingInfrastucture;

namespace ClivoxApp.Models.Clients.Events;

public record JobCreated(string Description, decimal Cost) : DomainEvent;
