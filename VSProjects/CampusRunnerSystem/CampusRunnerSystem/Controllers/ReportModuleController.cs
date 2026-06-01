using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class ReportModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "统计报表模块";
        ViewBag.Tables = "reports、report_audit_items";
        ViewBag.Todo = "TODO：实现订单、支付、投诉统计报表生成和审计项关联。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
