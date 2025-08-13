using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AzureFunction.Routes
{
    public class HealthCheck
    {
        private readonly HealthCheckService _healthCheck;

        public HealthCheck(HealthCheckService healthCheck)
        {
            _healthCheck = healthCheck;
        }

        [Function(nameof(HealthCheck))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "health")] HttpRequestData req,
           FunctionContext context)
        {
            var healthStatus = await _healthCheck.CheckHealthAsync();
            return new OkObjectResult(Enum.GetName(typeof(HealthStatus), healthStatus.Status));
        }
    }
}
