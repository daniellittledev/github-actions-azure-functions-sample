using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AzureFunction.Routes;

public class RootFunction
{
    private readonly ILogger<RootFunction> logger;

    public RootFunction(ILogger<RootFunction> logger)
    {
        this.logger = logger;
    }

    [Function("Root")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{ignored:maxlength(0)?}")] HttpRequestData req)
    {
        logger.LogInformation("Root function processed a request.");
        
        var response = new
        {
            Message = "Azure Functions CI/CD Demo",
            Timestamp = DateTime.UtcNow,
            Endpoints = new
            {
                Config = "/config",
                Health = "/health"
            }
        };

        return new OkObjectResult(response);
    }
}
