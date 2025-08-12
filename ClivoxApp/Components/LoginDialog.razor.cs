using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using ClivoxApp.Services;

namespace ClivoxApp.Components;     

public partial class LoginDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private AuthenticationService AuthService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe = false;
    private bool _showPassword = false;
    private bool _isLoading = false;
    private string _errorMessage = string.Empty;

    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
        {
            _errorMessage = "Please enter both username and password.";
            return;
        }

        _isLoading = true;
        _errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var result = await AuthService.LoginAsync(_username, _password, _rememberMe);
            
            if (result.Success)
            {
                Snackbar.Add($"Welcome back, {result.User?.FullName}!", Severity.Success);
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _errorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"An unexpected error occurred. Please try again. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void TogglePasswordVisibility()
    {
        _showPassword = !_showPassword;
    }

    private async Task OnKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !_isLoading)
        {
            await Login();
        }
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}