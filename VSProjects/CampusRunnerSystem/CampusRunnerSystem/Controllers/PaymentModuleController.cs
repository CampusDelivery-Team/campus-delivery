using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class PaymentModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "支付模块";
        ViewBag.Tables = "payments";
        ViewBag.Todo = "TODO：实现支付记录查询、支付状态维护、微信/支付宝/现金枚举值展示。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
