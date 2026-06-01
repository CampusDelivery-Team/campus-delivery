using CampusRunnerSystem.Filters;
using CampusRunnerSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize(SystemConstants.Roles.Admin)]
public class AdminDashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
