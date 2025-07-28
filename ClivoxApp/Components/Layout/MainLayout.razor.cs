using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace ClivoxApp.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    //private MudTheme _theme = new();
    private bool _isDarkMode = true;
    private MudThemeProvider? _mudThemeProvider;

    // Customize theme colors if you want rather than defaults.
    // https://mudblazor.com/customization/overview#custom-themes        

    MudTheme _theme = new MudTheme()
    {
        // Colors : https://mudblazor.com/features/colors#material-colors-list-of-material-colors

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            StateHasChanged();
        }
    }

    private void NavigateToHome()
    {
        NavigationManager.NavigateTo("/");
    }

    private void ChangeLanguage(string language)
    {
        var culture = new System.Globalization.CultureInfo(language);
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
        NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
    }

    private async Task OpenBusinessOwnerDialog()
    {
        var parameters = new DialogParameters();
        var options = new DialogOptions { CloseOnEscapeKey = true };
        await DialogService.ShowAsync<BusinessOwnerDialog>("Business Owner", parameters, options);
    }

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.LightMode,
        false => Icons.Material.Outlined.DarkMode,
    };
}
