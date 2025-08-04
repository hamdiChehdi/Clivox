using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Models.Invoice;
using CommunityToolkit.Maui.Storage;

namespace ClivoxApp.Components.Pages.Jobs;

/// <summary>
/// Standalone dialog component for managing expense proof files independently from invoice editing.
/// </summary>
public partial class ExpenseProofFilesManagerDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Guid InvoiceId { get; set; }

    [Parameter]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Parameter]
    public List<ExpenseProofFile> ExpenseProofFiles { get; set; } = new();

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private InvoiceRepository InvoiceRepository { get; set; } = null!;

    private bool _isUploadingFile = false;

    public decimal TotalExpenseAmount => ExpenseProofFiles.Sum(f => f.Amount);

    private async Task SelectFile()
    {
        if (_isUploadingFile) return;

        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.my.comic.extension" } },
                    { DevicePlatform.Android, new[] { "application/comics" } },
                    { DevicePlatform.WinUI, new[] { ".jpg", ".pdf", ".png" } },
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.macOS, new[] { "cbr", "cbz" } },
                });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick PDF/Image",
                FileTypes = customFileType,
            });

            if (result != null)
            {
                if (result.FileName.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||
                    result.FileName.EndsWith("png", StringComparison.OrdinalIgnoreCase) ||
                    result.FileName.EndsWith("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    await OnFilesChanged(result);
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error selecting file: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnFilesChanged(FileResult? file)
    {
        if (file == null) return;

        _isUploadingFile = true;
        StateHasChanged();

        try
        {
            const int maxFileSize = 10 * 1024 * 1024; // 10 MB limit

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

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

            // Show dialog to enter description and amount
            var parameters = new DialogParameters
            {
                ["CurrentDescription"] = "",
                ["CurrentAmount"] = 0.0m,
                ["FileName"] = file.FileName
            };

            var dialog = await DialogService.ShowAsync<FileDetailsDialog>("Add File Details", parameters);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is FileDetailsResult fileDetails)
            {
                expenseProofFile.Description = fileDetails.Description;
                expenseProofFile.Amount = fileDetails.Amount;
            }

            // Add to local list for immediate UI update
            ExpenseProofFiles.Add(expenseProofFile);

            // Save to database immediately
            await InvoiceRepository.AddExpenseProofFilesAsync(InvoiceId, new List<ExpenseProofFile> { expenseProofFile });

            Snackbar.Add("File uploaded and saved successfully.", Severity.Success);
            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
            Snackbar.Add("File upload timed out. Please try again.", Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error uploading file: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isUploadingFile = false;
            StateHasChanged();
        }
    }

    private async Task EditFileDetails(ExpenseProofFile file)
    {
        var parameters = new DialogParameters
        {
            ["CurrentDescription"] = file.Description,
            ["CurrentAmount"] = file.Amount,
            ["FileName"] = file.FileName
        };

        var dialog = await DialogService.ShowAsync<FileDetailsDialog>("Edit File Details", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is FileDetailsResult fileDetails)
        {
            file.Description = fileDetails.Description;
            file.Amount = fileDetails.Amount;

            // Save changes to database immediately
            await InvoiceRepository.ModifyExpenseProofFilesAsync(InvoiceId, new List<ExpenseProofFile> { file });

            Snackbar.Add("File details updated successfully.", Severity.Success);
            StateHasChanged();
        }
    }

    private async Task RemoveFile(ExpenseProofFile file)
    {
        var confirmation = await DialogService.ShowMessageBox(
            "Confirm Delete",
            "Are you sure you want to delete this file? This action cannot be undone.",
            yesText: "Delete", cancelText: "Cancel");

        if (confirmation == true)
        {
            // Remove from local list for immediate UI update
            ExpenseProofFiles.Remove(file);

            // Remove from database immediately
            await InvoiceRepository.DeleteExpenseProofFilesAsync(InvoiceId, new List<Guid> { file.Id });

            Snackbar.Add("File removed successfully.", Severity.Success);
            StateHasChanged();
        }
    }

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

    private void Close() => MudDialog.Close();
}