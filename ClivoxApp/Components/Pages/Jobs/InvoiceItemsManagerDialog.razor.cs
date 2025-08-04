using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Models.Invoice;

namespace ClivoxApp.Components.Pages.Jobs;

/// <summary>
/// Standalone dialog component for managing invoice items independently from invoice editing.
/// </summary>
public partial class InvoiceItemsManagerDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Guid InvoiceId { get; set; }

    [Parameter]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Parameter]
    public List<InvoiceItem> InvoiceItems { get; set; } = new();

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private InvoiceRepository InvoiceRepository { get; set; } = null!;

    public decimal TotalAmount => InvoiceItems.Sum(i => i.Total);

    private async Task AddNewItem(BillingType billingType)
    {
        var newItem = new InvoiceItem
        {
            BillingType = billingType,
            Description = GetDefaultDescription(billingType)
        };

        // Add to local list for immediate UI update
        InvoiceItems.Add(newItem);

        // Save to database immediately
        await InvoiceRepository.AddInvoiceItemsAsync(InvoiceId, new List<InvoiceItem> { newItem });

        Snackbar.Add($"New {GetBillingTypeDisplayName(billingType)} item added.", Severity.Success);
        StateHasChanged();
    }

    private async Task OnItemChanged(InvoiceItem item)
    {
        try
        {
            // Save changes to database immediately
            await InvoiceRepository.ModifyInvoiceItemsAsync(InvoiceId, new List<InvoiceItem> { item });
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error updating item: {ex.Message}", Severity.Error);
        }
    }

    private async Task RemoveItem(InvoiceItem item)
    {
        var confirmation = await DialogService.ShowMessageBox(
            "Confirm Delete",
            "Are you sure you want to delete this item? This action cannot be undone.",
            yesText: "Delete", cancelText: "Cancel");

        if (confirmation == true)
        {
            // Remove from local list for immediate UI update
            InvoiceItems.Remove(item);

            // Remove from database immediately
            await InvoiceRepository.DeleteInvoiceItemsAsync(InvoiceId, new List<Guid> { item.Id });

            Snackbar.Add("Item removed successfully.", Severity.Success);
            StateHasChanged();
        }
    }

    private string GetBillingTypeDisplayName(BillingType billingType)
    {
        return billingType switch
        {
            BillingType.PerHour => "Per Hour",
            BillingType.PerSquareMeter => "Per Square Meter",
            BillingType.FixedPrice => "Fixed Price",
            BillingType.PerObject => "Per Object",
            _ => "Unknown"
        };
    }

    private string GetBillingTypeIcon(BillingType billingType)
    {
        return billingType switch
        {
            BillingType.PerHour => Icons.Material.Filled.Schedule,
            BillingType.PerSquareMeter => Icons.Material.Filled.SquareFoot,
            BillingType.FixedPrice => Icons.Material.Filled.AttachMoney,
            BillingType.PerObject => Icons.Material.Filled.Inventory,
            _ => Icons.Material.Filled.List
        };
    }

    private MudBlazor.Color GetBillingTypeColor(BillingType billingType)
    {
        return billingType switch
        {
            BillingType.PerHour => MudBlazor.Color.Primary,
            BillingType.PerSquareMeter => MudBlazor.Color.Secondary,
            BillingType.FixedPrice => MudBlazor.Color.Success,
            BillingType.PerObject => MudBlazor.Color.Info,
            _ => MudBlazor.Color.Default
        };
    }

    private string GetDefaultDescription(BillingType billingType)
    {
        return billingType switch
        {
            BillingType.PerHour => "Service hours",
            BillingType.PerSquareMeter => "Area-based service",
            BillingType.FixedPrice => "Fixed price service",
            BillingType.PerObject => "Object-based service",
            _ => "Service"
        };
    }

    private void Close() => MudDialog.Close();
}