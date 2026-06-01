using CampusRunnerSystem.Filters;
using CampusRunnerSystem.Models;
using CampusRunnerSystem.Services;
using CampusRunnerSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CampusRunnerSystem.Controllers;

[RoleAuthorize(SystemConstants.Roles.Admin)]
public class NodeController : Controller
{
    private readonly INodeService _nodeService;

    public NodeController(INodeService nodeService)
    {
        _nodeService = nodeService;
    }

    public IActionResult Index()
    {
        var result = _nodeService.GetAllNodes();
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(new List<NodeViewModel>());
        }

        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        ViewBag.ErrorMessage = TempData["ErrorMessage"];
        return View(result.Data ?? new List<NodeViewModel>());
    }

    public IActionResult Details(int id)
    {
        var result = _nodeService.GetNodeById(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    public IActionResult Create()
    {
        return View(new NodeViewModel { NodeStatus = SystemConstants.NodeStatus.Normal });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(NodeViewModel node)
    {
        var result = _nodeService.AddNode(node);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(node);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var result = _nodeService.GetNodeById(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(NodeViewModel node)
    {
        var result = _nodeService.UpdateNode(node);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(node);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var result = _nodeService.GetNodeById(id);
        if (!result.Success || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int nodeId)
    {
        var result = _nodeService.DeleteNode(nodeId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Delete), new { id = nodeId });
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
