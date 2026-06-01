using CampusRunnerSystem.Filters;
using CampusRunnerSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize(SystemConstants.Roles.User)]
public class UserDashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
