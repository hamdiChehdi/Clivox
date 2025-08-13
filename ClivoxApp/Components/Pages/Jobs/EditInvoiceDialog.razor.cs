using ClivoxApp.Models.Invoice;
using CommunityToolkit.Maui.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Linq;

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

            if (result is not null && !result.Canceled && result.Data is FileDetailsResult fileDetails)
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

        private async Task OnFilesChanged(FileResult? file)
        {
            if (file is null) 
            {
                Snackbar.Add($"No file loaded.", Severity.Error);
                return;
            }

            try
            {
                const int maxFileSize = 10 * 1024 * 1024; // 10 MB limit

                // Use CancellationToken with longer timeout for file operations
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

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

                
                // Use longer timeout for file reading with progress indication
                Snackbar.Add("Processing file...", Severity.Info);

                using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cts.Token);
                var fileContent = memoryStream.ToArray();

                if (fileContent.Length > maxFileSize)
                {
                    Snackbar.Add($"File {file.FileName} is too large. Maximum size is 10 MB.", Severity.Error);
                    return;
                }

                var expenseProofFile = new ExpenseProofFile
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = fileContent.Length,
                    FileContent = fileContent,
                    UploadedAt = DateTime.UtcNow
                };

                // Immediately show dialog to enter description and amount
                var parameters = new DialogParameters
                {
                    ["CurrentDescription"] = "",
                    ["CurrentAmount"] = 0.0m,
                    ["FileName"] = file.FileName
                };

                var dialog = await DialogService.ShowAsync<FileDetailsDialog>("Add File Details", parameters);
                var result = await dialog.Result;

                if (result is not null && !result.Canceled && result.Data is FileDetailsResult fileDetails)
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
            catch (OperationCanceledException)
            {
                Snackbar.Add("File upload timed out. Please try again with a smaller file or select the file more quickly.", Severity.Warning);
            }
            catch (TimeoutException)
            {
                Snackbar.Add("File selection took too long. Please try again.", Severity.Warning);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error uploading files: {ex.Message}", Severity.Error);
            }
        }

        public async void SelectFile()
        {
            try
            {
                var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.my.comic.extension" } }, // UTType values
                    { DevicePlatform.Android, new[] { "application/comics" } }, // MIME type
                    { DevicePlatform.WinUI, new[] { ".jpg", ".pdf", ".png" } }, // file extension
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "cbr", "cbz" } }, // UTType values
                });

                var result = await FilePicker.Default.PickAsync(new PickOptions
        {
                    PickerTitle = "Pick Pdf/Image",
                    FileTypes = customFileType,
                });

                if (result != null)
                {
                    if (result.FileName.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                        result.FileName.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                    {
                        //using var stream = await result.OpenReadAsync();
                        //var image = ImageSource.FromStream(() => stream);
                        await OnFilesChanged(result);
                    }
                }

            }
            catch (Exception)
            {
                // The user canceled or something went wrong

            }

        }

        private void Submit() => MudDialog.Close(DialogResult.Ok(Invoice));
        private void Cancel() => MudDialog.Cancel();

        private async Task ViewFile(ExpenseProofFile file)
        {
            var parameters = new DialogParameters
            {
                ["File"] = file
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                BackdropClick = true
            };

            await DialogService.ShowAsync<FileViewerDialog>("View File", parameters, options);
        }

        private bool IsImage(string contentType)
        {
            return contentType.ToLowerInvariant().Contains("image");
        }

        private bool IsPdf(string contentType)
        {
            return contentType.ToLowerInvariant().Contains("pdf");
        }

        private string GetImageDataUrl(ExpenseProofFile file)
        {
            if (!IsImage(file.ContentType)) return string.Empty;
            var base64String = Convert.ToBase64String(file.FileContent);
            return $"data:{file.ContentType};base64,{base64String}";
        }

        private string GetPdfDataUrl(ExpenseProofFile file)
        {
            if (!IsPdf(file.ContentType)) return string.Empty;
            var base64String = Convert.ToBase64String(file.FileContent);
            return $"data:{file.ContentType};base64,{base64String}";
        }
    }
}
