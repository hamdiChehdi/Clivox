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
}

public enum Gender { Male, Female}



