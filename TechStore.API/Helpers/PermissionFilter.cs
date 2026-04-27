using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechStore.API.Helpers;

/// <summary>
/// Allows access if the user is an Admin OR holds the specified permission claim.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission) : base(typeof(PermissionFilter))
    {
        Arguments = [permission];
    }
}

public class PermissionFilter(string permission) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext ctx)
    {
        var user = ctx.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) { ctx.Result = new UnauthorizedResult(); return; }
        if (user.IsInRole("Admin")) return;

        var perms = user.FindFirst("permissions")?.Value?.Split(',') ?? [];
        if (!perms.Contains(permission))
            ctx.Result = new ForbidResult();
    }
}
