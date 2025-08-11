using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace AzureFunction.Configuration;

public class AppSettings
{
  public const string SectionName = "AppSettings";

  public string? ApplicationInsightsConnectionString { get; set; }

  [Required]
  public ExternalApiSettings ExternalApi { get; set; } = new();
}

public class ExternalApiSettings
{
  [Required]
  public string BaseUrl { get; set; } = string.Empty;

  [Required]
  public string ApiKey { get; set; } = string.Empty;

  public int TimeoutSeconds { get; set; } = 30;
}

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
  public ValidateOptionsResult Validate(string? name, AppSettings options)
  {
    var errors = new List<string>();

    if (string.IsNullOrEmpty(options.ExternalApi.BaseUrl))
    {
      errors.Add("External API base URL is required");
    }

    if (string.IsNullOrEmpty(options.ExternalApi.ApiKey))
    {
      errors.Add("External API key is required");
    }

    if (errors.Any())
    {
      return ValidateOptionsResult.Fail(errors);
    }

    return ValidateOptionsResult.Success;
  }
}
