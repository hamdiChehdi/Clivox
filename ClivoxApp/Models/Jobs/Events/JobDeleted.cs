﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.EventSourcingInfrastucture;

namespace ClivoxApp.Models.Clients.Events;
public record JobDeleted : DomainEvent;
