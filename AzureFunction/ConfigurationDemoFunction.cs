using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AzureFunction.Configuration;

namespace AzureFunction;

public class ConfigurationDemoFunction
{
  private readonly ILogger<ConfigurationDemoFunction> _logger;
  private readonly AppSettings _appSettings;

  public ConfigurationDemoFunction(
      ILogger<ConfigurationDemoFunction> logger,
      IOptions<AppSettings> appSettings)
  {
    _logger = logger;
    _appSettings = appSettings.Value;
  }

  [Function("ConfigurationDemo")]
  public IActionResult Run(
      [HttpTrigger(AuthorizationLevel.Function, "get", Route = "config")] HttpRequestData req)
  {
    _logger.LogInformation("Configuration demo function processed a request.");

    var configInfo = new
    {
      Environment = _appSettings.Environment,
      DatabaseTimeout = _appSettings.Database.CommandTimeout,
      ExternalApiBaseUrl = _appSettings.ExternalApi.BaseUrl,
      // Don't expose sensitive data like API keys or secrets in responses
      HasApiKey = !string.IsNullOrEmpty(_appSettings.ExternalApi.ApiKey),
      HasJwtSecret = !string.IsNullOrEmpty(_appSettings.Security.JwtSecret),
      TokenExpirationMinutes = _appSettings.Security.TokenExpirationMinutes,
      AllowedOrigins = _appSettings.Security.AllowedOrigins,
      Timestamp = DateTime.UtcNow
    };

    return new OkObjectResult(configInfo);
  }
}
