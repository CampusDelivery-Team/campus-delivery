using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class RefundModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "退款模块";
        ViewBag.Tables = "refunds";
        ViewBag.Todo = "TODO：实现退款申请、审核、处理状态流转和关联支付记录。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
