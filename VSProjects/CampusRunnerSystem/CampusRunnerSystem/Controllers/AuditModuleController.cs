using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class AuditModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "审计模块";
        ViewBag.Tables = "audit_logs、audit_status_log_checks、audit_payment_checks、audit_refund_checks";
        ViewBag.Todo = "TODO：实现状态日志、支付记录、退款记录的抽查审计与异常标记。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
