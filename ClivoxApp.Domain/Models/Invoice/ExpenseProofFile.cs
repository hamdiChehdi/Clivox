using System;

namespace ClivoxApp.Models.Invoice;

/// <summary>
/// Represents a file attachment for proof of expenses
/// </summary>
public class ExpenseProofFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty; // description for the file
    public decimal Amount { get; set; } = 0.0m; // Expense amount for this proof file
}