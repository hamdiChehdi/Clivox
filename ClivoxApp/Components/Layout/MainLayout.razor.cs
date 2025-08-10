using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Services;
using ClivoxApp.Components;

namespace ClivoxApp.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    private AuthenticationService AuthService { get; set; } = null!;

    private bool _isDarkMode = true;
    private MudThemeProvider? _mudThemeProvider;
    private bool _isAuthenticated = false;
    private string _currentUserName = string.Empty;
    private string _currentLanguage = "en"; // Track current language
    private bool _authenticationCheckCompleted = false;
    private bool _showError = false;
    private string _errorMessage = string.Empty;
    private bool _needsLoginDialog = false;
    private bool _needsUnlockDialog = false;
    private bool _initialRenderComplete = false;

    MudTheme _theme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = MudBlazor.Colors.Blue.Default,
            Secondary = MudBlazor.Colors.Green.Accent4,
            AppbarBackground = MudBlazor.Colors.Red.Default,
        },
        PaletteDark = new PaletteDark()
        {
            Primary = MudBlazor.Colors.Yellow.Darken4,
        },
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px"
        }
    };

    protected override async Task OnInitializedAsync()
    {
        AuthService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        
        // Initialize current language from culture
        var currentCulture = System.Globalization.CultureInfo.CurrentUICulture;
        _currentLanguage = currentCulture.TwoLetterISOLanguageName.ToLower();
        
        try
        {
            await CheckAuthenticationState();
        }
        catch (OperationCanceledException)
        {
            _showError = true;
            _errorMessage = "Authentication check timed out. Please check your database connection.";
            _authenticationCheckCompleted = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _showError = true;
            _errorMessage = $"Authentication error: {ex.Message}";
            _authenticationCheckCompleted = true;
            StateHasChanged();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Load saved theme preference first, then fall back to system preference
            await LoadThemePreferenceAsync();
            _initialRenderComplete = true;
            
            // Show dialogs after initial render is complete
            if (_needsLoginDialog)
            {
                _needsLoginDialog = false;
                await ShowLoginDialog();
            }
            else if (_needsUnlockDialog)
            {
                _needsUnlockDialog = false;
                await ShowUnlockDialog();
            }
            
            StateHasChanged();
        }
    }

    private async Task LoadThemePreferenceAsync()
    {
        try
        {
            // Try to get saved theme preference from secure storage
            var savedTheme = await SecureStorage.Default.GetAsync("theme_preference");
            
            if (savedTheme == "dark")
            {
                _isDarkMode = true;
            }
            else if (savedTheme == "light")
            {
                _isDarkMode = false;
            }
            else
            {
                // No saved preference, use system preference
                _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
            // Fallback to system preference
            _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
        }
    }

    private async Task SaveThemePreferenceAsync()
    {
        try
        {
            await SecureStorage.Default.SetAsync("theme_preference", _isDarkMode ? "dark" : "light");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
        }
    }

    private async Task OnThemeChanged()
    {
        await SaveThemePreferenceAsync();
        StateHasChanged();
    }

    private async Task CheckAuthenticationState()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Starting authentication check...");
            
            // Try to restore session
            var sessionRestored = await AuthService.TryRestoreSessionAsync();
            
            System.Diagnostics.Debug.WriteLine($"Session restore result: {sessionRestored}");
            
            if (sessionRestored)
            {
                // Check if session is locked
                var isLocked = await AuthService.IsSessionLockedAsync();
                if (isLocked)
                {
                    if (_initialRenderComplete)
                    {
                        await ShowUnlockDialog();
                    }
                    else
                    {
                        _needsUnlockDialog = true;
                    }
                }
            }
            else
            {
                // No session found, show login dialog
                if (_initialRenderComplete)
                {
                    await ShowLoginDialog();
                }
                else
                {
                    _needsLoginDialog = true;
                }
            }
            
            UpdateAuthenticationState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Authentication check error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // If there's an error during authentication check, show login dialog
            if (_initialRenderComplete)
            {
                await ShowLoginDialog();
            }
            else
            {
                _needsLoginDialog = true;
            }
            UpdateAuthenticationState();
        }
        finally
        {
            _authenticationCheckCompleted = true;
            StateHasChanged();
        }
    }

    private void OnAuthenticationStateChanged(bool isAuthenticated)
    {
        UpdateAuthenticationState();
        StateHasChanged();
    }

    private void UpdateAuthenticationState()
    {
        _isAuthenticated = AuthService.IsAuthenticated;
        _currentUserName = AuthService.CurrentUserName ?? string.Empty;
        System.Diagnostics.Debug.WriteLine($"Authentication state updated: {_isAuthenticated}, User: {_currentUserName}");
    }

    private async Task ShowLoginDialog()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Attempting to show login dialog...");
            
            // Add a small delay to ensure the UI is ready
            await Task.Delay(100);
            
            var options = new DialogOptions 
            { 
                CloseOnEscapeKey = false,
                BackdropClick = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            System.Diagnostics.Debug.WriteLine("Opening login dialog...");
            var dialog = await DialogService.ShowAsync<LoginDialog>("Login Required", options);
            System.Diagnostics.Debug.WriteLine("Login dialog opened, waiting for result...");
            
            var result = await dialog.Result;
            System.Diagnostics.Debug.WriteLine($"Login dialog result: Canceled={result.Canceled}");

            if (result.Canceled)
            {
                // User canceled login - exit app
                System.Diagnostics.Debug.WriteLine("User canceled login, exiting app...");
                Application.Current?.Quit();
            }
            else
            {
                // Login was successful, update state
                System.Diagnostics.Debug.WriteLine("Login successful, updating state...");
                UpdateAuthenticationState();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing login dialog: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            Snackbar.Add("Error showing login dialog. Please restart the application.", Severity.Error);
        }
    }

    private async Task ShowUnlockDialog()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Attempting to show unlock dialog...");
            
            // Add a small delay to ensure the UI is ready
            await Task.Delay(100);
            
            var options = new DialogOptions 
            { 
                CloseOnEscapeKey = false,
                BackdropClick = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<UnlockDialog>("Session Locked", options);
            var result = await dialog.Result;

            if (result.Canceled || (result.Data is bool success && !success))
            {
                // Session unlock failed or user chose logout
                await ShowLoginDialog();
            }
            else
            {
                // Unlock was successful, update state
                UpdateAuthenticationState();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing unlock dialog: {ex.Message}");
            await ShowLoginDialog();
        }
    }

    private async Task RetryAuthentication()
    {
        _showError = false;
        _authenticationCheckCompleted = false;
        StateHasChanged();
        
        await CheckAuthenticationState();
    }

    private void NavigateToHome()
    {
        NavigationManager.NavigateTo("/");
    }

    private async Task ChangeLanguage(string language)
    {
        // Save current theme preference before reload
        await SaveThemePreferenceAsync();

        // Update current language
        _currentLanguage = language;

        var culture = new System.Globalization.CultureInfo(language);
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
        NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
    }

    private void OpenDatabaseAdmin()
    {
        NavigationManager.NavigateTo("/admin/database");
    }

    private async Task OpenBusinessOwnerDialog()
    {
        var parameters = new DialogParameters();
        var options = new DialogOptions { CloseOnEscapeKey = true };
        await DialogService.ShowAsync<BusinessOwnerDialog>("Business Owner", parameters, options);
    }

    private async Task LockSession()
    {
        await AuthService.LockSessionAsync();
        Snackbar.Add("Session locked. Enter your password to continue.", Severity.Info);
        await ShowUnlockDialog();
    }

    private async Task Logout()
    {
        var confirmation = await DialogService.ShowMessageBox(
            "Confirm Logout",
            "Are you sure you want to logout?",
            yesText: "Logout", cancelText: "Cancel");

        if (confirmation == true)
        {
            await AuthService.LogoutAsync();
            Snackbar.Add("You have been logged out.", Severity.Info);
            await ShowLoginDialog();
        }
    }

    // Test method to manually trigger login dialog
    private async Task TestShowLoginDialog()
    {
        System.Diagnostics.Debug.WriteLine("Test button clicked - showing login dialog...");
        await ShowLoginDialog();
    }

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.LightMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    public void Dispose()
    {
        AuthService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
