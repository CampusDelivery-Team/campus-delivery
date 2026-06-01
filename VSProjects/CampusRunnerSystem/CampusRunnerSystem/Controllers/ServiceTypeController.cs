using CampusRunnerSystem.Filters;
using CampusRunnerSystem.Models;
using CampusRunnerSystem.Services;
using CampusRunnerSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize(SystemConstants.Roles.Admin)]
public class ServiceTypeController : Controller
{
    private readonly IServiceTypeService _serviceTypeService;

    public ServiceTypeController(IServiceTypeService serviceTypeService)
    {
        _serviceTypeService = serviceTypeService;
    }

    public IActionResult Index()
    {
        var result = _serviceTypeService.GetAllServiceTypes();
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(new List<ServiceTypeViewModel>());
        }

        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        ViewBag.ErrorMessage = TempData["ErrorMessage"];
        return View(result.Data ?? new List<ServiceTypeViewModel>());
    }

    public IActionResult Details(int id)
    {
        var result = _serviceTypeService.GetServiceTypeById(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    public IActionResult Create()
    {
        return View(new ServiceTypeViewModel { TypeStatus = SystemConstants.ServiceTypeStatus.Enabled });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(ServiceTypeViewModel serviceType)
    {
        var result = _serviceTypeService.AddServiceType(serviceType);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(serviceType);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var result = _serviceTypeService.GetServiceTypeById(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ServiceTypeViewModel serviceType)
    {
        var result = _serviceTypeService.UpdateServiceType(serviceType);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(serviceType);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var result = _serviceTypeService.GetServiceTypeById(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int serviceTypeId)
    {
        var result = _serviceTypeService.DeleteServiceType(serviceTypeId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Delete), new { id = serviceTypeId });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
