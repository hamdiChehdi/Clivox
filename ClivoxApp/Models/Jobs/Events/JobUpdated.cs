using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients.Events;

public record JobUpdated(string Description, decimal Cost, List<JobSchedule> Schedules) : DomainEvent;
