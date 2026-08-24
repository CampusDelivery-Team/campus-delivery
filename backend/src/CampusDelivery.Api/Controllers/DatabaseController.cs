using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class DatabaseController(
    IDatabaseService databaseService) : Controller
{
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        return View(await databaseService.GetStatusAsync(cancellationToken));
    }
}
