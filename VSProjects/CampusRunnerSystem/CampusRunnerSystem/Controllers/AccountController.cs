using CampusRunnerSystem.Models;
using CampusRunnerSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(string username, string password)
    {
        var result = _accountService.Login(username, password);
        if (!result.Success || result.Data == null)
        {
            ViewBag.ErrorMessage = result.Message;
            return View();
        }

        HttpContext.Session.SetInt32("UserId", result.Data.UserId);
        HttpContext.Session.SetString("Username", result.Data.Username);
        HttpContext.Session.SetString("UserRole", result.Data.UserRole);

        return result.Data.UserRole switch
        {
            SystemConstants.Roles.User => RedirectToAction("Index", "UserDashboard"),
            SystemConstants.Roles.Runner => RedirectToAction("Index", "RunnerDashboard"),
            SystemConstants.Roles.Admin => RedirectToAction("Index", "AdminDashboard"),
            _ => RedirectToAction("AccessDenied")
        };
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(string username)
    {
        ViewBag.Message = $"账号 {username} 的注册流程已预留，后续由账号模块同学接入 users 表写入逻辑。";
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
