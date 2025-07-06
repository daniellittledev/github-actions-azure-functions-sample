using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace AzureFunction.Configuration;

public class ConfigurationValidationService
{
  public static void ValidateConfiguration(IServiceProvider serviceProvider)
  {
    var logger = serviceProvider.GetRequiredService<ILogger<ConfigurationValidationService>>();

    try
    {
      // Validate AppSettings
      var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
      ValidateObject(appSettings, "AppSettings");

      logger.LogInformation("Configuration validation completed successfully");
    }
    catch (ValidationException ex)
    {
      logger.LogError(ex, "Configuration validation failed: {Message}", ex.Message);
      throw;
    }
  }

  private static void ValidateObject(object obj, string sectionName)
  {
    var context = new ValidationContext(obj);
    var results = new List<ValidationResult>();

    if (!Validator.TryValidateObject(obj, context, results, true))
    {
      var errors = results.Select(r => r.ErrorMessage).ToArray();
      throw new ValidationException($"Configuration validation failed for {sectionName}: {string.Join(", ", errors)}");
    }
  }
}
