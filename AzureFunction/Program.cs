using System.Text.Json;
using AzureFunction.Configuration;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.IO;

// Configure minimal Serilog bootstrap logger for early startup logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Path.GetTempPath(), "azurefunction-startup.txt"), 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 3)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Azure Function application");

    var builder = FunctionsApplication.CreateBuilder(args);

    // Configure comprehensive configuration sources
    builder.ConfigureAppConfiguration();

    // Replace bootstrap logger with fully configured Serilog from appsettings
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
    );

    builder.ConfigureFunctionsWebApplication();

    builder.Services.ConfigureAppSettings(builder.Configuration);

    builder.Services.AddHealthChecks();

    builder.Services.Configure<JsonSerializerOptions>(options =>
    {
        options.WriteIndented = true;
    });

    var app = builder.Build();

    // Validate configuration on startup
    using (var scope = app.Services.CreateScope())
    {
        Log.Information("Validating configuration on startup");
        scope.ServiceProvider.ValidateConfiguration();
        Log.Information("Configuration validation completed successfully");
    }

    Log.Information("Azure Function application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Azure Function application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
