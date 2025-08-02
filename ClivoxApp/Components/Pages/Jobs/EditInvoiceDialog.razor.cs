using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using ClivoxApp.Models.Invoice;
using CommunityToolkit.Maui.Storage;

namespace ClivoxApp.Components.Pages.Jobs
{
    public partial class EditInvoiceDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = null!;

        [Parameter]
        public Invoice Invoice { get; set; } = new();

        [Inject]
        private ISnackbar Snackbar { get; set; } = null!;

        [Inject]
        private IDialogService DialogService { get; set; } = null!;

        private void AddItem(BillingType type)
        {
            Invoice.Items.Add(new InvoiceItem { BillingType = type });
        }

        private void RemoveItem(InvoiceItem item)
        {
            Invoice.Items.Remove(item);
        }

        private async void OnFilesChanged(IBrowserFile file)
        {
            try
            {
                const int maxFileSize = 10 * 1024 * 1024; // 10 MB limit

                if (file.Size > maxFileSize)
                {
                    Snackbar.Add($"File {file.Name} is too large. Maximum size is 10 MB.", Severity.Error);
                    return;
                }

                using var stream = file.OpenReadStream(maxFileSize);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var expenseProofFile = new ExpenseProofFile
                {
                    FileName = file.Name,
                    ContentType = file.ContentType,
                    FileSize = file.Size,
                    FileContent = memoryStream.ToArray(),
                    UploadedAt = DateTime.UtcNow
                };

                Invoice.ExpenseProofFiles.Add(expenseProofFile);


                Snackbar.Add($"Successfully uploaded file.", Severity.Success);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error uploading files: {ex.Message}", Severity.Error);
            }
        }
        private void RemoveFile(ExpenseProofFile file)
        {
            Invoice.ExpenseProofFiles.Remove(file);
            Snackbar.Add("File removed successfully.", Severity.Info);
            StateHasChanged();
        }

        private async Task EditFileDescription(ExpenseProofFile file)
        {
            var parameters = new DialogParameters
            {
                ["CurrentDescription"] = file.Description
            };

            var dialog = await DialogService.ShowAsync<FileDescriptionDialog>("Edit File Description", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is string newDescription)
            {
                file.Description = newDescription;
                StateHasChanged();
            }
        }

        private async Task DownloadFile(ExpenseProofFile file)
        {
            try
            {
                using var stream = new MemoryStream(file.FileContent);
                await FileSaver.Default.SaveAsync(file.FileName, stream, default);
                Snackbar.Add("File downloaded successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error downloading file: {ex.Message}", Severity.Error);
            }
        }

        private string GetFileIcon(string contentType)
        {
            return contentType.ToLower() switch
            {
                string ct when ct.Contains("pdf") => Icons.Material.Filled.PictureAsPdf,
                string ct when ct.Contains("image") => Icons.Material.Filled.Image,
                string ct when ct.Contains("word") || ct.Contains("document") => Icons.Material.Filled.Description,
                string ct when ct.Contains("excel") || ct.Contains("spreadsheet") => Icons.Material.Filled.TableChart,
                _ => Icons.Material.Filled.AttachFile
            };
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            return $"{bytes / (1024 * 1024):F1} MB";
        }

        private void Submit() => MudDialog.Close(DialogResult.Ok(Invoice));
        private void Cancel() => MudDialog.Cancel();
    }

    // Simple dialog for editing file descriptions
    public partial class FileDescriptionDialog : ComponentBase
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = null!;

        [Parameter]
        public string CurrentDescription { get; set; } = string.Empty;

        private string _description = string.Empty;

        protected override void OnInitialized()
        {
            _description = CurrentDescription;
        }

        private void Submit() => MudDialog.Close(DialogResult.Ok(_description));
        private void Cancel() => MudDialog.Cancel();
    }
}
