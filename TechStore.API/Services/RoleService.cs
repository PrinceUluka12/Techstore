using TechStore.API.DTOs.Role;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class RoleService(IRoleRepository roles) : IRoleService
{
    public async Task<IEnumerable<RoleDto>> GetAllAsync() =>
        (await roles.GetAllAsync()).Select(Map);

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        var role = await roles.GetByIdAsync(id);
        return role == null ? null : Map(role);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest req)
    {
        if (await roles.ExistsAsync(req.Name))
            throw new InvalidOperationException($"A role named '{req.Name}' already exists.");

        var invalidPerms = req.Permissions.Except(Perms.All).ToList();
        if (invalidPerms.Count > 0)
            throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", invalidPerms)}");

        var role = new AppRole
        {
            Name        = req.Name.Trim(),
            Description = req.Description?.Trim(),
            IsSystem    = false,
            Permissions = req.Permissions.Distinct().ToList(),
        };

        return Map(await roles.CreateAsync(role));
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest req)
    {
        var role = await roles.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be modified.");

        var invalidPerms = req.Permissions.Except(Perms.All).ToList();
        if (invalidPerms.Count > 0)
            throw new InvalidOperationException($"Unknown permissions: {string.Join(", ", invalidPerms)}");

        role.Description = req.Description?.Trim();
        role.Permissions = req.Permissions.Distinct().ToList();

        return Map(await roles.UpdateAsync(role));
    }

    public async Task DeleteAsync(int id)
    {
        var role = await roles.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Role not found.");

        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

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

    private static RoleDto Map(AppRole r) =>
        new(r.Id, r.Name, r.Description, r.IsSystem, r.Permissions, r.CreatedAt);
}
