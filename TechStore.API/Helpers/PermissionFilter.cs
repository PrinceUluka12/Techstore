using System.Security.Claims;
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
        if (!user.HasPermission(permission))
            ctx.Result = new ForbidResult();
    }
}

public static class PermissionClaims
{
    /// <summary>Admins have every permission; other users have the ones in their token</summary>
    public static bool HasPermission(this ClaimsPrincipal user, string permission) =>
        user.IsInRole("Admin") || (user.FindFirst("permissions")?.Value?.Split(',') ?? []).Contains(permission);
}
