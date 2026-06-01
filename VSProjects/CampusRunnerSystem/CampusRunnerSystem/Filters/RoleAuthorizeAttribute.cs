using CampusRunnerSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CampusRunnerSystem.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleAuthorizeAttribute : Attribute, IActionFilter
{
    private readonly string[] _roles;

    public RoleAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userRole = session.GetString("UserRole");

        if (string.IsNullOrWhiteSpace(userRole))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(userRole))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
