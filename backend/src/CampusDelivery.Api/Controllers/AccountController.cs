using CampusDelivery.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusDelivery.Api.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class AccountController(IUserService userService) : Controller
{
    public IActionResult Index()
    {
        return View(userService.GetAccountManagement());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Block(int id)
    {
        SetMessage(() => userService.BlockAccount(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Unblock(int id)
    {
        SetMessage(() => userService.UnblockAccount(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(int id)
    {
        SetMessage(() => userService.CancelAccount(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RevokeRunner(int id)
    {
        SetMessage(() => userService.RevokeRunnerQualification(id));
        return RedirectToAction(nameof(Index));
    }

    private void SetMessage(Func<UserAccountOperationResult> action)
    {
        UserAccountOperationResult result = action();
        TempData["AccountMessage"] = result.Message;
    }
}
