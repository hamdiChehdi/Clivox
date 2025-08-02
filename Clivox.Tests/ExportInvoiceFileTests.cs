using System;
using System.IO;
using System.Linq;
using ClivoxApp.Models;
using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using ClivoxApp.Models.Shared;
using ClivoxApp.Services;
using NPOI.SS.UserModel;

public class ExportInvoiceFileTests
{
    [Fact]
    public void ExportToExcel_CreatesValidExcelFile()
    {
        // Arrange
        var random = new Random(42); // Fixed seed for reproducible tests
        var testInvoiceNumber = $"INV-{random.Next(1000, 9999)}";
        var baseDate = new DateTime(2024, 1, 1);
        
        var invoice = new Invoice
        {
            InvoiceNumber = testInvoiceNumber,
            InvoiceDate = baseDate.AddDays(random.Next(0, 365)),
            DueDate = baseDate.AddDays(random.Next(366, 395)),
            ServiceDate = baseDate.AddDays(random.Next(-30, 30)),
            Items = new()
            {
                new InvoiceItem { Description = "Professional consulting services - Analysis and recommendations for system optimization", BillingType = BillingType.PerHour, Quantity = random.Next(5, 50), UnitPrice = (decimal)(random.NextDouble() * 100 + 50) },
                new InvoiceItem { Description = "Technical implementation - Database setup and configuration with performance tuning", BillingType = BillingType.PerHour, Quantity = random.Next(10, 40), UnitPrice = (decimal)(random.NextDouble() * 80 + 40) },
                new InvoiceItem { Description = "Project management fee - Coordination and oversight of development activities", BillingType = BillingType.FixedPrice, FixedAmount = (decimal)(random.NextDouble() * 1000 + 500) },
                new InvoiceItem { Description = "Software maintenance - Monthly support and bug fixes for existing applications", BillingType = BillingType.PerHour, Quantity = random.Next(8, 25), UnitPrice = (decimal)(random.NextDouble() * 60 + 30) },
                new InvoiceItem { Description = "Quality assurance testing - Comprehensive testing of all system components", BillingType = BillingType.PerObject, Quantity = random.Next(3, 12), UnitPrice = (decimal)(random.NextDouble() * 200 + 100) },
                new InvoiceItem { Description = "Documentation services - Creation of user manuals and technical specifications", BillingType = BillingType.PerHour, Quantity = random.Next(15, 35), UnitPrice = (decimal)(random.NextDouble() * 45 + 25) },
                new InvoiceItem { Description = "Training and support - End-user training sessions and ongoing support", BillingType = BillingType.PerHour, Quantity = random.Next(12, 30), UnitPrice = (decimal)(random.NextDouble() * 70 + 35) },
                new InvoiceItem { Description = "Security audit - Comprehensive security review and vulnerability assessment", BillingType = BillingType.FixedPrice, FixedAmount = (decimal)(random.NextDouble() * 800 + 400) },
                new InvoiceItem { Description = "Performance optimization - Code review and system performance improvements", BillingType = BillingType.PerHour, Quantity = random.Next(6, 20), UnitPrice = (decimal)(random.NextDouble() * 90 + 50) },
                new InvoiceItem { Description = "Data migration services - Transfer and validation of legacy data systems", BillingType = BillingType.PerSquareMeter, Area = random.Next(20, 100), PricePerSquareMeter = (decimal)(random.NextDouble() * 10 + 5) }
            }
        };

        var client = new Client
        {
            FirstName = "John",
            LastName = "Anderson",
            Gender = Gender.Male,
            Address = new Address
            {
                Street = "123 Business Avenue, Suite 456",
                PostalCode = "12345",
                City = "Springfield",
                Country = Countries.Germany
            }
        };

        var businessOwner = new BusinessOwner
        {
            CompanyName = "TechSolutions Pro Services",
            Email = "contact@techsolutions.example.com",
            PhoneNumber = "+49-123-456-7890",
            TaxNumber = "DE123456789",
            Address = new Address
            {
                Street = "789 Innovation Drive",
                PostalCode = "54321",
                City = "TechCity",
                Country = Countries.Germany
            },
            bankAccount = new BankAccount(
                "TechSolutions Pro Services",
                "DE89 3704 0044 0532 0130 00",
                "COBADEFFXXX",
                testInvoiceNumber
            )
        };

        // Act
        using var stream = ExportInvoiceFile.ExportToExcel(invoice, client, businessOwner);

        // Save the file locally for inspection (using proper temp directory)
        var outputDir = Path.Combine(Path.GetTempPath(), "TestExports");
        Directory.CreateDirectory(outputDir);
        var filePath = Path.Combine(outputDir, $"Invoice_{invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        stream.Position = 0;
        using (var file = File.Create(filePath))
        {
            stream.CopyTo(file);
        }

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);

        // Further: Check the Excel content (optional, basic check)
        stream.Position = 0;
        IWorkbook workbook = WorkbookFactory.Create(stream);
        var sheet = workbook.GetSheetAt(0);
        Assert.NotNull(sheet);

        // Check that the invoice number appears in the sheet
        bool foundInvoiceNumber = false;
        for (int i = 0; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null) continue;
            for (int j = 0; j < row.LastCellNum; j++)
            {
                var cell = row.GetCell(j);
                if (cell != null && cell.ToString().Contains(invoice.InvoiceNumber))
                {
                    foundInvoiceNumber = true;
                    break;
                }
            }
            if (foundInvoiceNumber) break;
        }
        Assert.True(foundInvoiceNumber, "Invoice number should appear in the exported Excel file.");
        Assert.True(File.Exists(filePath));
        
        // Clean up test file
        File.Delete(filePath);
    }
}