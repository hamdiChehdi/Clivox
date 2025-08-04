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

    /// <summary>
    /// Apply AddInvoiceItems event - adds one or more invoice items to the invoice
    /// </summary>
    public void Apply(AddInvoiceItems @event, Invoice invoice)
    {
        if (invoice.Items == null)
            invoice.Items = new List<InvoiceItem>();

        foreach (var item in @event.InvoiceItems)
        {
            // Check if item with same ID already exists to avoid duplicates
            if (!invoice.Items.Any(i => i.Id == item.Id))
            {
                invoice.Items.Add(item);
            }
        }
    }

    /// <summary>
    /// Apply DeleteInvoiceItems event - removes one or more invoice items from the invoice
    /// </summary>
    public void Apply(DeleteInvoiceItems @event, Invoice invoice)
    {
        if (invoice.Items == null)
            return;

        foreach (var itemId in @event.InvoiceItemIds)
        {
            var itemToRemove = invoice.Items.FirstOrDefault(i => i.Id == itemId);
            if (itemToRemove != null)
            {
                invoice.Items.Remove(itemToRemove);
            }
        }
    }

    /// <summary>
    /// Apply ModifyInvoiceItems event - updates one or more invoice items in the invoice
    /// </summary>
    public void Apply(ModifyInvoiceItems @event, Invoice invoice)
    {
        if (invoice.Items == null)
            invoice.Items = new List<InvoiceItem>();

        foreach (var updatedItem in @event.InvoiceItems)
        {
            var existingItem = invoice.Items.FirstOrDefault(i => i.Id == updatedItem.Id);
            if (existingItem != null)
            {
                // Update existing item properties
                existingItem.Description = updatedItem.Description;
                existingItem.BillingType = updatedItem.BillingType;
                existingItem.Quantity = updatedItem.Quantity;
                existingItem.UnitPrice = updatedItem.UnitPrice;
                existingItem.Area = updatedItem.Area;
                existingItem.PricePerSquareMeter = updatedItem.PricePerSquareMeter;
                existingItem.FixedAmount = updatedItem.FixedAmount;
            }
            else
            {
                // Item doesn't exist, add it
                invoice.Items.Add(updatedItem);
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
