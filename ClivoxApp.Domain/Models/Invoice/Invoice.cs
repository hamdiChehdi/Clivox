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
    public DateTime? InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime? ServiceDate { get; set; } // Optional, e.g. date when service was provided
    public decimal TotalAmount { get; set; } = 0.0m;
    // Navigation properties
    public Guid ClientId { get; set; }
    // List of invoice items
    public List<InvoiceItem> Items { get; set; } = new();
    // List of expense proof files
    public List<ExpenseProofFile> ExpenseProofFiles { get; set; } = new();

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
            ClientId = this.ClientId,
            Items = new List<InvoiceItem>(),
            ExpenseProofFiles = new List<ExpenseProofFile>()
        };

        // Deep copy items
        foreach (var item in this.Items)
        {
            copy.Items.Add(new InvoiceItem
            {
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

        return HasInvoiceDataChangedFrom(other) || HasExpenseProofFilesChangedFrom(other);
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
}

public enum BillingType
{
    PerHour,
    PerSquareMeter,
    FixedPrice,
    PerObject
}

public class InvoiceItem
{
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
/// Represents the changes in expense proof files between two invoice states
/// </summary>
public class ExpenseProofFileChanges
{
    public List<ExpenseProofFile> Added { get; set; } = new();
    public List<ExpenseProofFile> Modified { get; set; } = new();
    public List<Guid> Deleted { get; set; } = new();

    public bool HasChanges => Added.Any() || Modified.Any() || Deleted.Any();
}


