using ClivoxApp.EventSourcingInfrastucture;
using System;

namespace ClivoxApp.Models.Invoice.Events;

public record InvoiceDeleted() : DomainEvent;
