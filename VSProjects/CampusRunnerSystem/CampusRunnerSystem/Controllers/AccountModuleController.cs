using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class AccountModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "账号与地址模块";
        ViewBag.Tables = "users、user_addresses、runners";
        ViewBag.Todo = "TODO：实现用户资料、常用地址管理、跑腿员资料提交与管理员审核。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
