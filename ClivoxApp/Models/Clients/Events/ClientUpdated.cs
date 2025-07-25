using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.Models.Shared;

namespace ClivoxApp.Models.Clients.Events;

public record ClientUpdated(string FirstName, string LastName, Gender Genre, string Email, string PhoneNumber, string Address) : DomainEvent;
