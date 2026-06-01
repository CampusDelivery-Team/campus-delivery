using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class StatusLogModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "任务状态日志模块";
        ViewBag.Tables = "task_status_logs";
        ViewBag.Todo = "TODO：实现任务状态变更记录，保存变更前后状态、操作人和备注。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
