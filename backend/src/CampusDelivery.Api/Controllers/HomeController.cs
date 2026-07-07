using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
