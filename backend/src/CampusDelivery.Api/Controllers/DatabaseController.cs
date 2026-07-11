using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class DatabaseController(
    OracleConnectionFactory connectionFactory,
    IHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var model = new DatabaseStatusViewModel();

        try
        {
            await using var connection = connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM users";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            model.IsConnected = true;
            model.UserCount = Convert.ToInt32(result);
            model.Message = "Oracle connected successfully.";
        }
        catch (Exception ex)
        {
            model.IsConnected = false;
            model.Message = environment.IsDevelopment()
                ? ex.Message
                : "数据库连接失败，请联系服务器负责人查看后端日志。";
        }

        return View(model);
    }
}
