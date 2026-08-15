using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using CampusDelivery.Api.Services.Interfaces;
using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Controllers
{
    // ⚠️ 核心操作：加上 [Authorize] 标签！
    // 它的作用是：没有登录（没有Cookie）的人如果想强行访问网址进入这个控制器，会被系统直接踹回登录页！这就完成了“权限判断”。
    [Authorize]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // 1. 查看个人信息
        [HttpGet]
        public IActionResult Profile()
        {
            // 从 Cookie 中读取当前登录的账号名
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            UserViewModel? model = _userService.GetProfile(username);
            if (model == null) return NotFound();

            return View(model);
        }
        // 2. 跳转到修改信息页面 (GET)
        [HttpGet]
        public IActionResult Edit()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            UserViewModel? model = _userService.GetProfile(username);
            if (model == null) return NotFound();

            return View(model);
        }

        // 3. 接收用户提交的新手机号 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserViewModel model)
        {
            // 剔除不需要验证的字段，因为页面上账号和角色是禁止编辑的
            ModelState.Remove("Username");
            ModelState.Remove("UserRole");

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            UserViewModel? currentUser = _userService.GetProfile(username);
            if (currentUser == null) return NotFound();

            if (!ModelState.IsValid)
            {
                UserViewModel retryModel = currentUser;
                retryModel.Phone = model.Phone;
                return View(retryModel);
            }

            var (success, errorMessage) = _userService.UpdatePhone(username, model.Phone);

            if (success)
            {
                // 更新成功，带着成功提示跳回个人中心看结果
                TempData["SuccessMessage"] = "手机号修改成功！";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(nameof(model.Phone), errorMessage);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAccount()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            var (success, errorMessage) = _userService.CancelOwnAccount(username);
            if (!success)
            {
                TempData["SuccessMessage"] = errorMessage;
                return RedirectToAction(nameof(Profile));
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "账号已注销，历史数据会被保留。";
            return RedirectToAction("Login", "Auth");
        }

    }
}
