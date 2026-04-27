namespace TechStore.API.DTOs.Role;

public record RoleDto(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    List<string> Permissions,
    DateTime CreatedAt
);

public record CreateRoleRequest(
    string Name,
    string? Description,
    List<string> Permissions
);

public record UpdateRoleRequest(
    string? Description,
    List<string> Permissions
);

public record PermissionInfoDto(string Key, string Label);
