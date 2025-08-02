using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ClivoxApp.Components.Pages.Jobs;

/// <summary>
/// Dialog component for editing file details (description and amount) for expense proof files.
/// </summary>
public partial class FileDetailsDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string CurrentDescription { get; set; } = string.Empty;

    [Parameter]
    public decimal CurrentAmount { get; set; } = 0.0m;

    [Parameter]
    public string FileName { get; set; } = string.Empty;

    private string _description = string.Empty;
    private decimal _amount = 0.0m;
    private string _fileName = string.Empty;

    protected override void OnInitialized()
    {
        _description = CurrentDescription;
        _amount = CurrentAmount;
        _fileName = FileName;
    }

    private void Submit()
    {
        var result = new FileDetailsResult
        {
            Description = _description,
            Amount = _amount
        };
        MudDialog.Close(DialogResult.Ok(result));
    }

    private void Cancel() => MudDialog.Cancel();
}

/// <summary>
/// Result class for file details dialog containing description and amount data.
/// </summary>
public class FileDetailsResult
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 0.0m;
}