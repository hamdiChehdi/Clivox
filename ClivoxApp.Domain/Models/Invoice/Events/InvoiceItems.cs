using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Invoice;

namespace ClivoxApp.Models.Invoice.Events;

/// <summary>
/// Event fired when one or more invoice items are added to an invoice
/// </summary>
public record AddInvoiceItems(
    Guid InvoiceId,
    List<InvoiceItem> InvoiceItems
) : DomainEvent;

/// <summary>
/// Event fired when one or more invoice items are deleted from an invoice
/// </summary>
public record DeleteInvoiceItems(
    Guid InvoiceId,
    List<Guid> InvoiceItemIds
) : DomainEvent;

/// <summary>
/// Event fired when one or more invoice items are modified in an invoice
/// </summary>
public record ModifyInvoiceItems(
    Guid InvoiceId,
    List<InvoiceItem> InvoiceItems
) : DomainEvent;