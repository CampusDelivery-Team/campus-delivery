using CampusRunnerSystem.Filters;
using CampusRunnerSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize(SystemConstants.Roles.Runner)]
public class RunnerDashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
