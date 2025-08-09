using ClivoxApp.Models.Auth;

namespace ClivoxApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) 
        { 
            Title = "ClivoxApp"
        };

        // Set minimum and default window dimensions for desktop platforms
        if (DeviceInfo.Platform == DevicePlatform.WinUI || 
            DeviceInfo.Platform == DevicePlatform.MacCatalyst)
        {
            // Minimum window size - prevents user from making window too small
            window.MinimumWidth = 1024;   // Minimum width: 1024px
            window.MinimumHeight = 768;   // Minimum height: 768px
            
            // Default window size when app starts
            window.Width = 1200;          // Default width: 1200px
            window.Height = 900;          // Default height: 900px
            
            // Optional: Set maximum dimensions if you want to limit window size
            // window.MaximumWidth = 1920;
            // window.MaximumHeight = 1080;

            // Optional: Center the window on screen
            window.X = (DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - window.Width) / 2;
            window.Y = (DeviceDisplay.Current.MainDisplayInfo.Height / DeviceDisplay.Current.MainDisplayInfo.Density - window.Height) / 2;
        }

        // Optional: Handle window events
        window.Created += OnWindowCreated;
        window.Activated += OnWindowActivated;
        window.Deactivated += OnWindowDeactivated;
        
        // Initialize default user on app start
        Task.Run(async () =>
        {
            try
            {
                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    var userRepository = services.GetService<UserRepository>();
                    if (userRepository != null)
                    {
                        await userRepository.EnsureDefaultUserAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"Error initializing default user: {ex.Message}");
            }
        });
        
        return window;
    }

    private void OnWindowCreated(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Window created");
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Window activated (focused)");
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("Window deactivated (lost focus)");
    }
}
