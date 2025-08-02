using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Invoice;

namespace ClivoxApp.Models.Invoice.Events;

/// <summary>
/// Event fired when one or more expense proof files are added to an invoice
/// </summary>
public record AddExpenseProofFiles(
    Guid InvoiceId,
    List<ExpenseProofFile> ExpenseProofFiles
) : DomainEvent;

/// <summary>
/// Event fired when one or more expense proof files are deleted from an invoice
/// </summary>
public record DeleteExpenseProofFiles(
    Guid InvoiceId,
    List<Guid> ExpenseProofFileIds
) : DomainEvent;

/// <summary>
/// Event fired when one or more expense proof files are modified in an invoice
/// </summary>
public record ModifyExpenseProofFiles(
    Guid InvoiceId,
    List<ExpenseProofFile> ExpenseProofFiles
) : DomainEvent;
