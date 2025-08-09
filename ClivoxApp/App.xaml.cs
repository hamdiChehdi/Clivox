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
        var window = new Window(new MainPage()) { Title = "ClivoxApp" };
        
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
}
