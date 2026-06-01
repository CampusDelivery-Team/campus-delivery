using CampusDelivery.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse(
            Application: "CampusDelivery.Api",
            Status: "OK",
            Environment: hostEnvironment.EnvironmentName,
            ServerTime: DateTimeOffset.UtcNow));
    }
}
