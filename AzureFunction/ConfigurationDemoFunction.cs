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
        _logger.LogInformation("Environment: {Environment}", _appSettings.Environment);
        return new OkObjectResult(_appSettings);
    }
}
