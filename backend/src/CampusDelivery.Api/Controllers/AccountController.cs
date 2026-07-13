using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class AccountController(UserService userService) : Controller
{
    public IActionResult Index()
    {
        return View(userService.GetAccountManagement());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Block(int id)
    {
        SetMessage(() => userService.BlockAccount(id), "账号已封控", "账号无法封控，可能已被处理或不是可管理账号");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Unblock(int id)
    {
        SetMessage(() => userService.UnblockAccount(id), "账号已解除封控", "账号无法解除封控");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(int id)
    {
        SetMessage(() => userService.CancelAccount(id), "账号已注销", "账号无法注销，可能已被处理或不是可管理账号");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RevokeRunner(int id)
    {
        SetMessage(() => userService.RevokeRunnerQualification(id), "已撤销跑腿员资格，账号已降为普通用户", "无法撤销跑腿员资格");
        return RedirectToAction(nameof(Index));
    }

    private void SetMessage(Func<bool> action, string successMessage, string failureMessage)
    {
        try
        {
            TempData["AccountMessage"] = action() ? successMessage : failureMessage;
        }
        catch (OracleException exception) when (exception.Number == 2290)
        {
            TempData["AccountMessage"] = "数据库账号状态尚未升级，请先执行 003_add_account_lifecycle.sql。";
        }
    }
}
