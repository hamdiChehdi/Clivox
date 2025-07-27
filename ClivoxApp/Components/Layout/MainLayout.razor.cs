﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MudBlazor;

namespace ClivoxApp.Components.Layout;
public partial class MainLayout
{
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

    protected override async Task OnInitializedAsync()
    {

    }

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.LightMode,
        false => Icons.Material.Outlined.DarkMode,
    };
}
