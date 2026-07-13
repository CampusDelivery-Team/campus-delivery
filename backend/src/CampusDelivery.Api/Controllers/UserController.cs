using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Oracle.ManagedDataAccess.Client;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Services;
using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Controllers
{
    // ⚠️ 核心操作：加上 [Authorize] 标签！
    // 它的作用是：没有登录（没有Cookie）的人如果想强行访问网址进入这个控制器，会被系统直接踹回登录页！这就完成了“权限判断”。
    [Authorize]
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly UserService _userService;

        public UserController(UserRepository userRepository, UserService userService)
        {
            _userRepository = userRepository;
            _userService = userService;
        }

        // 1. 查看个人信息
        [HttpGet]
        public IActionResult Profile()
        {
            // 从 Cookie 中读取当前登录的账号名
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            // 去数据库查这个人的完整信息
            var user = _userRepository.GetUserByUsername(username);
            if (user == null) return NotFound();

            return View(BuildUserViewModel(user));
        }
        // 2. 跳转到修改信息页面 (GET)
        [HttpGet]
        public IActionResult Edit()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            var user = _userRepository.GetUserByUsername(username);
            if (user == null) return NotFound();

            return View(BuildUserViewModel(user));
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

            var currentUser = _userRepository.GetUserByUsername(username);
            if (currentUser == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var retryModel = BuildUserViewModel(currentUser);
                retryModel.Phone = model.Phone;
                return View(retryModel);
            }

            var (success, errorMessage) = _userService.UpdatePhone(currentUser.UserId, model.Phone);

            if (success)
            {
                // 更新成功，带着成功提示跳回个人中心看结果
                TempData["SuccessMessage"] = "手机号修改成功！";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAccount()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            var currentUser = _userRepository.GetUserByUsername(username);
            if (currentUser == null) return NotFound();

            (bool success, string errorMessage) result;
            try
            {
                result = _userService.CancelOwnAccount(currentUser.UserId);
            }
            catch (OracleException exception) when (exception.Number == 2290)
            {
                TempData["SuccessMessage"] = "数据库账号状态尚未升级，请联系管理员执行账号状态迁移。";
                return RedirectToAction(nameof(Profile));
            }

            var (success, errorMessage) = result;
            if (!success)
            {
                TempData["SuccessMessage"] = errorMessage;
                return RedirectToAction(nameof(Profile));
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "账号已注销，历史数据会被保留。";
            return RedirectToAction("Login", "Auth");
        }

        private UserViewModel BuildUserViewModel(Models.User user)
        {
            var address = _userRepository.GetPrimaryAddress(user.UserId);

            return new UserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Phone = user.Phone,
                UserRole = _userService.GetChineseRoleName(user.UserRole),
                HasAddress = address != null,
                AddressSummary = address == null
                    ? "暂未设置常用地址"
                    : $"{address.Campus} · {address.BuildingRoom}",
                AddressContact = address == null
                    ? "后续可在地址管理中新增收货地址"
                    : $"{address.ContactName} · {address.ContactPhone}"
            };
        }
    }
}
