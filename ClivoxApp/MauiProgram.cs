using ClivoxApp.Models.Clients;
using ClivoxApp.Models.Invoice;
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
            builder.Services.AddSingleton<Npgsql.NpgsqlDataSource>(_ => Npgsql.NpgsqlDataSource.Create("Host=localhost;Port=5432;Database=invoicing_db;Username=postgres;Password=123"));
            // This is the absolute, simplest way to integrate Marten into your
            // .NET application with Marten's default configuration
            builder.Services.AddMarten(options =>
            {
                options.Projections.Add<ClientProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<InvoiceProjection>(ProjectionLifecycle.Inline);
            })// This is recommended in new development projects
            .UseLightweightSessions()// If you're using Aspire, use this option *instead* of specifying a connection
            // string to Marten
            .UseNpgsqlDataSource();
            builder.Services.AddSingleton<ClientRepository>();
            builder.Services.AddSingleton<InvoiceRepository>();
            builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
            builder.Services.AddMudServices();
            return builder.Build();
        }
    }
}