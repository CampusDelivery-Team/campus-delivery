using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

            // 打包成 ViewModel 传给前端页面
            var model = new UserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Phone = user.Phone,
                UserRole = _userService.GetChineseRoleName(user.UserRole) // 调用之前写的翻译方法，把 USER 变成 "普通用户"
            };

            return View(model);
        }
        // 2. 跳转到修改信息页面 (GET)
        [HttpGet]
        public IActionResult Edit()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Auth");

            var user = _userRepository.GetUserByUsername(username);
            if (user == null) return NotFound();

            var model = new UserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Phone = user.Phone,
                UserRole = _userService.GetChineseRoleName(user.UserRole)
            };

            return View(model);
        }

        // 3. 接收用户提交的新手机号 (POST)
        [HttpPost]
        public IActionResult Edit(UserViewModel model)
        {
            // 剔除不需要验证的字段，因为页面上账号和角色是禁止编辑的
            ModelState.Remove("Username");
            ModelState.Remove("UserRole");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, errorMessage) = _userService.UpdatePhone(model.UserId, model.Phone);

            if (success)
            {
                // 更新成功，带着成功提示跳回个人中心看结果
                TempData["SuccessMessage"] = "手机号修改成功！";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
    }
}
