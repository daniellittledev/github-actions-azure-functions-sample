using System;
using AzureFunction.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureFunction;

public class ConfigurationDemoFunction
{
    private readonly ILogger<ConfigurationDemoFunction> logger;
    private readonly IHostEnvironment environment;
    private readonly AppSettings appSettings;

    public ConfigurationDemoFunction(
        ILogger<ConfigurationDemoFunction> logger,
        IOptions<AppSettings> appSettings,
        IHostEnvironment environment)
    {
        this.logger = logger;
        this.environment = environment;
        this.appSettings = appSettings.Value;
    }

    [Function("ConfigurationDemo")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "config")] HttpRequestData req)
    {
        logger.LogInformation("Configuration demo function processed a request.");
        logger.LogInformation("Environment: {Environment}", environment.EnvironmentName);
        return new OkObjectResult(appSettings);
    }
}
