using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Invoice.Events;
using Marten;
using Microsoft.Extensions.Logging;
using NPOI.OpenXmlFormats.Dml;

namespace ClivoxApp.Models.Invoice;

public class InvoiceRepository
{
    private readonly ILogger _logger;
    private readonly IQuerySession _querySession;
    private readonly IDocumentStore _documentStore;

    public InvoiceRepository(IQuerySession querySession, IDocumentStore documentStore, ILogger<InvoiceRepository> logger)
    {
        _logger = logger;
        _querySession = querySession;
        _documentStore = documentStore;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching invoice with ID: {InvoiceId}", id);
        var invoice = await _querySession.LoadAsync<Invoice>(id);
        if (invoice == null)
        {
            _logger.LogWarning("Invoice with ID: {InvoiceId} not found", id);
            return null;
        }
        return invoice;
    }

    public async Task<IReadOnlyList<Invoice>> GetAllInvoicesAsync()
    {
        _logger.LogInformation("Fetching all invoices");
        var invoices = await _querySession.Query<Invoice>().OrderBy(x => x.InvoiceNumber).ToListAsync();
        if (invoices.Count == 0)
        {
            _logger.LogWarning("No invoices found");
        }
        return invoices;
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesByClientIdAsync(Guid clientId)
    {
        var invoices = await _querySession.Query<Invoice>()
                                           .Where(x => x.ClientId == clientId)
                                           .OrderBy(x => x.InvoiceNumber)
                                           .ToListAsync();
        return invoices;
    }

    public async Task AddInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));
        
        // Validate invoice before saving
        if (!invoice.IsValid())
        {
            var errors = invoice.GetValidationErrors();
            throw new ArgumentException($"Invoice validation failed: {string.Join(", ", errors)}");
        }
        
        _logger.LogInformation("Adding new invoice: {InvoiceNumber}", invoice.InvoiceNumber);
        var evt = new InvoiceCreated(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.ServiceDate,
            invoice.TotalAmount,
            invoice.ClientId,
            invoice.Items);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Invoice>(null, evt, null);
        await session.SaveChangesAsync();
    }

    public async Task DeleteInvoiceAsync(Guid id)
    {
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Invoice>(id, new InvoiceDeleted(), null);
        await session.SaveChangesAsync();
    }

    public async Task UpdateInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));
        
        // Validate invoice before saving
        if (!invoice.IsValid())
        {
            var errors = invoice.GetValidationErrors();
            throw new ArgumentException($"Invoice validation failed: {string.Join(", ", errors)}");
        }
        
        _logger.LogInformation("Updating invoice: {InvoiceNumber}", invoice.InvoiceNumber);
        var evt = new InvoiceUpdated(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.DueDate,
            invoice.ServiceDate,
            invoice.TotalAmount,
            invoice.ClientId,
            invoice.Items);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Invoice>(invoice.Id, evt, null);
        await session.SaveChangesAsync();
    }

    /// <summary>
    /// Adds one or more expense proof files to an invoice
    /// </summary>
    public async Task AddExpenseProofFilesAsync(Guid invoiceId, List<ExpenseProofFile> expenseProofFiles)
    {
        if (expenseProofFiles == null || !expenseProofFiles.Any()) 
            throw new ArgumentException("Expense proof files list cannot be null or empty", nameof(expenseProofFiles));

        _logger.LogInformation("Adding {Count} expense proof files to invoice: {InvoiceId}", expenseProofFiles.Count, invoiceId);
        
        var evt = new AddExpenseProofFiles(invoiceId, expenseProofFiles);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Invoice>(invoiceId, evt, null);
        await session.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes one or more expense proof files from an invoice
    /// </summary>
    public async Task DeleteExpenseProofFilesAsync(Guid invoiceId, List<Guid> expenseProofFileIds)
    {
        if (expenseProofFileIds == null || !expenseProofFileIds.Any()) 
            throw new ArgumentException("Expense proof file IDs list cannot be null or empty", nameof(expenseProofFileIds));

        _logger.LogInformation("Deleting {Count} expense proof files from invoice: {InvoiceId}", expenseProofFileIds.Count, invoiceId);
        
        var evt = new DeleteExpenseProofFiles(invoiceId, expenseProofFileIds);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Invoice>(invoiceId, evt, null);
        await session.SaveChangesAsync();
    }

    /// <summary>
    /// Modifies one or more expense proof files in an invoice
    /// </summary>
    public async Task ModifyExpenseProofFilesAsync(Guid invoiceId, List<ExpenseProofFile> expenseProofFiles)
    {
        if (expenseProofFiles == null || !expenseProofFiles.Any()) 
            throw new ArgumentException("Expense proof files list cannot be null or empty", nameof(expenseProofFiles));

        _logger.LogInformation("Modifying {Count} expense proof files in invoice: {InvoiceId}", expenseProofFiles.Count, invoiceId);
        
        var evt = new ModifyExpenseProofFiles(invoiceId, expenseProofFiles);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Invoice>(invoiceId, evt, null);
        await session.SaveChangesAsync();
    }
}
