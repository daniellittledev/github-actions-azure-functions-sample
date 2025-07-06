using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker.Builder;

namespace AzureFunction.Configuration;

public static class ConfigurationExtensions
{
  public static FunctionsApplicationBuilder ConfigureAppConfiguration(this FunctionsApplicationBuilder builder)
  {
    // Get the current configuration
    var configuration = builder.Configuration;
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    // Create a new configuration builder
    var configBuilder = new ConfigurationBuilder();

    // 1. Base configuration files
    configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

    // 2. User secrets (for Development environment only)
    if (environment == "Development")
    {
      configBuilder.AddUserSecrets<Program>(optional: true);
    }

    // 3. Environment variables
    configBuilder.AddEnvironmentVariables();

    // 4. Azure Key Vault (for Production environment)
    if (environment == "Production")
    {
      AddKeyVaultConfiguration(configBuilder);
    }

    // Build and replace the configuration
    var newConfiguration = configBuilder.Build();

    // Replace the configuration in the builder
    foreach (var source in configBuilder.Sources)
    {
      builder.Configuration.Sources.Add(source);
    }

    return builder;
  }

  public static IServiceCollection ConfigureAppSettings(this IServiceCollection services, IConfiguration configuration)
  {
    // Configure and validate AppSettings
    services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
    services.AddSingleton<IValidateOptions<AppSettings>, AppSettingsValidator>();

    return services;
  }

  private static void AddKeyVaultConfiguration(IConfigurationBuilder config)
  {
    // Build intermediate configuration to get Key Vault URL
    var tempConfig = config.Build();
    var keyVaultUrl = tempConfig["KeyVault:Url"];

    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
      try
      {
        var credential = new DefaultAzureCredential();
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
      }
      catch (Exception ex)
      {
        // Log the error but don't fail the application startup
        Console.WriteLine($"Warning: Could not connect to Key Vault: {ex.Message}");
      }
    }
  }
}

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
  public ValidateOptionsResult Validate(string? name, AppSettings options)
  {
    var errors = new List<string>();

    if (string.IsNullOrEmpty(options.Environment))
      errors.Add("Environment is required");

    if (string.IsNullOrEmpty(options.Database.ConnectionString))
      errors.Add("Database connection string is required");

    if (string.IsNullOrEmpty(options.ExternalApi.BaseUrl))
      errors.Add("External API base URL is required");

    if (string.IsNullOrEmpty(options.ExternalApi.ApiKey))
      errors.Add("External API key is required");

    if (string.IsNullOrEmpty(options.Security.JwtSecret))
      errors.Add("JWT secret is required");

    if (errors.Any())
    {
      return ValidateOptionsResult.Fail(errors);
    }

    return ValidateOptionsResult.Success;
  }
}
