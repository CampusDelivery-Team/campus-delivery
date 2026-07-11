using Microsoft.AspNetCore.Mvc;
using CampusDelivery.Api.Presentation.ViewModels;
using CampusDelivery.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace CampusDelivery.Api.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserService _userService;

        // 接待员一上班，系统就会自动把 UserService 派发给它
        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        // 1. 用户在浏览器输入网址时，返回登录页面
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // 2. 用户点击“登录”按钮后，接收表单数据
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // 如果用户没填账号或密码，直接打回重填
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 调用前面写的核心逻辑
            var (success, errorMessage, user) = _userService.Login(model.Username, model.Password);

            if (!success)
            {
                // 登录失败，把错误信息显示在页面上
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }

            // 登录成功！给用户发一张“通行证” (Cookie 认证)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user!.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.UserRole)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // 登录成功后跳转到项目首页
            return RedirectToAction("Index", "Home");
        }
        // 3. 用户点击“注册”链接时，返回注册页面 (GET)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // 4. 用户填完表单点击“提交注册”时，接收数据 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            // 如果表单没填对（比如两次密码不一致、手机号格式不对），打回重填
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 把网页传来的 ViewModel 转换成底层的 User Model
            var newUser = new CampusDelivery.Api.Models.User
            {
                Username = model.Username,
                Phone = model.Phone,
                PasswordHash = model.Password, // 根据文档，目前暂存明文
                UserRole = "USER",             // 新注册的默认是普通用户
                AccountStatus = "NORMAL"       // 状态正常
            };

            // 调用 Service 层的注册逻辑
            var (success, errorMessage) = _userService.Register(newUser);

            if (!success)
            {
                // 注册失败，把错误信息显示在页面上（比如账号已存在）
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(model);
            }

            // 注册成功！利用 TempData 存一条消息，然后跳转到登录页让用户登录
            TempData["SuccessMessage"] = "账号注册成功，请登录！";
            return RedirectToAction("Login");
        }
        // 5. 退出登录 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // 清除系统发放的 Cookie 通行证
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 提示退出成功，并跳转回登录页
            TempData["SuccessMessage"] = "您已安全退出系统";
            return RedirectToAction("Login");
        }
    }
}
