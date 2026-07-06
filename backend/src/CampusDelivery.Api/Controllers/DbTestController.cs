using CampusDelivery.Api.Dtos;
using CampusDelivery.Api.Persistence.Oracle;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DbTestController(OracleConnectionFactory connectionFactory) : ControllerBase
{
    [HttpGet("ping")]
    public async Task<ActionResult<DatabasePingResponse>> Ping(CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM users";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var count = Convert.ToInt32(result);

        return Ok(new DatabasePingResponse(
            Message: "Oracle connected successfully",
            UserCount: count));
    }
}
