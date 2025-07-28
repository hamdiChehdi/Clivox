using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.Models.Clients.Events;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace ClivoxApp.Models.Clients;

public class JobProjection : SingleStreamProjection<Client, Guid>
{
    public JobProjection()
    {
        DeleteEvent<JobDeleted>();
    }

    public void Apply(JobCreated @event, Job job)
    {
        job.Description = @event.Description;
        job.Cost = @event.Cost;
    }

    public void Apply(JobUpdated @event, Job job)
    {
        job.Description = @event.Description;
        job.Cost = @event.Cost;
        job.Schedules = @event.Schedules ?? new List<JobSchedule>();
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
