using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients.Events;

public record ClientCreated(
    string? FirstName,
    string? LastName,
    string? CompanyName,
    bool IsCompany,
    Gender? Genre,
    string? Email,
    string PhoneNumber,
    Address Address) : DomainEvent;
