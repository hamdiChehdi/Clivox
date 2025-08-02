using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.Models.Invoice.Events;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace ClivoxApp.Models.Invoice;

public class InvoiceProjection : SingleStreamProjection<Invoice, Guid>
{
    public InvoiceProjection()
    {
        DeleteEvent<InvoiceDeleted>();
    }

    public void Apply(InvoiceCreated @event, Invoice invoice)
    {
        invoice.Id = @event.Id;
        invoice.InvoiceNumber = @event.InvoiceNumber;
        invoice.InvoiceDate = @event.InvoiceDate;
        invoice.DueDate = @event.DueDate;
        invoice.ServiceDate = @event.ServiceDate;
        invoice.TotalAmount = @event.TotalAmount;
        invoice.ClientId = @event.ClientId;
        invoice.Items = @event.Items;
    }

    public void Apply(InvoiceUpdated @event, Invoice invoice)
    {
        invoice.InvoiceNumber = @event.InvoiceNumber;
        invoice.InvoiceDate = @event.InvoiceDate;
        invoice.DueDate = @event.DueDate;
        invoice.ServiceDate = @event.ServiceDate;
        invoice.TotalAmount = @event.TotalAmount;
        invoice.ClientId = @event.ClientId;
        invoice.Items = @event.Items;
    }

    /// <summary>
    /// Apply AddExpenseProofFiles event - adds one or more expense proof files to the invoice
    /// </summary>
    public void Apply(AddExpenseProofFiles @event, Invoice invoice)
    {
        if (invoice.ExpenseProofFiles == null)
            invoice.ExpenseProofFiles = new List<ExpenseProofFile>();

        foreach (var file in @event.ExpenseProofFiles)
        {
            // Check if file with same ID already exists to avoid duplicates
            if (!invoice.ExpenseProofFiles.Any(f => f.Id == file.Id))
            {
                invoice.ExpenseProofFiles.Add(file);
            }
        }
    }

    /// <summary>
    /// Apply DeleteExpenseProofFiles event - removes one or more expense proof files from the invoice
    /// </summary>
    public void Apply(DeleteExpenseProofFiles @event, Invoice invoice)
    {
        if (invoice.ExpenseProofFiles == null)
            return;

        foreach (var fileId in @event.ExpenseProofFileIds)
        {
            var fileToRemove = invoice.ExpenseProofFiles.FirstOrDefault(f => f.Id == fileId);
            if (fileToRemove != null)
            {
                invoice.ExpenseProofFiles.Remove(fileToRemove);
            }
        }
    }

    /// <summary>
    /// Apply ModifyExpenseProofFiles event - updates one or more expense proof files in the invoice
    /// </summary>
    public void Apply(ModifyExpenseProofFiles @event, Invoice invoice)
    {
        if (invoice.ExpenseProofFiles == null)
            invoice.ExpenseProofFiles = new List<ExpenseProofFile>();

        foreach (var updatedFile in @event.ExpenseProofFiles)
        {
            var existingFile = invoice.ExpenseProofFiles.FirstOrDefault(f => f.Id == updatedFile.Id);
            if (existingFile != null)
            {
                // Update existing file properties
                existingFile.FileName = updatedFile.FileName;
                existingFile.ContentType = updatedFile.ContentType;
                existingFile.FileSize = updatedFile.FileSize;
                existingFile.FileContent = updatedFile.FileContent;
                existingFile.Description = updatedFile.Description;
                existingFile.Amount = updatedFile.Amount;
                existingFile.UploadedAt = updatedFile.UploadedAt;
            }
            else
            {
                // File doesn't exist, add it
                invoice.ExpenseProofFiles.Add(updatedFile);
            }
        }
    }

    public override Invoice ApplyMetadata(Invoice invoice, IEvent lastEvent)
    {
        if (invoice.CreatedOn == default)
        {
            invoice.CreatedOn = lastEvent.Timestamp.UtcDateTime;
        }
        invoice.ModifiedOn = lastEvent.Timestamp.UtcDateTime;
        return invoice;
    }
}
