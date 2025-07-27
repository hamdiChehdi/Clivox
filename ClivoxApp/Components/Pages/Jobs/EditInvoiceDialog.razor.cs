using System.Linq;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Models.Invoice;

namespace ClivoxApp.Components.Pages.Jobs
{
    public partial class EditInvoiceDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; }

        [Parameter]
        public Invoice Invoice { get; set; } = new();

        private void AddItem(BillingType type)
        {
            Invoice.Items.Add(new InvoiceItem { BillingType = type });
        }

        private void RemoveItem(InvoiceItem item)
        {
            Invoice.Items.Remove(item);
        }

        private void Submit() => MudDialog.Close(DialogResult.Ok(Invoice));
        private void Cancel() => MudDialog.Cancel();
    }
}
