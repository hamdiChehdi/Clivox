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


