using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients;

public class Client : IAggregateRoot
{
    public Guid Id { get; set; } 
    public int Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string? FirstName { get; set; } = null!; 
    public string? LastName { get; set; } = null!;
    public string? CompanyName { get; set; } = null!; // Optional, e.g. company name
    public bool IsCompany { get; set; } = false; // Indicates if this is a company or individual client
    public Gender? Gender { get; set; } = null;
    public string? Email { get; set; } = null!; 
    public string PhoneNumber { get; set; } = null!;
    public Address Address { get; set; } = new();

    // Add computed property for full name display
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Validates that the client has the minimum required information
    /// </summary>
    /// <returns>True if client has minimum required information, false otherwise</returns>
    public bool IsValid()
    {
        // Phone number is always required
        if (string.IsNullOrWhiteSpace(PhoneNumber))
            return false;

        if (IsCompany)
        {
            // For companies, company name is required
            return !string.IsNullOrWhiteSpace(CompanyName);
        }
        else
        {
            // For individuals, first name and last name are required
            return !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
        }
    }

    /// <summary>
    /// Gets validation errors for the client
    /// </summary>
    /// <returns>List of validation error messages</returns>
    public List<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(PhoneNumber))
            errors.Add("Phone number is required.");

        if (IsCompany)
        {
            if (string.IsNullOrWhiteSpace(CompanyName))
                errors.Add("Company name is required for company clients.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                errors.Add("First name is required for individual clients.");
            
            if (string.IsNullOrWhiteSpace(LastName))
                errors.Add("Last name is required for individual clients.");
        }

        return errors;
    }

    /// <summary>
    /// Creates a deep copy of the client for editing purposes
    /// </summary>
    /// <returns>A new Client instance with the same values</returns>
    public Client DeepCopy()
    {
        return new Client
        {
            Id = this.Id,
            Version = this.Version,
            CreatedOn = this.CreatedOn,
            ModifiedOn = this.ModifiedOn,
            FirstName = this.FirstName,
            LastName = this.LastName,
            CompanyName = this.CompanyName,
            IsCompany = this.IsCompany,
            Gender = this.Gender,
            Email = this.Email,
            PhoneNumber = this.PhoneNumber,
            Address = new Address
            {
                CompanyOrPerson = this.Address.CompanyOrPerson,
                Street = this.Address.Street,
                PostalCode = this.Address.PostalCode,
                City = this.Address.City,
                Country = this.Address.Country
            }
        };
    }
}

public enum Gender { Male, Female}



