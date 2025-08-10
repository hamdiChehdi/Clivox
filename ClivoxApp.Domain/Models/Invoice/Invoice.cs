using ClivoxApp.EventSourcingInfrastucture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClivoxApp.Models.Invoice;

public class Invoice : IAggregateRoot
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string InvoiceNumber { get; set; } = "RN-";
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime ServiceDate { get; set; } = DateTime.UtcNow.AddDays(-7);
    public decimal TotalAmount { get; set; } = 0.0m;
    
    // New payment status fields
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime? PaidDate { get; set; }
    public string? PaymentNotes { get; set; }
    
    // Navigation properties
    public Guid ClientId { get; set; }
    // List of invoice items
    public List<InvoiceItem> Items { get; set; } = new();
    // List of expense proof files
    public List<ExpenseProofFile> ExpenseProofFiles { get; set; } = new();

    /// <summary>
    /// Calculates the total amount from all invoice items (what you charge the client)
    /// </summary>
    public decimal ItemsTotal => Items?.Sum(x => x.Total) ?? 0m;

    /// <summary>
    /// Calculates the total amount from all expense proof files (what you spent)
    /// </summary>
    public decimal ExpensesTotal => ExpenseProofFiles?.Sum(x => x.Amount) ?? 0m;

    /// <summary>
    /// Calculates the net invoice total (Items - Expenses = what client actually pays)
    /// </summary>
    public decimal NetTotal => ItemsTotal - ExpensesTotal;

    /// <summary>
    /// Gets the effective status of the invoice considering both the status field and due date
    /// </summary>
    public InvoiceStatus EffectiveStatus
    {
        get
        {
            // If explicitly marked as paid, return paid
            if (Status == InvoiceStatus.Paid)
                return InvoiceStatus.Paid;
            
            // If cancelled, return cancelled
            if (Status == InvoiceStatus.Cancelled)
                return InvoiceStatus.Cancelled;
            
            // If draft, return draft
            if (Status == InvoiceStatus.Draft)
                return InvoiceStatus.Draft;
            
            // For sent invoices, check if overdue
            if (Status == InvoiceStatus.Sent && DueDate < DateTime.Now.Date)
                return InvoiceStatus.Overdue;
            
            // Otherwise return the current status
            return Status;
        }
    }

    /// <summary>
    /// Checks if the invoice is overdue (past due date and not paid)
    /// </summary>
    public bool IsOverdue => EffectiveStatus == InvoiceStatus.Overdue;

    /// <summary>
    /// Checks if the invoice is due soon (within 7 days and not paid)
    /// </summary>
    public bool IsDueSoon
    {
        get
        {
            if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.Cancelled)
                return false;
            
            var daysUntilDue = (DueDate - DateTime.Now.Date).TotalDays;
            return daysUntilDue <= 7 && daysUntilDue >= 0;
        }
    }

    /// <summary>
    /// Marks the invoice as paid
    /// </summary>
    public void MarkAsPaid(DateTime? paidDate = null, string? paymentNotes = null)
    {
        Status = InvoiceStatus.Paid;
        PaidDate = paidDate ?? DateTime.Now;
        PaymentNotes = paymentNotes;
    }

    /// <summary>
    /// Changes the invoice status
    /// </summary>
    public void ChangeStatus(InvoiceStatus newStatus, string? notes = null)
    {
        Status = newStatus;
        
        if (newStatus == InvoiceStatus.Paid && !PaidDate.HasValue)
        {
            PaidDate = DateTime.Now;
        }
        else if (newStatus != InvoiceStatus.Paid)
        {
            PaidDate = null;
        }
        
        if (!string.IsNullOrEmpty(notes))
        {
            PaymentNotes = notes;
        }
    }

    /// <summary>
    /// Validates that the invoice has the minimum required information
    /// </summary>
    /// <returns>True if invoice is valid, false otherwise</returns>
    public bool IsValid()
    {
        return GetValidationErrors().Count == 0;
    }

    /// <summary>
    /// Gets validation errors for the invoice
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        // Check if invoice has at least one item
        if (Items == null || Items.Count == 0)
        {
            errors.Add("Invoice must have at least one item.");
        }

        // Check if invoice number is provided
        if (string.IsNullOrWhiteSpace(InvoiceNumber) || InvoiceNumber == "RN-")
        {
            errors.Add("Invoice number is required.");
        }

        // Check if client ID is valid
        if (ClientId == Guid.Empty)
        {
            errors.Add("Client is required.");
        }

        // Check if any invoice items have invalid data
        if (Items != null)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var itemErrors = ValidateInvoiceItem(item, i + 1);
                errors.AddRange(itemErrors);
            }
        }

        return errors;
    }

    /// <summary>
    /// Validates a single invoice item
    /// </summary>
    /// <param name="item">The invoice item to validate</param>
    /// <param name="itemNumber">The item number for error reporting</param>
    /// <returns>List of validation errors for the item</returns>
    private List<string> ValidateInvoiceItem(InvoiceItem item, int itemNumber)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            errors.Add($"Item {itemNumber}: Description is required.");
        }

        switch (item.BillingType)
        {
            case BillingType.PerHour:
            case BillingType.PerObject:
                if (item.Quantity <= 0)
                {
                    errors.Add($"Item {itemNumber}: Quantity must be greater than 0.");
                }
                if (item.UnitPrice <= 0)
                {
                    errors.Add($"Item {itemNumber}: Unit price must be greater than 0.");
                }
                break;

            case BillingType.PerSquareMeter:
                if (item.Area <= 0)
                {
                    errors.Add($"Item {itemNumber}: Area must be greater than 0.");
                }
                if (item.PricePerSquareMeter <= 0)
                {
                    errors.Add($"Item {itemNumber}: Price per square meter must be greater than 0.");
                }
                break;

            case BillingType.FixedPrice:
                if (item.FixedAmount <= 0)
                {
                    errors.Add($"Item {itemNumber}: Fixed amount must be greater than 0.");
                }
                break;
        }

        return errors;
    }

    /// <summary>
    /// Creates a deep copy of the invoice for comparison purposes
    /// </summary>
    public Invoice DeepCopy()
    {
        var copy = new Invoice
        {
            Id = this.Id,
            Version = this.Version,
            CreatedOn = this.CreatedOn,
            ModifiedOn = this.ModifiedOn,
            InvoiceNumber = this.InvoiceNumber,
            InvoiceDate = this.InvoiceDate,
            DueDate = this.DueDate,
            ServiceDate = this.ServiceDate,
            TotalAmount = this.TotalAmount,
            Status = this.Status,
            PaidDate = this.PaidDate,
            PaymentNotes = this.PaymentNotes,
            ClientId = this.ClientId,
            Items = new List<InvoiceItem>(),
            ExpenseProofFiles = new List<ExpenseProofFile>()
        };

        // Deep copy items
        foreach (var item in this.Items)
        {
            copy.Items.Add(new InvoiceItem
            {
                Id = item.Id,
                Description = item.Description,
                BillingType = item.BillingType,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Area = item.Area,
                PricePerSquareMeter = item.PricePerSquareMeter,
                FixedAmount = item.FixedAmount
            });
        }

        // Deep copy expense proof files
        foreach (var file in this.ExpenseProofFiles)
        {
            copy.ExpenseProofFiles.Add(new ExpenseProofFile
            {
                Id = file.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                FileContent = (byte[])file.FileContent.Clone(),
                UploadedAt = file.UploadedAt,
                Description = file.Description,
                Amount = file.Amount
            });
        }

        return copy;
    }

    /// <summary>
    /// Compares this invoice with another to detect changes
    /// </summary>
    public bool HasChangedFrom(Invoice other)
    {
        if (other == null) return true;

        return HasInvoiceDataChangedFrom(other) || HasExpenseProofFilesChangedFrom(other) || HasInvoiceItemsChangedFrom(other);
    }

    /// <summary>
    /// Compares only the invoice data (excluding expense proof files) to detect changes
    /// </summary>
    public bool HasInvoiceDataChangedFrom(Invoice other)
    {
        if (other == null) return true;

        // Compare basic properties
        if (InvoiceNumber != other.InvoiceNumber ||
            InvoiceDate != other.InvoiceDate ||
            DueDate != other.DueDate ||
            ServiceDate != other.ServiceDate ||
            TotalAmount != other.TotalAmount ||
            Status != other.Status ||
            PaidDate != other.PaidDate ||
            PaymentNotes != other.PaymentNotes ||
            ClientId != other.ClientId)
        {
            return true;
        }

        // Compare items
        if (Items.Count != other.Items.Count)
            return true;

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var otherItem = other.Items[i];

            if (item.Description != otherItem.Description ||
                item.BillingType != otherItem.BillingType ||
                item.Quantity != otherItem.Quantity ||
                item.UnitPrice != otherItem.UnitPrice ||
                item.Area != otherItem.Area ||
                item.PricePerSquareMeter != otherItem.PricePerSquareMeter ||
                item.FixedAmount != otherItem.FixedAmount)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compares only the expense proof files to detect changes
    /// </summary>
    public bool HasExpenseProofFilesChangedFrom(Invoice other)
    {
        if (other == null) return true;

        // Compare expense proof files
        if (ExpenseProofFiles.Count != other.ExpenseProofFiles.Count)
            return true;

        for (int i = 0; i < ExpenseProofFiles.Count; i++)
        {
            var file = ExpenseProofFiles[i];
            var otherFile = other.ExpenseProofFiles.FirstOrDefault(f => f.Id == file.Id);

            if (otherFile == null ||
                file.FileName != otherFile.FileName ||
                file.Description != otherFile.Description ||
                file.Amount != otherFile.Amount ||
                !file.FileContent.SequenceEqual(otherFile.FileContent))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Compares only the invoice items to detect changes
    /// </summary>
    public bool HasInvoiceItemsChangedFrom(Invoice other)
    {
        if (other == null) return true;

        if (Items.Count != other.Items.Count)
            return true;

        foreach (var item in Items)
        {
            var otherItem = other.Items.FirstOrDefault(i => i.Id == item.Id);
            if (otherItem == null ||
                item.Description != otherItem.Description ||
                item.BillingType != otherItem.BillingType ||
                item.Quantity != otherItem.Quantity ||
                item.UnitPrice != otherItem.UnitPrice ||
                item.Area != otherItem.Area ||
                item.PricePerSquareMeter != otherItem.PricePerSquareMeter ||
                item.FixedAmount != otherItem.FixedAmount)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the expense proof file changes compared to another invoice
    /// </summary>
    public ExpenseProofFileChanges GetExpenseProofFileChanges(Invoice other)
    {
        if (other == null)
        {
            return new ExpenseProofFileChanges
            {
                Added = ExpenseProofFiles.ToList(),
                Modified = new List<ExpenseProofFile>(),
                Deleted = new List<Guid>()
            };
        }

        var changes = new ExpenseProofFileChanges
        {
            Added = new List<ExpenseProofFile>(),
            Modified = new List<ExpenseProofFile>(),
            Deleted = new List<Guid>()
        };

        // Find added files (exist in current but not in other)
        foreach (var file in ExpenseProofFiles)
        {
            var otherFile = other.ExpenseProofFiles.FirstOrDefault(f => f.Id == file.Id);
            if (otherFile == null)
            {
                changes.Added.Add(file);
            }
            else
            {
                // Check if file was modified
                if (file.FileName != otherFile.FileName ||
                    file.Description != otherFile.Description ||
                    file.Amount != otherFile.Amount ||
                    !file.FileContent.SequenceEqual(otherFile.FileContent))
                {
                    changes.Modified.Add(file);
                }
            }
        }

        // Find deleted files (exist in other but not in current)
        foreach (var otherFile in other.ExpenseProofFiles)
        {
            if (!ExpenseProofFiles.Any(f => f.Id == otherFile.Id))
            {
                changes.Deleted.Add(otherFile.Id);
            }
        }

        return changes;
    }

    /// <summary>
    /// Gets the invoice item changes compared to another invoice
    /// </summary>
    public InvoiceItemChanges GetInvoiceItemChanges(Invoice other)
    {
        if (other == null)
        {
            return new InvoiceItemChanges
            {
                Added = Items.ToList(),
                Modified = new List<InvoiceItem>(),
                Deleted = new List<Guid>()
            };
        }

        var changes = new InvoiceItemChanges
        {
            Added = new List<InvoiceItem>(),
            Modified = new List<InvoiceItem>(),
            Deleted = new List<Guid>()
        };

        // Find added items (exist in current but not in other)
        foreach (var item in Items)
        {
            var otherItem = other.Items.FirstOrDefault(i => i.Id == item.Id);
            if (otherItem == null)
            {
                changes.Added.Add(item);
            }
            else
            {
                // Check if item was modified
                if (item.Description != otherItem.Description ||
                    item.BillingType != otherItem.BillingType ||
                    item.Quantity != otherItem.Quantity ||
                    item.UnitPrice != otherItem.UnitPrice ||
                    item.Area != otherItem.Area ||
                    item.PricePerSquareMeter != otherItem.PricePerSquareMeter ||
                    item.FixedAmount != otherItem.FixedAmount)
                {
                    changes.Modified.Add(item);
                }
            }
        }

        // Find deleted items (exist in other but not in current)
        foreach (var otherItem in other.Items)
        {
            if (!Items.Any(i => i.Id == otherItem.Id))
            {
                changes.Deleted.Add(otherItem.Id);
            }
        }

        return changes;
    }
}

public enum BillingType
{
    PerHour,
    PerSquareMeter,
    FixedPrice,
    PerObject
}

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue,
    Cancelled
}

public class InvoiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public BillingType BillingType { get; set; }

    // For PerHour and PerObject
    public decimal Quantity { get; set; } // hours or objects
    public decimal UnitPrice { get; set; } // price per hour or per object

    // For PerSquareMeter
    public decimal Area { get; set; } // in square meters
    public decimal PricePerSquareMeter { get; set; }

    // For FixedPrice
    public decimal FixedAmount { get; set; }

    public decimal Total
    {
        get
        {
            return BillingType switch
            {
                BillingType.PerHour => Quantity * UnitPrice,
                BillingType.PerSquareMeter => Area * PricePerSquareMeter,
                BillingType.PerObject => Quantity * UnitPrice,
                BillingType.FixedPrice => FixedAmount,
                _ => 0m
            };
        }
    }
}

/// <summary>
/// Represents the changes in invoice items between two invoice states
/// </summary>
public class InvoiceItemChanges
{
    public List<InvoiceItem> Added { get; set; } = new();
    public List<InvoiceItem> Modified { get; set; } = new();
    public List<Guid> Deleted { get; set; } = new();

    public bool HasChanges => Added.Any() || Modified.Any() || Deleted.Any();
}

/// <summary>
/// Represents the changes in expense proof files between two invoice states
/// </summary>
public class ExpenseProofFileChanges
{
    public List<ExpenseProofFile> Added { get; set; } = new();
    public List<ExpenseProofFile> Modified { get; set; } = new();
    public List<Guid> Deleted { get; set; } = new();

    public bool HasChanges => Added.Any() || Modified.Any() || Deleted.Any();
}


