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

        public decimal TotalExpenseAmount => Invoice.ExpenseProofFiles.Sum(f => f.Amount);

        [Inject]
        private ISnackbar Snackbar { get; set; } = null!;

        [Inject]
        private IDialogService DialogService { get; set; } = null!;

        // Nullable DateTime properties for MudDatePicker binding
        private DateTime? _invoiceDate;
        private DateTime? _dueDate;
        private DateTime? _serviceDate;

        public DateTime? InvoiceDate
        {
            get => _invoiceDate ?? Invoice.InvoiceDate;
            set
            {
                _invoiceDate = value;
                if (value.HasValue)
                    Invoice.InvoiceDate = value.Value;
            }
        }

        public DateTime? DueDate
        {
            get => _dueDate ?? Invoice.DueDate;
            set
            {
                _dueDate = value;
                if (value.HasValue)
                    Invoice.DueDate = value.Value;
            }
        }

        public DateTime? ServiceDate
        {
            get => _serviceDate ?? Invoice.ServiceDate;
            set
            {
                _serviceDate = value;
                if (value.HasValue)
                    Invoice.ServiceDate = value.Value;
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Initialize nullable properties from Invoice
            _invoiceDate = Invoice.InvoiceDate;
            _dueDate = Invoice.DueDate;
            _serviceDate = Invoice.ServiceDate;
        }

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

                // Validate file type - only allow PDF and image files
                var allowedContentTypes = new[]
                {
                    "application/pdf",
                    "image/jpeg",
                    "image/jpg", 
                    "image/png"
                };

                if (!allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    Snackbar.Add($"File type '{file.ContentType}' is not supported. Only PDF and image files are allowed.", Severity.Error);
                    return;
                }

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

                // Immediately show dialog to enter description and amount
                var parameters = new DialogParameters
                {
                    ["CurrentDescription"] = "",
                    ["CurrentAmount"] = 0.0m,
                    ["FileName"] = file.Name
                };

                var dialog = await DialogService.ShowAsync<FileDetailsDialog>("Add File Details", parameters);
                var result = await dialog.Result;

                if (!result.Canceled && result.Data is FileDetailsResult fileDetails)
                {
                    expenseProofFile.Description = fileDetails.Description;
                    expenseProofFile.Amount = fileDetails.Amount;
                    
                    Invoice.ExpenseProofFiles.Add(expenseProofFile);
                    Snackbar.Add($"Successfully uploaded file with details.", Severity.Success);
                }
                else
                {
                    // User canceled - still add file but without description/amount
                    Invoice.ExpenseProofFiles.Add(expenseProofFile);
                    Snackbar.Add($"File uploaded. You can add details later by clicking the edit button.", Severity.Info);
                }

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

        private async Task EditFileDetails(ExpenseProofFile file)
        {
            var parameters = new DialogParameters
            {
                ["CurrentDescription"] = file.Description,
                ["CurrentAmount"] = file.Amount
            };

            var dialog = await DialogService.ShowAsync<FileDetailsDialog>("Edit File Details", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is FileDetailsResult fileDetails)
            {
                file.Description = fileDetails.Description;
                file.Amount = fileDetails.Amount;
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
}
