using ClivoxApp.EventSourcingInfrastucture;
using System;

namespace ClivoxApp.Models.Invoice.Events;

public record InvoiceCreated(
    Guid Id,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    DateTime ServiceDate,
    decimal TotalAmount,
    Guid ClientId,
    List<InvoiceItem> Items,
    InvoiceStatus Status = InvoiceStatus.Draft,
    DateTime? PaidDate = null,
    string? PaymentNotes = null
) : DomainEvent;
