using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using ClivoxApp.Services;

namespace ClivoxApp.Components;

public partial class UnlockDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private AuthenticationService AuthService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private string _password = string.Empty;
    private bool _showPassword = false;
    private bool _isLoading = false;
    private string _errorMessage = string.Empty;

    private async Task Unlock()
    {
        if (string.IsNullOrWhiteSpace(_password))
        {
            _errorMessage = "Please enter your password.";
            return;
        }

        _isLoading = true;
        _errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var success = await AuthService.UnlockSessionAsync(_password);
            
            if (success)
            {
                Snackbar.Add("Session unlocked successfully!", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _errorMessage = "Invalid password. Please try again.";
                _password = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task Logout()
    {
        await AuthService.LogoutAsync();
        MudDialog.Close(DialogResult.Ok(false));
    }

    private void TogglePasswordVisibility()
    {
        _showPassword = !_showPassword;
    }

    private async Task OnKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !_isLoading)
        {
            await Unlock();
        }
    }
}