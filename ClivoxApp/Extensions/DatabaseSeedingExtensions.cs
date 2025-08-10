using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using ClivoxApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System;
using System.Threading.Tasks;

namespace ClivoxApp.Extensions;

/// <summary>
/// Extension methods for seeding the database with test data
/// </summary>
public static class DatabaseSeedingExtensions
{
    /// <summary>
    /// Adds the database seeding service to the service collection
    /// </summary>
    public static IServiceCollection AddDatabaseSeeding(this IServiceCollection services)
    {
        services.AddTransient<DatabaseSeeder>();
        return services;
    }

    /// <summary>
    /// Quick method to seed the database with default test data
    /// Can be called from anywhere in your MAUI app
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider, 
        int clientCount = 20, 
        int invoicesPerClient = 4,
        bool addExpenseFiles = true,
        ISnackbar? snackbar = null)
    {
        var seeder = serviceProvider.GetRequiredService<DatabaseSeeder>();
        var logger = serviceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            logger.LogInformation("Starting database seeding...");
            snackbar?.Add("?? Starting database seeding...", Severity.Info);

            await seeder.SeedDatabaseAsync(clientCount, invoicesPerClient, addExpenseFiles);

            var message = $"? Successfully created {clientCount} clients and {clientCount * invoicesPerClient} invoices!";
            logger.LogInformation(message);
            snackbar?.Add(message, Severity.Success);
        }
        catch (Exception ex)
        {
            var errorMessage = $"? Error seeding database: {ex.Message}";
            logger.LogError(ex, "Database seeding failed");
            snackbar?.Add(errorMessage, Severity.Error);
            throw;
        }
    }

    /// <summary>
    /// Clear all test data from the database
    /// </summary>
    public static async Task ClearTestDataAsync(this IServiceProvider serviceProvider, ISnackbar? snackbar = null)
    {
        var seeder = serviceProvider.GetRequiredService<DatabaseSeeder>();
        var logger = serviceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            logger.LogInformation("Clearing test data...");
            snackbar?.Add("?? Clearing test data...", Severity.Info);

            await seeder.ClearTestDataAsync();

            const string message = "? All test data cleared successfully!";
            logger.LogInformation(message);
            snackbar?.Add(message, Severity.Success);
        }
        catch (Exception ex)
        {
            var errorMessage = $"? Error clearing data: {ex.Message}";
            logger.LogError(ex, "Data clearing failed");
            snackbar?.Add(errorMessage, Severity.Error);
            throw;
        }
    }
}