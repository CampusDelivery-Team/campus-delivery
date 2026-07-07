using CampusDelivery.Api.Persistence.Oracle;
using CampusDelivery.Api.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

public sealed class DatabaseController(OracleConnectionFactory connectionFactory) : Controller
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
            model.Message = ex.Message;
        }

        return View(model);
    }
}
