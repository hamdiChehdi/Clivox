using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients.Events;

public record ClientCreated(string FirstName, string LastName, Gender Genre, string Email, string PhoneNumber, string Address) : DomainEvent;
