using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AzureFunction.Configuration;

public static class ConfigurationExtensions
{
  public static FunctionsApplicationBuilder ConfigureAppConfiguration(this FunctionsApplicationBuilder builder)
  {
    var configBuilder = builder.Configuration;
    var environment = builder.Environment;
        
    // 1. Base configuration files
    configBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

    // 2. User secrets (for Development environment only)
    if (builder.Environment.IsDevelopment())
    {
      configBuilder.AddUserSecrets<Program>(optional: true);
    }

    // 3. Environment variables
    configBuilder.AddEnvironmentVariables();

    // 4. Azure Key Vault (for live environments)
    if (!builder.Environment.IsDevelopment())
    {
      AddKeyVaultConfiguration(configBuilder);
    }

    return builder;
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
        Log.Information("Connecting to Azure Key Vault: {KeyVaultUrl}", keyVaultUrl);
        var credential = new DefaultAzureCredential();
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        Log.Information("Successfully connected to Azure Key Vault");
      } 
      catch (Exception ex)
      {
        // Log the error but don't fail the application startup
        Log.Warning(ex, "Could not connect to Key Vault: {KeyVaultUrl}", keyVaultUrl);
      }
    }
    else
    {
      Log.Information("No Key Vault URL configured, skipping Key Vault configuration");
    }
  }

  public static IServiceCollection ConfigureAppSettings(this IServiceCollection services, IConfiguration configuration)
  {
    // Configure and validate AppSettings
    services.AddSingleton<IValidateOptions<AppSettings>, AppSettingsValidator>();
    services
      .AddOptions<AppSettings>()
      .Bind(configuration.GetSection(AppSettings.SectionName))
      .ValidateDataAnnotations()
      .ValidateOnStart();

    return services;
  }

  public static void ValidateConfiguration(this IServiceProvider serviceProvider)
  {
    try
    {
      // Validate AppSettings configuration by resolving IOptions<AppSettings>
      var options = serviceProvider.GetRequiredService<IOptions<AppSettings>>();
      var appSettings = options.Value; // This will trigger validation
      Log.Information("AppSettings validation successful");
    }
    catch (OptionsValidationException ex)
    {
      Log.Error(ex, "Configuration validation failed: {ValidationFailures}", string.Join(", ", ex.Failures));
      throw;
    }
    catch (Exception ex)
    {
      Log.Error(ex, "Unexpected error during configuration validation");
      throw;
    }
  }
}

