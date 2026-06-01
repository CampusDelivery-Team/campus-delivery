using CampusRunnerSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize]
public class ReviewModuleController : Controller
{
    public IActionResult Index()
    {
        ViewBag.ModuleName = "评价模块";
        ViewBag.Tables = "reviews";
        ViewBag.Todo = "TODO：实现服务评价、评分、匿名标志和跑腿员信誉分联动。";
        return View("~/Views/ModuleTodo/Index.cshtml");
    }
}
