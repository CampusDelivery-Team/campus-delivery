using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class SettlementModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "结算模块";
        ViewBag.Tables = "settlements、settlement_payment_items";
        ViewBag.Todo = "TODO：实现跑腿员收入结算、支付记录归集和结算状态维护。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
