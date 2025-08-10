using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients;

/// <summary>
/// Filter criteria for client searches
/// </summary>
public class ClientFilter
{
    /// <summary>
    /// Text search query for name, company name, or email
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Filter by client type (individual or company)
    /// </summary>
    public ClientType? ClientType { get; set; }

    /// <summary>
    /// Filter by gender (for individual clients only)
    /// </summary>
    public Gender? Gender { get; set; }

    /// <summary>
    /// Filter by country
    /// </summary>
    public Countries? Country { get; set; }

    /// <summary>
    /// Filter by client creation year
    /// </summary>
    public int? CreationYear { get; set; }

    /// <summary>
    /// Filter by creation date range - start date
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by creation date range - end date
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter clients with invoices created in a specific year
    /// </summary>
    public int? InvoiceYear { get; set; }

    /// <summary>
    /// Filter clients with invoices created in a date range - start date
    /// </summary>
    public DateTime? InvoicesFrom { get; set; }

    /// <summary>
    /// Filter clients with invoices created in a date range - end date
    /// </summary>
    public DateTime? InvoicesTo { get; set; }

    /// <summary>
    /// Filter clients with minimum number of jobs/invoices
    /// </summary>
    public int? MinJobCount { get; set; }

    /// <summary>
    /// Filter clients with maximum number of jobs/invoices
    /// </summary>
    public int? MaxJobCount { get; set; }

    /// <summary>
    /// Filter clients that have jobs/invoices
    /// </summary>
    public bool? HasJobs { get; set; }

    /// <summary>
    /// City filter
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Postal code filter
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Checks if any filter is applied
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchQuery) ||
        ClientType.HasValue ||
        Gender.HasValue ||
        Country.HasValue ||
        CreationYear.HasValue ||
        CreatedFrom.HasValue ||
        CreatedTo.HasValue ||
        InvoiceYear.HasValue ||
        InvoicesFrom.HasValue ||
        InvoicesTo.HasValue ||
        MinJobCount.HasValue ||
        MaxJobCount.HasValue ||
        HasJobs.HasValue ||
        !string.IsNullOrWhiteSpace(City) ||
        !string.IsNullOrWhiteSpace(PostalCode);

    /// <summary>
    /// Resets all filters to default values
    /// </summary>
    public void Reset()
    {
        SearchQuery = null;
        ClientType = null;
        Gender = null;
        Country = null;
        CreationYear = null;
        CreatedFrom = null;
        CreatedTo = null;
        InvoiceYear = null;
        InvoicesFrom = null;
        InvoicesTo = null;
        MinJobCount = null;
        MaxJobCount = null;
        HasJobs = null;
        City = null;
        PostalCode = null;
    }

    /// <summary>
    /// Creates a deep copy of the current filter
    /// </summary>
    public ClientFilter DeepCopy()
    {
        return new ClientFilter
        {
            SearchQuery = this.SearchQuery,
            ClientType = this.ClientType,
            Gender = this.Gender,
            Country = this.Country,
            CreationYear = this.CreationYear,
            CreatedFrom = this.CreatedFrom,
            CreatedTo = this.CreatedTo,
            InvoiceYear = this.InvoiceYear,
            InvoicesFrom = this.InvoicesFrom,
            InvoicesTo = this.InvoicesTo,
            MinJobCount = this.MinJobCount,
            MaxJobCount = this.MaxJobCount,
            HasJobs = this.HasJobs,
            City = this.City,
            PostalCode = this.PostalCode
        };
    }
}

/// <summary>
/// Client type filter enum
/// </summary>
public enum ClientType
{
    Individual,
    Company
}