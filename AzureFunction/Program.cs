using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AzureFunction.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

// Configure comprehensive configuration sources
builder.ConfigureAppConfiguration();

builder.ConfigureFunctionsWebApplication();

// Configure application settings with validation
builder.Services.ConfigureAppSettings(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Validate configuration on startup
using (var scope = app.Services.CreateScope())
{
  ConfigurationValidationService.ValidateConfiguration(scope.ServiceProvider);
}

app.Run();
