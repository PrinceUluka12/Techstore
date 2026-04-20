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
        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        return await BuildAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token)
    {
        var storedToken = await _refreshTokens.GetByTokenAsync(token);

        if (storedToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!storedToken.IsActive)
        {
            if (storedToken.IsRevoked && storedToken.ReplacedByToken != null)
            {
                await _refreshTokens.RevokeAllUserTokensAsync(storedToken.UserId);
            }
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = storedToken.User;
        var newRefreshToken = GenerateRefreshToken(user.Id);
        await _refreshTokens.CreateAsync(newRefreshToken);
        await _refreshTokens.RevokeAsync(storedToken, newRefreshToken.Token);

        return BuildAuthResponse(user, newRefreshToken);
    }

    public async Task LogoutAsync(int userId, string? token = null)
    {
        if (token != null)
        {
            var storedToken = await _refreshTokens.GetByTokenAsync(token);
            if (storedToken != null && storedToken.UserId == userId && storedToken.IsActive)
            {
                await _refreshTokens.RevokeAsync(storedToken);
            }
        }
        else
        {
            await _refreshTokens.RevokeAllUserTokensAsync(userId);
        }
    }

    public async Task LogoutEverywhereAsync(int userId)
    {
        await _refreshTokens.RevokeAllUserTokensAsync(userId);
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

    private async Task<AuthResponse> BuildAuthResponse(User user)
    {
        var refreshToken = GenerateRefreshToken(user.Id);
        await _refreshTokens.CreateAsync(refreshToken);
        return BuildAuthResponse(user, refreshToken);
    }

    private AuthResponse BuildAuthResponse(User user, RefreshToken refreshToken)
    {
        var expiryMinutes = double.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        return new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.Role,
            _jwt.GenerateAccessToken(user),
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(expiryMinutes)
        );
    }

    private RefreshToken GenerateRefreshToken(int userId)
    {
        var expiryDays = double.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");
        return new RefreshToken
        {
            Token = _jwt.GenerateRefreshToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        };
    }

    private static UserProfileDto MapToProfile(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.Address, u.Role, u.CreatedAt);
}
