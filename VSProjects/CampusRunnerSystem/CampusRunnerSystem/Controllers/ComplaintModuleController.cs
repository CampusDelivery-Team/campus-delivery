using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class ComplaintModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "投诉模块";
        ViewBag.Tables = "complaints";
        ViewBag.Todo = "TODO：实现投诉提交、管理员处理和处理状态展示。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
