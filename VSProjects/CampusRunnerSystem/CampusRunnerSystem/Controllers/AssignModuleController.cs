using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class AssignModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "接单派单模块";
        ViewBag.Tables = "assign_records";
        ViewBag.Todo = "TODO：实现自主接单、管理员派单、重派单，并联动 tasks 和 runners 状态。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
