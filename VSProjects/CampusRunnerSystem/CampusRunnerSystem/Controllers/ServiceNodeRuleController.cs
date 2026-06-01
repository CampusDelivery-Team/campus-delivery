using CampusRunnerSystem.Filters;
using CampusRunnerSystem.Models;
using CampusRunnerSystem.Services;
using CampusRunnerSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize(SystemConstants.Roles.Admin)]
public class ServiceNodeRuleController : Controller
{
    private readonly IServiceNodeRuleService _ruleService;
    private readonly IServiceTypeService _serviceTypeService;
    private readonly INodeService _nodeService;

    public ServiceNodeRuleController(
        IServiceNodeRuleService ruleService,
        IServiceTypeService serviceTypeService,
        INodeService nodeService)
    {
        _ruleService = ruleService;
        _serviceTypeService = serviceTypeService;
        _nodeService = nodeService;
    }

    public IActionResult Index()
    {
        LoadSelectLists();

        var result = _ruleService.GetAllRules();
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(new List<ServiceNodeRuleViewModel>());
        }

        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        ViewBag.ErrorMessage = TempData["ErrorMessage"];
        return View(result.Data ?? new List<ServiceNodeRuleViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(int serviceTypeId, int nodeId)
    {
        var result = _ruleService.AddRule(serviceTypeId, nodeId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int serviceTypeId, int nodeId)
    {
        var result = _ruleService.DeleteRule(serviceTypeId, nodeId);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private void LoadSelectLists()
    {
        var serviceTypes = _serviceTypeService.GetAllServiceTypes().Data ?? new List<ServiceTypeViewModel>();
        var nodes = _nodeService.GetAllNodes().Data ?? new List<NodeViewModel>();

        ViewBag.ServiceTypes = serviceTypes.Select(item => new SelectListItem
        {
            Value = item.ServiceTypeId.ToString(),
            Text = $"{item.ServiceName}（{item.TypeStatus}）"
        }).ToList();

        ViewBag.Nodes = nodes.Select(item => new SelectListItem
        {
            Value = item.NodeId.ToString(),
            Text = $"{item.NodeName}（{item.NodeType}）"
        }).ToList();
    }
}
