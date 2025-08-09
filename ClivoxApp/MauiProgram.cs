using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
using ClivoxApp.Models.Auth;
using ClivoxApp.Services;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace ClivoxApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            }).UseMauiCommunityToolkit();
            
            builder.Services.AddMauiBlazorWebView();
            
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Database
            builder.Services.AddSingleton<Npgsql.NpgsqlDataSource>(_ => 
                Npgsql.NpgsqlDataSource.Create("Host=localhost;Port=5432;Database=invoicing_db;Username=postgres;Password=123"));
            
            // Marten configuration
            builder.Services.AddMarten(options =>
            {
                options.Projections.Add<ClientProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<InvoiceProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<UserProjection>(ProjectionLifecycle.Inline);
            })
            .UseLightweightSessions()
            .UseNpgsqlDataSource();

            // Repositories
            builder.Services.AddSingleton<ClientRepository>();
            builder.Services.AddSingleton<InvoiceRepository>();
            builder.Services.AddSingleton<UserRepository>();
            
            // Services
            builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
            builder.Services.AddSingleton<AuthenticationService>();
            
            // UI Services
            builder.Services.AddMudServices();
            
            return builder.Build();
        }
    }
}