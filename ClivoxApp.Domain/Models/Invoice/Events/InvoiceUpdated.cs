using ClivoxApp.EventSourcingInfrastucture;
using System;
using System.Collections.Generic;

namespace ClivoxApp.Models.Invoice.Events;

public record InvoiceUpdated(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    DateTime ServiceDate,
    decimal TotalAmount,
    Guid ClientId,
    List<InvoiceItem> Items
) : DomainEvent;
