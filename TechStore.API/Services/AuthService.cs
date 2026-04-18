using TechStore.API.DTOs.Auth;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtHelper _jwt;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IRefreshTokenRepository refreshTokens, IJwtHelper jwt, IConfiguration config)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await _users.EmailExistsAsync(req.Email))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            Phone = req.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "Customer"
        };

        await _users.CreateAsync(user);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        return await IssueTokensAsync(user);
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        return user == null ? null : MapToProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (req.FirstName != null) user.FirstName = req.FirstName;
        if (req.LastName != null) user.LastName = req.LastName;
        if (req.Phone != null) user.Phone = req.Phone;
        if (req.Address != null) user.Address = req.Address;

        await _users.UpdateAsync(user);
        return MapToProfile(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var rt = await _refreshTokens.GetByTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (rt.IsRevoked && rt.ReplacedByToken != null)
        {
            if (rt.User != null)
                await _refreshTokens.RevokeAllActiveForUserAsync(rt.User.Id);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (!rt.IsActive)
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked. Please log in again.");

        var user = rt.User;
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        // Rotation: revoke current, create new
        rt.RevokedAt = DateTime.UtcNow;

        var accessExpiryHours = double.Parse(_config["Jwt:ExpiryHours"] ?? "24");
        var refreshExpiryDays = int.Parse(_config["Jwt:RefreshExpiryDays"] ?? "7");

        var newAccessToken = _jwt.GenerateAccessToken(user);
        var newRefreshToken = _jwt.GenerateRefreshToken();
        var newRefreshExpires = DateTime.UtcNow.AddDays(refreshExpiryDays);

        rt.ReplacedByToken = newRefreshToken;

        var newRt = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = newRefreshExpires
        };

        await _refreshTokens.UpdateAsync(rt);
        await _refreshTokens.CreateAsync(newRt);

        return new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.Role,
            newAccessToken, newRefreshToken,
            DateTime.UtcNow.AddHours(accessExpiryHours)
        );
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var accessExpiryHours = double.Parse(_config["Jwt:ExpiryHours"] ?? "24");
        var refreshExpiryDays = int.Parse(_config["Jwt:RefreshExpiryDays"] ?? "7");

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshToken = _jwt.GenerateRefreshToken();
        var refreshExpires = DateTime.UtcNow.AddDays(refreshExpiryDays);

        var rtEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshExpires
        };
        await _refreshTokens.CreateAsync(rtEntity);

        return new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.Role,
            accessToken, refreshToken,
            DateTime.UtcNow.AddHours(accessExpiryHours)
        );
    }

    private static UserProfileDto MapToProfile(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.Address, u.Role, u.CreatedAt);
}
