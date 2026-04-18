namespace TechStore.API.DTOs.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Phone = null
);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt
);

public record UserProfileDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    string Role,
    DateTime CreatedAt
);

public record RefreshRequest(string RefreshToken);
