using TechStore.API.DTOs.Role;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Staff who manage roles or users look after the people below them: they can't hand out a
// permission they don't hold themselves, change their own access, or change an Admin.
// Admins can do all of it, except change their own role or switch their own account off.
public class RoleService(IRoleRepository roles, IUserRepository users) : IRoleService
{
    public async Task<IEnumerable<RoleDto>> GetAllAsync() =>
        (await roles.GetAllAsync()).Select(Map);

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await roles.GetByIdAsync(id);
        return role == null ? null : Map(role);
    }

    public async Task<RoleDto> CreateAsync(int actorId, CreateRoleRequest req)
    {
        if (await roles.ExistsAsync(req.Name))
            throw new InvalidOperationException($"A role named '{req.Name}' already exists.");

        var invalidPerms = req.Permissions.Except(Perms.All).ToList();
        if (invalidPerms.Count > 0)
            throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", invalidPerms)}");

        var actor = await GetActorAsync(actorId);
        await EnsureHoldsAsync(actor, req.Permissions);

        var role = new AppRole
        {
            Name        = req.Name.Trim(),
            Description = req.Description?.Trim(),
            IsSystem    = false,
            Permissions = req.Permissions.Distinct().ToList(),
        };

        return Map(await roles.CreateAsync(role));
    }

    public async Task<RoleDto> UpdateAsync(int actorId, int id, UpdateRoleRequest req)
    {
        var role = await roles.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be modified.");

        var invalidPerms = req.Permissions.Except(Perms.All).ToList();
        if (invalidPerms.Count > 0)
            throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", invalidPerms)}");

        var actor = await GetActorAsync(actorId);
        if (actor.Role != "Admin")
        {
            if (IsActorsRole(actor, role))
                throw new ForbiddenException("You can't change your own role's permissions. Ask an Admin.");
            await EnsureRoleNotAboveAsync(actor, role, "change it");
            await EnsureHoldsAsync(actor, req.Permissions);
        }

        role.Description = req.Description?.Trim();
        role.Permissions = req.Permissions.Distinct().ToList();

        return Map(await roles.UpdateAsync(role));
    }

    public async Task DeleteAsync(int actorId, int id)
    {
        var role = await roles.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

        var actor = await GetActorAsync(actorId);
        if (actor.Role != "Admin")
        {
            if (IsActorsRole(actor, role))
                throw new ForbiddenException("You can't delete your own role. Ask an Admin.");
            await EnsureRoleNotAboveAsync(actor, role, "delete it");
        }

        await roles.DeleteAsync(id);
    }

    public Task<IEnumerable<PermissionInfoDto>> GetAvailablePermissionsAsync() =>
        Task.FromResult<IEnumerable<PermissionInfoDto>>(
            Perms.All.Select(p => new PermissionInfoDto(p, Perms.Labels[p])));

    public async Task<IEnumerable<string>> GetPermissionsForRoleAsync(string roleName)
    {
        var role = await roles.GetByNameAsync(roleName);
        if (roleName == "Admin") return Perms.All;
        return role?.Permissions ?? [];
    }

    public async Task EnsureCanManageUserAsync(int actorId, User target)
    {
        var actor = await GetActorAsync(actorId);
        if (actor.Role == "Admin") return;

        if (target.Id == actor.Id)
            throw new ForbiddenException("You can't change your own account. Ask an Admin.");
        if (target.Role == "Admin")
            throw new ForbiddenException("Only an Admin can change an Admin's account.");

        var missing = await MissingAsync(actor, await GetPermissionsForRoleAsync(target.Role));
        if (missing.Count > 0)
            throw new ForbiddenException(
                $"{target.FirstName} {target.LastName} has permissions you don't have ({Describe(missing)}), " +
                "so only an Admin can change their account.");
    }

    public async Task EnsureCanGiveRoleAsync(int actorId, string role)
    {
        var actor = await GetActorAsync(actorId);
        if (actor.Role == "Admin") return;

        if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("Only an Admin can give someone the Admin role.");

        var missing = await MissingAsync(actor, await GetPermissionsForRoleAsync(role));
        if (missing.Count > 0)
            throw new ForbiddenException(
                $"The {role} role has permissions you don't have ({Describe(missing)}), so only an Admin can give it out.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<User> GetActorAsync(int actorId) =>
        await users.GetByIdAsync(actorId)
            ?? throw new UnauthorizedAccessException("Please sign in again.");

    private static bool IsActorsRole(User actor, AppRole role) =>
        string.Equals(actor.Role, role.Name, StringComparison.OrdinalIgnoreCase);

    // The permissions the actor doesn't hold (none for an Admin)
    private async Task<List<string>> MissingAsync(User actor, IEnumerable<string> permissions)
    {
        if (actor.Role == "Admin") return [];
        var held = (await GetPermissionsForRoleAsync(actor.Role)).ToHashSet();
        return permissions.Where(p => !held.Contains(p)).Distinct().ToList();
    }

    private async Task EnsureHoldsAsync(User actor, IEnumerable<string> permissions)
    {
        var missing = await MissingAsync(actor, permissions);
        if (missing.Count > 0)
            throw new ForbiddenException($"You can't give a role permissions you don't have: {Describe(missing)}.");
    }

    private async Task EnsureRoleNotAboveAsync(User actor, AppRole role, string action)
    {
        var missing = await MissingAsync(actor, role.Permissions);
        if (missing.Count > 0)
            throw new ForbiddenException(
                $"The {role.Name} role has permissions you don't have ({Describe(missing)}), so only an Admin can {action}.");
    }

    private static string Describe(IEnumerable<string> permissions) =>
        string.Join(", ", permissions.Select(p => Perms.Labels.GetValueOrDefault(p, p)));

    private static RoleDto Map(AppRole r) =>
        new(r.Id, r.Name, r.Description, r.IsSystem, r.Permissions, r.CreatedAt);
}
