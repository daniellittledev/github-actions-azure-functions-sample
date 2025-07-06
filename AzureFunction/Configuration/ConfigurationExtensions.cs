using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Functions.Worker.Builder;

namespace AzureFunction.Configuration;

public static class ConfigurationExtensions
{
  public static FunctionsApplicationBuilder ConfigureAppConfiguration(this FunctionsApplicationBuilder builder)
  {
    var configBuilder = builder.Configuration;
    var environment = builder.Environment.EnvironmentName;

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

    // 4. Azure Key Vault (for live environments)
    if (environment != "Development")
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
        var credential = new DefaultAzureCredential();
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        config.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
      } catch (Exception ex)
      {
        // Log the error but don't fail the application startup
        Console.WriteLine($"Warning: Could not connect to Key Vault: {ex.Message}");
      }
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

  //IServiceScope
  public static void ValidateConfiguration(this IServiceProvider serviceProvider)
  {
    // Validate AppSettings configuration by resolving IOptions<AppSettings>
    serviceProvider.GetRequiredService<IOptions<AppSettings>>();
  }
}

