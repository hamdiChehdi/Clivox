using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Clients.Events;
using ClivoxApp.Models.Invoice;
using ClivoxApp.Models.Shared;
using Marten;
using Microsoft.Extensions.Logging;

namespace ClivoxApp.Models.Clients;

public class ClientRepository
{
    private readonly ILogger _logger;
    private readonly IQuerySession _querySession;
    private readonly IDocumentStore _documentStore;

    public ClientRepository(IQuerySession querySession, IDocumentStore documentStore, ILogger<ClientRepository> logger)
    {
        _logger = logger;
        _querySession = querySession;
        _documentStore = documentStore;
    }

    public async Task<Client?> GetClientByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching client with ID: {ClientId}", id);
        var client = await _querySession.LoadAsync<Client>(id);
        if (client == null)
        {
            _logger.LogWarning("Client with ID: {ClientId} not found", id);
            return null;
        }
        
        // Load job count for the client
        await LoadJobCountAsync(client);
        
        return client;
    }

    public async Task<IReadOnlyList<Client>> GetAllClientsAsync()
    {
        _logger.LogInformation("Fetching all clients");
        var clients = await _querySession.Query<Client>().OrderBy(x => x.FullName).ToListAsync();
        if (clients.Count == 0)
        {
            _logger.LogWarning("No clients found");
        }

        // Load job counts for all clients
        await LoadJobCountsAsync(clients);

        return clients;
    }

    public async Task<IReadOnlyList<Client>> GetAllClientsWithJobCountsAsync()
    {
        _logger.LogInformation("Fetching all clients with job counts");
        var clients = await _querySession.Query<Client>().OrderBy(x => x.FullName).ToListAsync();
        
        if (clients.Count == 0)
        {
            _logger.LogWarning("No clients found");
            return clients;
        }

        // Load job counts for all clients
        await LoadJobCountsAsync(clients);

        return clients;
    }

    /// <summary>
    /// Gets filtered clients based on advanced criteria
    /// </summary>
    public async Task<IReadOnlyList<Client>> GetFilteredClientsAsync(ClientFilter filter)
    {
        _logger.LogInformation("Fetching filtered clients with criteria");

        // Get all clients first and then apply filters
        var allClients = await _querySession.Query<Client>().ToListAsync();
        
        // Load job counts for all clients
        await LoadJobCountsAsync(allClients);

        var filteredClients = allClients.AsEnumerable();

        // Apply text search
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchText = filter.SearchQuery.ToLower();
            filteredClients = filteredClients.Where(c => 
                (c.FirstName != null && c.FirstName.ToLower().Contains(searchText)) ||
                (c.LastName != null && c.LastName.ToLower().Contains(searchText)) ||
                (c.Email != null && c.Email.ToLower().Contains(searchText)) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(searchText)));
        }

        // Apply client type filter
        if (filter.ClientType.HasValue)
        {
            var isCompany = filter.ClientType.Value == ClientType.Company;
            filteredClients = filteredClients.Where(c => c.IsCompany == isCompany);
        }

        // Apply gender filter (for individual clients only)
        if (filter.Gender.HasValue)
        {
            filteredClients = filteredClients.Where(c => !c.IsCompany && c.Gender == filter.Gender.Value);
        }

        // Apply country filter
        if (filter.Country.HasValue)
        {
            filteredClients = filteredClients.Where(c => c.Address?.Country == filter.Country.Value);
        }

        // Apply city filter
        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var city = filter.City.ToLower();
            filteredClients = filteredClients.Where(c => c.Address?.City != null && c.Address.City.ToLower().Contains(city));
        }

        // Apply postal code filter
        if (!string.IsNullOrWhiteSpace(filter.PostalCode))
        {
            filteredClients = filteredClients.Where(c => c.Address?.PostalCode != null && c.Address.PostalCode.Contains(filter.PostalCode));
        }

        // Apply creation year filter
        if (filter.CreationYear.HasValue)
        {
            filteredClients = filteredClients.Where(c => c.CreatedOn.Year == filter.CreationYear.Value);
        }

        // Apply creation date range filter
        if (filter.CreatedFrom.HasValue)
        {
            filteredClients = filteredClients.Where(c => c.CreatedOn >= filter.CreatedFrom.Value);
        }
        if (filter.CreatedTo.HasValue)
        {
            var endDate = filter.CreatedTo.Value.AddDays(1);
            filteredClients = filteredClients.Where(c => c.CreatedOn < endDate);
        }

        // Apply invoice year filter
        if (filter.InvoiceYear.HasValue || filter.InvoicesFrom.HasValue || filter.InvoicesTo.HasValue)
        {
            var clientsWithInvoices = await GetClientsWithInvoicesInPeriodAsync(
                filter.InvoiceYear, filter.InvoicesFrom, filter.InvoicesTo);
            var clientIdsWithInvoices = clientsWithInvoices.Select(c => c.Id).ToHashSet();
            filteredClients = filteredClients.Where(c => clientIdsWithInvoices.Contains(c.Id));
        }

        // Apply job count filters
        if (filter.MinJobCount.HasValue)
        {
            filteredClients = filteredClients.Where(c => c.JobCount >= filter.MinJobCount.Value);
        }
        if (filter.MaxJobCount.HasValue)
        {
            filteredClients = filteredClients.Where(c => c.JobCount <= filter.MaxJobCount.Value);
        }

        // Apply has jobs filter
        if (filter.HasJobs.HasValue)
        {
            if (filter.HasJobs.Value)
            {
                filteredClients = filteredClients.Where(c => c.JobCount > 0);
            }
            else
            {
                filteredClients = filteredClients.Where(c => c.JobCount == 0);
            }
        }

        return filteredClients.OrderBy(c => c.FullName).ToList();
    }

    private async Task LoadJobCountAsync(Client client)
    {
        var jobCount = await _querySession.Query<Invoice.Invoice>()
            .CountAsync(i => i.ClientId == client.Id);
        client.JobCount = jobCount;
    }

    private async Task LoadJobCountsAsync(IReadOnlyList<Client> clients)
    {
        if (!clients.Any()) return;

        var clientIds = clients.Select(c => c.Id).ToList();
        
        // Get job counts for all clients in one query
        var invoices = await _querySession.Query<Invoice.Invoice>()
            .Where(i => clientIds.Contains(i.ClientId))
            .ToListAsync();

        var invoiceCounts = invoices
            .GroupBy(i => i.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToList();

        // Apply job counts to clients
        foreach (var client in clients)
        {
            var jobCount = invoiceCounts.FirstOrDefault(jc => jc.ClientId == client.Id);
            client.JobCount = jobCount?.Count ?? 0;
        }
    }

    /// <summary>
    /// Gets clients that have invoices within the specified period
    /// </summary>
    private async Task<List<Client>> GetClientsWithInvoicesInPeriodAsync(int? year, DateTime? from, DateTime? to)
    {
        var allInvoices = await _querySession.Query<Invoice.Invoice>().ToListAsync();

        var filteredInvoices = allInvoices.AsEnumerable();

        // Apply year filter
        if (year.HasValue)
        {
            filteredInvoices = filteredInvoices.Where(i => i.InvoiceDate.Year == year.Value);
        }

        // Apply date range filters
        if (from.HasValue)
        {
            filteredInvoices = filteredInvoices.Where(i => i.InvoiceDate >= from.Value);
        }
        if (to.HasValue)
        {
            var endDate = to.Value.AddDays(1);
            filteredInvoices = filteredInvoices.Where(i => i.InvoiceDate < endDate);
        }

        var clientIds = filteredInvoices.Select(i => i.ClientId).Distinct().ToList();

        var clientList = await _querySession.Query<Client>()
            .Where(c => clientIds.Contains(c.Id))
            .ToListAsync();
        
        return clientList.ToList();
    }

    /// <summary>
    /// Gets available years for client creation
    /// </summary>
    public async Task<List<int>> GetClientCreationYearsAsync()
    {
        var clients = await _querySession.Query<Client>().ToListAsync();
        return clients.Select(c => c.CreatedOn.Year)
                     .Distinct()
                     .OrderByDescending(y => y)
                     .ToList();
    }

    /// <summary>
    /// Gets available years for invoice creation
    /// </summary>
    public async Task<List<int>> GetInvoiceYearsAsync()
    {
        var invoices = await _querySession.Query<Invoice.Invoice>().ToListAsync();
        return invoices.Select(i => i.InvoiceDate.Year)
                      .Distinct()
                      .OrderByDescending(y => y)
                      .ToList();
    }

    /// <summary>
    /// Gets available countries from client addresses
    /// </summary>
    public async Task<List<Countries>> GetAvailableCountriesAsync()
    {
        var clients = await _querySession.Query<Client>()
            .Where(c => c.Address != null)
            .ToListAsync();
        
        return clients.Where(c => c.Address?.Country != null)
                     .Select(c => c.Address.Country)
                     .Distinct()
                     .OrderBy(c => c.ToString())
                     .ToList();
    }

    /// <summary>
    /// Gets available cities from client addresses
    /// </summary>
    public async Task<List<string>> GetAvailableCitiesAsync()
    {
        var clients = await _querySession.Query<Client>()
            .Where(c => c.Address != null && c.Address.City != null)
            .ToListAsync();
        
        return clients.Select(c => c.Address.City!)
                     .Where(city => !string.IsNullOrWhiteSpace(city))
                     .Distinct()
                     .OrderBy(city => city)
                     .ToList();
    }

    public async Task FindClient(string lowerCaseSearchText)
    {
        var clients = await _querySession.Query<Client>()
            .Where(c => (c.FirstName != null && c.FirstName.ToLower().Contains(lowerCaseSearchText)) ||
                        (c.LastName != null && c.LastName.ToLower().Contains(lowerCaseSearchText)) || 
                        (c.Email != null && c.Email.ToLower().Contains(lowerCaseSearchText)) ||
                        (c.CompanyName != null && c.CompanyName.ToLower().Contains(lowerCaseSearchText)))
            .ToListAsync();
    }

    public async Task AddClientAsync(Client client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        
        // Validate client has minimum required information
        if (!client.IsValid())
        {
            var errors = client.GetValidationErrors();
            var errorMessage = string.Join(", ", errors);
            _logger.LogError("Cannot add client - validation failed: {Errors}", errorMessage);
            throw new ArgumentException($"Client validation failed: {errorMessage}", nameof(client));
        }
        
        _logger.LogInformation("Adding new client: {ClientName}", client.FullName);
        var evt = new ClientCreated(
            client.FirstName,
            client.LastName,
            client.CompanyName,
            client.IsCompany,
            client.Gender,
            client.Email,
            client.PhoneNumber,
            client.Address);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Client>(null, evt, null);
        await session.SaveChangesAsync();
    }

    public async Task DeleteClientAsync(Guid id)
    {
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Client>(id, new ClientDeleted(), null);
        await session.SaveChangesAsync();
    }

    public async Task UpdateClientAsync(Client client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        
        // Validate client has minimum required information
        if (!client.IsValid())
        {
            var errors = client.GetValidationErrors();
            var errorMessage = string.Join(", ", errors);
            _logger.LogError("Cannot update client - validation failed: {Errors}", errorMessage);
            throw new ArgumentException($"Client validation failed: {errorMessage}", nameof(client));
        }
        
        _logger.LogInformation("Updating client: {ClientName}", client.FullName);
        var evt = new ClientUpdated(
            client.FirstName,
            client.LastName,
            client.CompanyName,
            client.IsCompany,
            client.Gender,
            client.Email,
            client.PhoneNumber,
            client.Address);
        using var session = _documentStore.LightweightSession();
        session.StoreEvents<Client>(client.Id, evt, null);
        await session.SaveChangesAsync();
    }
}
