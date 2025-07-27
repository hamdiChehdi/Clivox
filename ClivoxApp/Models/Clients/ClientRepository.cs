using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Clients.Events;
using JasperFx.CodeGeneration.Frames;
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
        _querySession = querySession; // Assuming DocumentStore is configured elsewhere
        _documentStore = documentStore; // Get the store from the query session
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
        return clients;
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
