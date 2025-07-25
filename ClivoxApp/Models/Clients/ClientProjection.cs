using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.Models.Clients.Events;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace ClivoxApp.Models.Clients;

public class ClientProjection : SingleStreamProjection<Client, Guid>
{
    public ClientProjection()
    {
        DeleteEvent<ClientDeleted>();
    }

    public void Apply(ClientCreated @event, Client client)
    {
        client.FirstName = @event.FirstName;
        client.LastName = @event.LastName;
        client.Gender = @event.Genre;
        client.Email = @event.Email;
        client.PhoneNumber = @event.PhoneNumber;
        client.Address = @event.Address;
    }

    public void Apply(ClientUpdated @event, Client client)
    {
        client.FirstName = @event.FirstName;
        client.LastName = @event.LastName;
        client.Gender = @event.Genre;
        client.Email = @event.Email;
        client.PhoneNumber = @event.PhoneNumber;
        client.Address = @event.Address;
    }

    public override Client ApplyMetadata(Client client, IEvent lastEvent)
    {
        if (client.CreatedOn == default)
        {
            client.CreatedOn = lastEvent.Timestamp.UtcDateTime;
        }
        client.ModifiedOn = lastEvent.Timestamp.UtcDateTime;

        return client;
    }
}
