using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using ClivoxApp.Services;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ClivoxApp.DatabaseSeeding;

/// <summary>
/// Console application to seed the database with test data
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("?? ClivoxApp Database Seeder");
        Console.WriteLine("============================");
        
        // Build host with services
        using var host = CreateHost();
        
        var seeder = host.Services.GetRequiredService<DatabaseSeeder>();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            Console.WriteLine();
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Seed database with test data (20 clients, 4 invoices each)");
            Console.WriteLine("2. Seed database with custom amount");
            Console.WriteLine("3. Clear all test data");
            Console.WriteLine("4. Exit");
            Console.Write("\nEnter your choice (1-4): ");
            
            var choice = Console.ReadLine();
            
            switch (choice)
            {
                case "1":
                    await SeedDefaultData(seeder);
                    break;
                case "2":
                    await SeedCustomData(seeder);
                    break;
                case "3":
                    await ClearData(seeder);
                    break;
                case "4":
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Exiting.");
                    return;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while running the database seeder");
            Console.WriteLine($"\n? Error: {ex.Message}");
            Console.WriteLine("Check the logs for more details.");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Database configuration
                services.AddSingleton<Npgsql.NpgsqlDataSource>(_ => 
                    Npgsql.NpgsqlDataSource.Create("Host=localhost;Port=5432;Database=invoicing_db;Username=postgres;Password=123"));
                
                // Marten configuration
                services.AddMarten(options =>
                {
                    options.Projections.Add<ClientProjection>(ProjectionLifecycle.Inline);
                    options.Projections.Add<InvoiceProjection>(ProjectionLifecycle.Inline);
                })
                .UseLightweightSessions()
                .UseNpgsqlDataSource();

                // Repositories
                services.AddSingleton<ClientRepository>();
                services.AddSingleton<InvoiceRepository>();
                
                // Seeder service
                services.AddTransient<DatabaseSeeder>();
                
                // Logging
                services.AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();
    }
    
    static async Task SeedDefaultData(DatabaseSeeder seeder)
    {
        Console.WriteLine("\n?? Starting database seeding with default data...");
        Console.WriteLine("   • 20 clients (70% individuals, 30% companies)");
        Console.WriteLine("   • 4 invoices per client (80 total invoices)");
        Console.WriteLine("   • Random addresses across German cities");
        Console.WriteLine("   • Varied invoice items with different billing types");
        
        Console.Write("\nProceed? (y/N): ");
        var confirm = Console.ReadLine();
        
        if (confirm?.ToLower() == "y")
        {
            var startTime = DateTime.Now;
            await seeder.SeedDatabaseAsync(20, 4);
            var elapsed = DateTime.Now - startTime;
            
            Console.WriteLine($"\n? Database seeding completed in {elapsed.TotalSeconds:F1} seconds!");
            Console.WriteLine("\n?? Summary:");
            Console.WriteLine("   • 20 clients created");
            Console.WriteLine("   • 80 invoices created");
            Console.WriteLine("   • Ready for UI testing!");
        }
        else
        {
            Console.WriteLine("Operation cancelled.");
        }
    }
    
    static async Task SeedCustomData(DatabaseSeeder seeder)
    {
        Console.WriteLine("\n???  Custom Data Seeding");
        
        Console.Write("Number of clients to create: ");
        if (!int.TryParse(Console.ReadLine(), out int clientCount) || clientCount < 1)
        {
            Console.WriteLine("Invalid number of clients. Using default: 20");
            clientCount = 20;
        }
        
        Console.Write("Number of invoices per client: ");
        if (!int.TryParse(Console.ReadLine(), out int invoicesPerClient) || invoicesPerClient < 1)
        {
            Console.WriteLine("Invalid number of invoices. Using default: 4");
            invoicesPerClient = 4;
        }
        
        var totalInvoices = clientCount * invoicesPerClient;
        Console.WriteLine($"\nThis will create:");
        Console.WriteLine($"   • {clientCount} clients");
        Console.WriteLine($"   • {totalInvoices} invoices total");
        
        Console.Write("\nProceed? (y/N): ");
        var confirm = Console.ReadLine();
        
        if (confirm?.ToLower() == "y")
        {
            var startTime = DateTime.Now;
            await seeder.SeedDatabaseAsync(clientCount, invoicesPerClient);
            var elapsed = DateTime.Now - startTime;
            
            Console.WriteLine($"\n? Custom seeding completed in {elapsed.TotalSeconds:F1} seconds!");
            Console.WriteLine($"\n?? Summary:");
            Console.WriteLine($"   • {clientCount} clients created");
            Console.WriteLine($"   • {totalInvoices} invoices created");
        }
        else
        {
            Console.WriteLine("Operation cancelled.");
        }
    }
    
    static async Task ClearData(DatabaseSeeder seeder)
    {
        Console.WriteLine("\n?? Clear All Test Data");
        Console.WriteLine("This will permanently delete ALL clients and invoices from the database!");
        
        Console.Write("\nAre you absolutely sure? (yes/N): ");
        var confirm = Console.ReadLine();
        
        if (confirm?.ToLower() == "yes")
        {
            Console.WriteLine("\nClearing data...");
            var startTime = DateTime.Now;
            await seeder.ClearTestDataAsync();
            var elapsed = DateTime.Now - startTime;
            
            Console.WriteLine($"\n? Data cleared in {elapsed.TotalSeconds:F1} seconds!");
        }
        else
        {
            Console.WriteLine("Operation cancelled - no data was deleted.");
        }
    }
}