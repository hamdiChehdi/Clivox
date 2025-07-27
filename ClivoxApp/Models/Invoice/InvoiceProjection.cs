using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClivoxApp.Models.Invoice.Events;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace ClivoxApp.Models.Invoice;

public class InvoiceProjection : SingleStreamProjection<Invoice, Guid>
{
    public InvoiceProjection()
    {
        DeleteEvent<InvoiceDeleted>();
    }

    public void Apply(InvoiceCreated @event, Invoice invoice)
    {
        invoice.Id = @event.Id;
        invoice.InvoiceNumber = @event.InvoiceNumber;
        invoice.InvoiceDate = @event.InvoiceDate;
        invoice.DueDate = @event.DueDate;
        invoice.ServiceDate = @event.ServiceDate;
        invoice.TotalAmount = @event.TotalAmount;
        invoice.ClientId = @event.ClientId;
        invoice.Items = @event.Items;
    }

    public void Apply(InvoiceUpdated @event, Invoice invoice)
    {
        invoice.InvoiceNumber = @event.InvoiceNumber;
        invoice.InvoiceDate = @event.InvoiceDate;
        invoice.DueDate = @event.DueDate;
        invoice.ServiceDate = @event.ServiceDate;
        invoice.TotalAmount = @event.TotalAmount;
        invoice.ClientId = @event.ClientId;
        invoice.Items = @event.Items;
    }

    public override Invoice ApplyMetadata(Invoice invoice, IEvent lastEvent)
    {
        if (invoice.CreatedOn == default)
        {
            invoice.CreatedOn = lastEvent.Timestamp.UtcDateTime;
        }
        invoice.ModifiedOn = lastEvent.Timestamp.UtcDateTime;
        return invoice;
    }
}
