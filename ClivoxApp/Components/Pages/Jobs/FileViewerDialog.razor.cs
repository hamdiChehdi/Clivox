using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Models.Invoice;
using CommunityToolkit.Maui.Storage;

namespace ClivoxApp.Components.Pages.Jobs;

/// <summary>
/// Dialog component for viewing expense proof files (images and PDFs) with download functionality.
/// </summary>
public partial class FileViewerDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public ExpenseProofFile File { get; set; } = new();

    private bool IsImage()
    {
        var contentType = File.ContentType.ToLowerInvariant();
        return contentType.Contains("image");
    }

    private bool IsPdf()
    {
        var contentType = File.ContentType.ToLowerInvariant();
        return contentType.Contains("pdf");
    }

    private string GetFileIcon()
    {
        if (IsImage()) return Icons.Material.Filled.Image;
        if (IsPdf()) return Icons.Material.Filled.PictureAsPdf;
        return Icons.Material.Filled.AttachFile;
    }

    private string GetImageSource()
    {
        if (!IsImage()) return string.Empty;
        var base64String = Convert.ToBase64String(File.FileContent);
        return $"data:{File.ContentType};base64,{base64String}";
    }

    private string GetPdfSource()
    {
        if (!IsPdf()) return string.Empty;
        var base64String = Convert.ToBase64String(File.FileContent);
        return $"data:{File.ContentType};base64,{base64String}";
    }

    private async Task DownloadFile()
    {
        try
        {
            using var stream = new MemoryStream(File.FileContent);
            await FileSaver.Default.SaveAsync(File.FileName, stream, default);
        }
        catch (Exception)
        {
            // Handle download error - could show a snackbar if injected
        }
    }

    private void Close() => MudDialog.Close();
}