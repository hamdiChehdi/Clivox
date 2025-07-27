using ClivoxApp.Models.Clients;

namespace ClivoxApp.Models.Shared;

public class Address
{
    public string? CompanyOrPerson { get; set; } // Optional, e.g. company name
    public string Street { get; set; } = null!; // e.g. Hauptstraße 19
    public string PostalCode { get; set; } = null!; // e.g. 89257
    public string City { get; set; } = null!; // e.g. Illertissen
    public Countries Country { get; set; } = Countries.Germany; // Default to Germany

    public override string ToString() =>
        string.Join("\n", new[]
        {
            CompanyOrPerson,
            Street,
            $"{PostalCode} {City}"
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
}