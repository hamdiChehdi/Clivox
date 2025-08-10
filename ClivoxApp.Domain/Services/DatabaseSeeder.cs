using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using ClivoxApp.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClivoxApp.Services;

/// <summary>
/// Service for seeding the database with fake clients and invoices for testing purposes
/// </summary>
public class DatabaseSeeder
{
    private readonly ClientRepository _clientRepository;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly Random _random;

    // Fake data arrays
    private readonly string[] _maleFirstNames = {
        "Alexander", "Andreas", "Christian", "Daniel", "David", "Florian", "Jan", "Johannes", "Julian", "Kevin",
        "Lars", "Manuel", "Marc", "Marco", "Mario", "Markus", "Martin", "Matthias", "Max", "Michael",
        "Nico", "Oliver", "Patrick", "Paul", "Peter", "Philipp", "Rafael", "Sebastian", "Stefan", "Thomas"
    };

    private readonly string[] _femaleFirstNames = {
        "Alexandra", "Andrea", "Angela", "Anna", "Beatrice", "Bianca", "Carla", "Christina", "Daniela", "Diana",
        "Elena", "Elisabeth", "Eva", "Franziska", "Julia", "Katarina", "Laura", "Lisa", "Maria", "Marina",
        "Melanie", "Michelle", "Nadine", "Nicole", "Patricia", "Sandra", "Sara", "Stephanie", "Susanne", "Vanessa"
    };

    private readonly string[] _lastNames = {
        "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann",
        "Koch", "Richter", "Klein", "Wolf", "Schröder", "Neumann", "Schwarz", "Zimmermann", "Braun", "Krüger",
        "Hartmann", "Lange", "Schmitt", "Werner", "Schmitz", "Krause", "Meier", "Lehmann", "Huber", "Mayer"
    };

    private readonly string[] _companyNames = {
        "TechSolutions GmbH", "InnovaCorp AG", "DigitalWorks GmbH", "ProServices Deutschland", "ModernTech Solutions",
        "BusinessPartner GmbH", "Enterprise Solutions", "DataFlow Systems", "SmartBusiness GmbH", "Future Technologies",
        "ConsultingPlus AG", "SystemIntegrators", "WebDevelopment Pro", "CloudServices GmbH", "AutomationWorks",
        "ProcessOptimization", "QualityAssurance AG", "SoftwareCraft GmbH", "ProjectManagement Pro", "IT-Consulting Plus"
    };

    private readonly string[] _streets = {
        "Hauptstraße", "Bahnhofstraße", "Schulstraße", "Kirchstraße", "Gartenstraße", "Friedhofstraße", "Ringstraße",
        "Bergstraße", "Dorfstraße", "Lindenstraße", "Feldstraße", "Waldstraße", "Mühlstraße", "Marktplatz", "Poststraße"
    };

    private readonly string[] _cities = {
        "Berlin", "Hamburg", "München", "Köln", "Frankfurt", "Stuttgart", "Düsseldorf", "Leipzig", "Dortmund", "Essen",
        "Bremen", "Dresden", "Hannover", "Nürnberg", "Duisburg", "Bochum", "Wuppertal", "Bielefeld", "Bonn", "Münster"
    };

    private readonly string[] _serviceDescriptions = {
        "Beratungsdienstleistungen - Analyse und Empfehlungen für die Systemoptimierung",
        "Technische Implementierung - Datenbankeinrichtung und Konfiguration mit Performance-Tuning",
        "Projektmanagement - Koordination und Überwachung der Entwicklungsaktivitäten",
        "Software-Wartung - Monatlicher Support und Fehlerbehebungen für bestehende Anwendungen",
        "Qualitätssicherung - Umfassende Tests aller Systemkomponenten",
        "Dokumentationsdienstleistungen - Erstellung von Benutzerhandbüchern und technischen Spezifikationen",
        "Schulung und Support - Endbenutzer-Schulungen und fortlaufender Support",
        "Sicherheitsaudit - Umfassende Sicherheitsüberprüfung und Schwachstellenbewertung",
        "Performance-Optimierung - Code-Review und Systemleistungsverbesserungen",
        "Datenmigration - Transfer und Validierung von Legacy-Daten-Systemen",
        "UI/UX Design - Benutzeroberflächen-Design und Benutzererfahrungsoptimierung",
        "API-Entwicklung - Erstellung und Integration von Anwendungsschnittstellen",
        "Cloud-Migration - Überführung bestehender Systeme in die Cloud-Infrastruktur",
        "DevOps-Services - Automatisierung der Bereitstellungs- und Entwicklungsprozesse",
        "Mobile App Entwicklung - Native und Cross-Platform mobile Anwendungen",
        "E-Commerce-Lösung - Online-Shop-Entwicklung mit Zahlungsintegration",
        "CRM-Implementierung - Kundenbeziehungsmanagement-System-Setup",
        "ERP-Integration - Enterprise Resource Planning System-Integration",
        "Business Intelligence - Datenanalyse und Reporting-Lösungen",
        "Cyber Security Services - IT-Sicherheitsberatung und -implementierung"
    };

    private readonly string[] _expenseDescriptions = {
        "Benzinkosten - Kundenbesuch München",
        "Hotelübernachtung - Geschäftsreise Berlin", 
        "Bahnfahrt - Meeting in Frankfurt",
        "Büromaterial - Stifte und Papier",
        "Software Lizenz - Entwicklungstools",
        "Kundenessen - Vertragsabschluss",
        "Parkgebühren - Innenstadttermin",
        "Mautgebühren - Autobahnfahrt",
        "Telefonkosten - Kundenberatung",
        "Fortbildung - Fachseminar",
        "Hardware - USB-Stick und Kabel",
        "Werkzeugkauf - Messgeräte",
        "Fahrtkosten - Öffentliche Verkehrsmittel",
        "Versicherung - Berufshaftpflicht",
        "Büromiete - Anteiliger Monatsbeitrag",
        "Internet - Mobilfunkvertrag",
        "Druckkosten - Projektdokumentation",
        "Versandkosten - Paketlieferung",
        "Konferenzgebühr - Branchenevent",
        "Arbeitskleidung - Sicherheitsschuhe"
    };

    private readonly string[] _expenseFileNames = {
        "Tankquittung_{0}.pdf",
        "Hotelrechnung_{0}.jpg", 
        "Bahnticket_{0}.png",
        "Kassenbon_{0}.jpg",
        "Rechnung_{0}.pdf",
        "Beleg_{0}.png",
        "Quittung_{0}.jpg",
        "Fahrkarte_{0}.pdf",
        "Rechnung_Software_{0}.pdf",
        "Beleg_Material_{0}.jpg"
    };

    public DatabaseSeeder(ClientRepository clientRepository, InvoiceRepository invoiceRepository, ILogger<DatabaseSeeder> logger)
    {
        _clientRepository = clientRepository;
        _invoiceRepository = invoiceRepository;
        _logger = logger;
        _random = new Random(42); // Fixed seed for reproducible results
    }

    /// <summary>
    /// Seeds the database with fake clients and invoices
    /// </summary>
    /// <param name="clientCount">Number of clients to create (default: 20)</param>
    /// <param name="invoicesPerClient">Number of invoices per client (default: 4)</param>
    /// <param name="addExpenseFiles">Whether to add expense proof files to invoices (default: true)</param>
    public async Task SeedDatabaseAsync(int clientCount = 20, int invoicesPerClient = 4, bool addExpenseFiles = true)
    {
        _logger.LogInformation("Starting database seeding with {ClientCount} clients and {InvoicesPerClient} invoices per client", 
            clientCount, invoicesPerClient);

        try
        {
            // Check if data already exists
            var existingClients = await _clientRepository.GetAllClientsAsync();
            if (existingClients.Count > 0)
            {
                _logger.LogWarning("Database already contains {Count} clients. Skipping seeding to avoid duplicates.", existingClients.Count);
                //return;
            }

            
            // Create clients
            for (int i = 0; i < clientCount; i++)
            {
                var client = GenerateRandomClient();
                await _clientRepository.AddClientAsync(client);
                _logger.LogInformation("Created client {ClientNumber}/{TotalClients}: {ClientName}", 
                    i + 1, clientCount, client.IsCompany ? client.CompanyName : client.FullName);
            }

            var clients = await _clientRepository.GetAllClientsAsync();
            // Create invoices for each client
            int totalInvoicesCreated = 0;
            foreach (var client in clients)
            {
                for (int j = 0; j < invoicesPerClient; j++)
                {
                    var invoice = GenerateRandomInvoice(client.Id, totalInvoicesCreated + 1);
                    await _invoiceRepository.AddInvoiceAsync(invoice);
                    totalInvoicesCreated++;

                    _logger.LogInformation("Created invoice {InvoiceNumber} for client {ClientName}",
                        invoice.InvoiceNumber, client.IsCompany ? client.CompanyName : client.FullName);
                }

                var invoices = await _invoiceRepository.GetInvoicesByClientIdAsync(client.Id);

                foreach (var invoice in invoices)
                {
                    // Add expense proof files to some invoices
                    if (addExpenseFiles && _random.NextDouble() < 0.7) // 60% of invoices get expense files
                    {
                        var expenseFiles = GenerateRandomExpenseFiles(invoice.Id);
                        if (expenseFiles.Count > 0)
                        {
                            await _invoiceRepository.AddExpenseProofFilesAsync(invoice.Id, expenseFiles);
                            _logger.LogInformation("Added {FileCount} expense files to invoice {InvoiceNumber}",
                                expenseFiles.Count, invoice.InvoiceNumber);
                        }
                    }
                }
            }

            _logger.LogInformation("Database seeding completed successfully! Created {ClientCount} clients and {InvoiceCount} invoices.", 
                clientCount, totalInvoicesCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database");
            throw;
        }
    }

    /// <summary>
    /// Clears all existing test data from the database
    /// </summary>
    public async Task ClearTestDataAsync()
    {
        _logger.LogInformation("Clearing all test data from database");

        try
        {
            var clients = await _clientRepository.GetAllClientsAsync();
            var invoices = await _invoiceRepository.GetAllInvoicesAsync();

            // Delete all invoices first (due to foreign key constraints)
            foreach (var invoice in invoices)
            {
                await _invoiceRepository.DeleteInvoiceAsync(invoice.Id);
            }

            // Then delete all clients
            foreach (var client in clients)
            {
                await _clientRepository.DeleteClientAsync(client.Id);
            }

            _logger.LogInformation("Successfully cleared {ClientCount} clients and {InvoiceCount} invoices", 
                clients.Count, invoices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while clearing test data");
            throw;
        }
    }

    private Client GenerateRandomClient()
    {
        var isCompany = _random.NextDouble() < 0.3; // 30% chance of being a company
        var client = new Client
        {
            CreatedOn = DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
            ModifiedOn = DateTime.UtcNow.AddDays(-_random.Next(0, 30)),
            IsCompany = isCompany,
            Address = GenerateRandomAddress()
        };

        if (isCompany)
        {
            client.CompanyName = _companyNames[_random.Next(_companyNames.Length)];
            client.Email = GenerateCompanyEmail(client.CompanyName);
        }
        else
        {
            var isFemale = _random.NextDouble() < 0.5;
            client.Gender = isFemale ? Gender.Female : Gender.Male;
            client.FirstName = isFemale ? 
                _femaleFirstNames[_random.Next(_femaleFirstNames.Length)] : 
                _maleFirstNames[_random.Next(_maleFirstNames.Length)];
            client.LastName = _lastNames[_random.Next(_lastNames.Length)];
            client.Email = GeneratePersonalEmail(client.FirstName, client.LastName);
        }

        client.PhoneNumber = GenerateGermanPhoneNumber();
        
        return client;
    }

    private Invoice GenerateRandomInvoice(Guid clientId, int invoiceNumber)
    {
        var baseDate = DateTime.UtcNow.AddDays(-_random.Next(0, 180));
        var invoice = new Invoice
        {
            ClientId = clientId,
            InvoiceNumber = $"RN-{invoiceNumber:D4}",
            InvoiceDate = baseDate,
            DueDate = baseDate.AddDays(14 + _random.Next(0, 16)), // 14-30 days
            ServiceDate = baseDate.AddDays(-_random.Next(1, 30)),
            CreatedOn = baseDate.AddMinutes(_random.Next(0, 60)),
            ModifiedOn = baseDate.AddDays(_random.Next(0, 5)),
            Items = GenerateRandomInvoiceItems()
        };

        return invoice;
    }

    private List<InvoiceItem> GenerateRandomInvoiceItems()
    {
        var items = new List<InvoiceItem>();
        var itemCount = _random.Next(1, 6); // 1-5 items per invoice

        for (int i = 0; i < itemCount; i++)
        {
            var billingType = (BillingType)_random.Next(0, 4);
            var item = new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = _serviceDescriptions[_random.Next(_serviceDescriptions.Length)],
                BillingType = billingType
            };

            switch (billingType)
            {
                case BillingType.PerHour:
                    item.Quantity = _random.Next(5, 50);
                    item.UnitPrice = (decimal)(_random.NextDouble() * 100 + 50); // 50-150 per hour
                    break;

                case BillingType.PerObject:
                    item.Quantity = _random.Next(1, 20);
                    item.UnitPrice = (decimal)(_random.NextDouble() * 200 + 100); // 100-300 per object
                    break;

                case BillingType.PerSquareMeter:
                    item.Area = _random.Next(10, 200);
                    item.PricePerSquareMeter = (decimal)(_random.NextDouble() * 15 + 5); // 5-20 per m²
                    break;

                case BillingType.FixedPrice:
                    item.FixedAmount = (decimal)(_random.NextDouble() * 2000 + 500); // 500-2500 fixed
                    break;
            }

            items.Add(item);
        }

        return items;
    }

    private List<ExpenseProofFile> GenerateRandomExpenseFiles(Guid invoiceId)
    {
        var files = new List<ExpenseProofFile>();
        var fileCount = _random.Next(1, 4); // 1-3 files per invoice

        for (int i = 0; i < fileCount; i++)
        {
            var file = new ExpenseProofFile
            {
                Id = Guid.NewGuid(),
                Description = _expenseDescriptions[_random.Next(_expenseDescriptions.Length)],
                Amount = (decimal)(_random.NextDouble() * 500 + 10), // 10-510 EUR expense amounts
                UploadedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 30)), // Uploaded within last 30 days
                FileName = string.Format(_expenseFileNames[_random.Next(_expenseFileNames.Length)], 
                    DateTime.Now.ToString("yyyyMMdd") + "_" + _random.Next(1000, 9999)),
                ContentType = GetRandomContentType(),
                FileContent = GenerateRandomFileContent(),
            };

            file.FileSize = file.FileContent.Length;
            files.Add(file);
        }

        return files;
    }

    private string GetRandomContentType()
    {
        var contentTypes = new[]
        {
            "application/pdf",
            "image/jpeg", 
            "image/png",
            "image/jpg"
        };
        
        return contentTypes[_random.Next(contentTypes.Length)];
    }

    private byte[] GenerateRandomFileContent()
    {
        // Generate fake file content for different file types
        var contentType = _random.Next(0, 4);
        
        switch (contentType)
        {
            case 0: // PDF
                return GenerateFakePdfContent();
            case 1: // JPEG
                return GenerateFakeImageContent("JPEG");
            case 2: // PNG  
                return GenerateFakeImageContent("PNG");
            default: // JPG
                return GenerateFakeImageContent("JPG");
        }
    }

    private byte[] GenerateFakePdfContent()
    {
        // Create a minimal PDF structure (this is just for testing - real PDFs would be much more complex)
        var pdfContent = "%PDF-1.4\n1 0 obj\n<<\n/Type /Catalog\n/Pages 2 0 R\n>>\nendobj\n" +
                        "2 0 obj\n<<\n/Type /Pages\n/Kids [3 0 R]\n/Count 1\n>>\nendobj\n" +
                        "3 0 obj\n<<\n/Type /Page\n/Parent 2 0 R\n/MediaBox [0 0 612 792]\n" +
                        "/Contents 4 0 R\n>>\nendobj\n" +
                        "4 0 obj\n<<\n/Length 44\n>>\nstream\nBT\n/F1 12 Tf\n100 700 Td\n" +
                        "(Expense Receipt) Tj\nET\nendstream\nendobj\n" +
                        "xref\n0 5\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n" +
                        "0000000115 00000 n \n0000000207 00000 n \n" +
                        "trailer\n<<\n/Size 5\n/Root 1 0 R\n>>\nstartxref\n299\n%%EOF";
        
        return System.Text.Encoding.UTF8.GetBytes(pdfContent);
    }

    private byte[] GenerateFakeImageContent(string format)
    {
        // Generate a simple fake image header based on format
        // This is just placeholder content for testing purposes
        
        var baseContent = new List<byte>();
        
        switch (format.ToUpper())
        {
            case "JPEG":
            case "JPG":
                // JPEG file header
                baseContent.AddRange(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 });
                // Add some fake image data
                baseContent.AddRange(GenerateRandomBytes(1024)); // 1KB fake image
                // JPEG end marker
                baseContent.AddRange(new byte[] { 0xFF, 0xD9 });
                break;
                
            case "PNG":
                // PNG file header
                baseContent.AddRange(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
                // Add some fake PNG chunks
                baseContent.AddRange(GenerateRandomBytes(1024)); // 1KB fake image
                break;
                
            default:
                baseContent.AddRange(GenerateRandomBytes(1024));
                break;
        }
        
        return baseContent.ToArray();
    }

    private byte[] GenerateRandomBytes(int count)
    {
        var bytes = new byte[count];
        _random.NextBytes(bytes);
        return bytes;
    }

    private Address GenerateRandomAddress()
    {
        return new Address
        {
            Street = $"{_streets[_random.Next(_streets.Length)]} {_random.Next(1, 200)}",
            PostalCode = $"{_random.Next(10000, 99999)}",
            City = _cities[_random.Next(_cities.Length)],
            Country = (Countries)_random.Next(0, Enum.GetValues<Countries>().Length)
        };
    }

    private string GenerateGermanPhoneNumber()
    {
        var formats = new[]
        {
            $"+49-{_random.Next(30, 99)}-{_random.Next(1000000, 9999999)}",
            $"0{_random.Next(30, 99)}{_random.Next(1000000, 9999999)}",
            $"+49 {_random.Next(30, 99)} {_random.Next(1000000, 9999999)}"
        };
        
        return formats[_random.Next(formats.Length)];
    }

    private string GenerateCompanyEmail(string companyName)
    {
        var domain = companyName.ToLower()
            .Replace(" ", "")
            .Replace("gmbh", "")
            .Replace("ag", "")
            .Replace("co", "")
            .Trim();
        
        var domains = new[] { ".de", ".com", ".eu" };
        return $"info@{domain}{domains[_random.Next(domains.Length)]}";
    }

    private string GeneratePersonalEmail(string firstName, string lastName)
    {
        var domains = new[] { "gmail.com", "outlook.de", "web.de", "t-online.de", "gmx.de" };
        var formats = new[]
        {
            $"{firstName.ToLower()}.{lastName.ToLower()}",
            $"{firstName.ToLower()}{lastName.ToLower()}",
            $"{firstName.Substring(0, 1).ToLower()}{lastName.ToLower()}",
            $"{firstName.ToLower()}.{lastName.Substring(0, 1).ToLower()}"
        };

        return $"{formats[_random.Next(formats.Length)]}@{domains[_random.Next(domains.Length)]}";
    }
}