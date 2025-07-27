using ClivoxApp.EventSourcingInfrastucture;

namespace ClivoxApp.Models.Clients;

public class Job : IAggregateRoot
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string Description { get; set; } = null!;
    public decimal Cost { get; set; } = default!;
    public List<JobSchedule> Schedules { get; set; } = [];
}

public record JobSchedule(DateTime StartTime, DateTime EndTime);



