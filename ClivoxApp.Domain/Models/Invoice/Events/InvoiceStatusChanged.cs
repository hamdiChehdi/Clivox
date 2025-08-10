using ClivoxApp.EventSourcingInfrastucture;
using System;

namespace ClivoxApp.Models.Invoice.Events;

/// <summary>
/// Event fired when an invoice status is changed
/// </summary>
public record InvoiceStatusChanged(
    Guid InvoiceId,
    InvoiceStatus NewStatus,
    InvoiceStatus? PreviousStatus,
    DateTime? PaidDate,
    string? PaymentNotes
) : DomainEvent;