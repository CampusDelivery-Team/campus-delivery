using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class TaskModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "任务发布与查询模块";
        ViewBag.Tables = "tasks、food_delivery_details、express_pickup_details、private_task_details";
        ViewBag.Todo = "TODO：实现发布任务、任务明细、我的任务、任务大厅等功能。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
