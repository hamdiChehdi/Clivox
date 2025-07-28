using ClivoxApp.Models.Shared;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClivoxApp.Models;

public class BusinessOwner
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? TaxNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public Address? Address { get; set; }
    public BankAccount? bankAccount { get; set; }

}

public record BankAccount (
    string? AccountHolder,
    string? IBAN,
    string? BIC,
    string? Reference
);