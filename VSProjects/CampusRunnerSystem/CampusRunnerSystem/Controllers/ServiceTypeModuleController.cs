using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class ServiceTypeModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "服务类型与节点规则模块";
        ViewBag.Tables = "service_types、service_node_rules";
        ViewBag.Todo = "TODO：实现服务类型维护、基础价格规则、服务类型可用节点配置。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
