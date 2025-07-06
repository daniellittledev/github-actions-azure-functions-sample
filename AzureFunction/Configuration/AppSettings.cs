using System.ComponentModel.DataAnnotations;

namespace AzureFunction.Configuration;

public class AppSettings
{
  public const string SectionName = "AppSettings";

  [Required]
  public string Environment { get; set; } = string.Empty;

  public string? ApplicationInsightsConnectionString { get; set; }

  public DatabaseSettings Database { get; set; } = new();

  public ExternalApiSettings ExternalApi { get; set; } = new();

  public SecuritySettings Security { get; set; } = new();
}

public class DatabaseSettings
{
  [Required]
  public string ConnectionString { get; set; } = string.Empty;

  public int CommandTimeout { get; set; } = 30;

  public int MaxRetryCount { get; set; } = 3;
}

public class ExternalApiSettings
{
  [Required]
  public string BaseUrl { get; set; } = string.Empty;

  [Required]
  public string ApiKey { get; set; } = string.Empty;

  public int TimeoutSeconds { get; set; } = 30;
}

public class SecuritySettings
{
  [Required]
  public string JwtSecret { get; set; } = string.Empty;

  public int TokenExpirationMinutes { get; set; } = 60;

  public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
