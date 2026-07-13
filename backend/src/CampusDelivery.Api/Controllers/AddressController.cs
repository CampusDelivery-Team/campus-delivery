using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CampusDelivery.Api.Services;
using CampusDelivery.Api.Repositories;
using CampusDelivery.Api.Models;
using CampusDelivery.Api.Presentation.ViewModels;

namespace CampusDelivery.Api.Controllers
{
    // 强制必须登录才能访问地址管理
    [Authorize]
    public class AddressController : Controller
    {
        private readonly AddressService _addressService;
        private readonly UserRepository _userRepository;

        public AddressController(AddressService addressService, UserRepository userRepository)
        {
            _addressService = addressService;
            _userRepository = userRepository;
        }

        // 辅助方法：获取当前登录用户的 UserId
        private int GetCurrentUserId()
        {
            var username = User.Identity?.Name;
            var user = _userRepository.GetUserByUsername(username!);
            return user?.UserId ?? 0;
        }

        // 1. 地址列表页
        [HttpGet]
        public IActionResult Index()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var addresses = _addressService.GetUserAddresses(userId);
            return View(addresses);
        }

        // 2. 新增地址页 (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AddressViewModel());
        }

        // 3. 提交新增地址 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AddressViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int userId = GetCurrentUserId();
            var newAddress = new UserAddress
            {
                UserId = userId,
                ContactName = model.ContactName,
                ContactPhone = model.ContactPhone,
                Campus = model.Campus,
                BuildingRoom = model.BuildingRoom,
                // Checkbox 没勾选时传过来的可能是 null，做个安全处理
                IsDefault = model.IsDefault == "Y" ? "Y" : "N"
            };

            var (success, error) = _addressService.AddAddress(newAddress);
            if (success)
            {
                TempData["SuccessMessage"] = "地址添加成功！";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        // 4. 修改地址页 (GET)
        [HttpGet]
        public IActionResult Edit(int id) // 这里的 id 就是 address_no
        {
            int userId = GetCurrentUserId();
            var address = _addressService.GetAddress(userId, id);

            if (address == null) return NotFound();

            var model = new AddressViewModel
            {
                UserId = address.UserId,
                AddressNo = address.AddressNo,
                ContactName = address.ContactName,
                ContactPhone = address.ContactPhone,
                Campus = address.Campus,
                BuildingRoom = address.BuildingRoom,
                IsDefault = address.IsDefault
            };

            return View(model);
        }

        // 5. 提交修改地址 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(AddressViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            int userId = GetCurrentUserId();
            var updateAddress = new UserAddress
            {
                UserId = userId,
                AddressNo = model.AddressNo,
                ContactName = model.ContactName,
                ContactPhone = model.ContactPhone,
                Campus = model.Campus,
                BuildingRoom = model.BuildingRoom,
                IsDefault = model.IsDefault == "Y" ? "Y" : "N"
            };

            var (success, error) = _addressService.UpdateAddress(updateAddress);
            if (success)
            {
                TempData["SuccessMessage"] = "地址修改成功！";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        // 6. 删除地址 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            int userId = GetCurrentUserId();
            _addressService.DeleteAddress(userId, id);
            TempData["SuccessMessage"] = "地址已删除！";
            return RedirectToAction("Index");
        }

        // 7. 设置为默认地址 (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetDefault(int id)
        {
            int userId = GetCurrentUserId();
            _addressService.SetDefault(userId, id);
            TempData["SuccessMessage"] = "默认地址设置成功！";
            return RedirectToAction("Index");
        }
    }
}
