using TechStore.API.DTOs.Auth;
using TechStore.API.DTOs.Role;
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
    private readonly IEmailService _email;
    private readonly IRoleService _roleService;
    private readonly IRoleRepository _roles;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IJwtHelper jwt,
        IConfiguration config,
        IEmailService email,
        IRoleService roleService,
        IRoleRepository roles,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _config = config;
        _email = email;
        _roleService = roleService;
        _roles = roles;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await _users.EmailExistsAsync(req.Email))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            Email        = req.Email,
            Phone        = req.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            Role         = "Customer"
        };

        await _users.CreateAsync(user);
        _logger.LogInformation("New user registered: {Email}", user.Email);
        _ = _email.SendWelcomeAsync(user.Email, user.FirstName);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email);

        // Generic error prevents user enumeration
        if (user == null)
        {
            _logger.LogWarning("Login attempt for unknown email: {Email}", req.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Check lockout before touching the password
        if (user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
        {
            _logger.LogWarning("Locked-out login attempt: {Email}", user.Email);
            throw new UnauthorizedAccessException(
                $"Account locked due to too many failed attempts. Try again after {user.LockoutUntil:HH:mm} UTC.");
        }

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            var maxAttempts    = int.Parse(_config["Auth:MaxFailedAttempts"] ?? "5");
            var lockoutMinutes = int.Parse(_config["Auth:LockoutMinutes"] ?? "15");

            user.FailedLoginAttempts++;
            _logger.LogWarning("Failed login {Count}/{Max}: {Email}",
                user.FailedLoginAttempts, maxAttempts, user.Email);

            if (user.FailedLoginAttempts >= maxAttempts)
            {
                user.LockoutUntil        = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                user.FailedLoginAttempts = 0;
                _logger.LogWarning("Account locked: {Email} until {Until}", user.Email, user.LockoutUntil);
            }

            await _users.UpdateAsync(user);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        user.FailedLoginAttempts = 0;
        user.LockoutUntil        = null;
        await _users.UpdateAsync(user);

        _logger.LogInformation("Successful login: {Email}", user.Email);
        return await IssueTokensAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token)
    {
        var stored = await _refreshTokens.GetByTokenAsync(token);

        if (stored == null)
        {
            _logger.LogWarning("Refresh attempt with unknown token");
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Reuse detection: revoked token presented again = stolen token family
        if (stored.IsRevoked)
        {
            _logger.LogWarning("Refresh token reuse detected for user {UserId} — revoking all sessions", stored.UserId);
            await _refreshTokens.RevokeAllUserTokensAsync(stored.UserId);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (stored.IsExpired)
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

        var user = stored.User;
        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var newRefreshToken = BuildRefreshToken(user.Id);
        await _refreshTokens.CreateAsync(newRefreshToken);
        await _refreshTokens.RevokeAsync(stored, newRefreshToken.Token);

        return BuildAuthResponse(user, newRefreshToken);
    }

    public async Task LogoutAsync(int userId, string? refreshToken = null)
    {
        if (refreshToken != null)
        {
            var stored = await _refreshTokens.GetByTokenAsync(refreshToken);
            if (stored != null && stored.UserId == userId && stored.IsActive)
                await _refreshTokens.RevokeAsync(stored);
        }
        else
        {
            await _refreshTokens.RevokeAllUserTokensAsync(userId);
        }
        _logger.LogInformation("User {UserId} logged out", userId);
    }

    public async Task LogoutEverywhereAsync(int userId)
    {
        await _refreshTokens.RevokeAllUserTokensAsync(userId);
        _logger.LogInformation("User {UserId} logged out from all devices", userId);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        await _users.UpdateAsync(user);

        // Revoke all sessions so any stolen sessions are killed immediately
        await _refreshTokens.RevokeAllUserTokensAsync(userId);
        _logger.LogInformation("Password changed for user {UserId} — all sessions revoked", userId);
    }

    public async Task PromoteToAdminAsync(int targetUserId)
    {
        var user = await _users.GetByIdAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == "Admin")
            throw new InvalidOperationException("User is already an Admin.");

        user.Role = "Admin";
        await _users.UpdateAsync(user);
        _logger.LogInformation("User {UserId} promoted to Admin", targetUserId);
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
        if (req.LastName  != null) user.LastName  = req.LastName;
        if (req.Phone     != null) user.Phone     = req.Phone;
        if (req.Address   != null) user.Address   = req.Address;

        await _users.UpdateAsync(user);
        return MapToProfile(user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var refreshToken = BuildRefreshToken(user.Id);
        await _refreshTokens.CreateAsync(refreshToken);
        return BuildAuthResponse(user, refreshToken);
    }

    private AuthResponse BuildAuthResponse(User user, RefreshToken refreshToken)
    {
        var expiryMinutes = double.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
        var permissions   = _roleService.GetPermissionsForRoleAsync(user.Role).GetAwaiter().GetResult();
        return new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.Role,
            _jwt.GenerateAccessToken(user, permissions),
            refreshToken.Token,
            DateTime.UtcNow.AddMinutes(expiryMinutes),
            permissions
        );
    }

    private RefreshToken BuildRefreshToken(int userId)
    {
        var expiryDays = double.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");
        return new RefreshToken
        {
            Token     = _jwt.GenerateRefreshToken(),
            UserId    = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        };
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _users.GetByEmailAsync(email);
        // Always return success to prevent email enumeration
        if (user == null || !user.IsActive) return;

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        user.PasswordResetToken       = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await _users.UpdateAsync(user);

        var baseUrl = _config["Email:PasswordResetBaseUrl"] ?? "https://www.pgusolutions.com/reset-password";
        var link = $"{baseUrl}?token={token}";
        _ = _email.SendPasswordResetAsync(user.Email, user.FirstName, link);
        _logger.LogInformation("Password reset token issued for {Email}", email);
    }

    public async Task ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _users.GetByResetTokenAsync(token)
            ?? throw new InvalidOperationException("Invalid or expired reset link.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Reset link has expired. Please request a new one.");

        user.PasswordHash             = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        user.PasswordResetToken       = null;
        user.PasswordResetTokenExpiry = null;
        await _users.UpdateAsync(user);

        // Invalidate all sessions
        await _refreshTokens.RevokeAllUserTokensAsync(user.Id);
        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
    }

    public async Task<UserProfileDto> CreateStaffAccountAsync(CreateStaffRequest req)
    {
        if (!await _roles.ExistsAsync(req.Role))
            throw new InvalidOperationException($"Role '{req.Role}' does not exist.");

        if (await _users.EmailExistsAsync(req.Email))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 12),
            Role         = req.Role,
        };

        await _users.CreateAsync(user);
        _logger.LogInformation("Staff account created: {Email} with role {Role}", user.Email, user.Role);
        return MapToProfile(user);
    }

    public async Task AssignRoleAsync(int targetUserId, string role)
    {
        if (!await _roles.ExistsAsync(role))
            throw new InvalidOperationException($"Role '{role}' does not exist.");

        var user = await _users.GetByIdAsync(targetUserId)
            ?? throw new KeyNotFoundException("User not found.");

        user.Role = role;
        await _users.UpdateAsync(user);

        // Revoke all sessions so the new role takes effect immediately
        await _refreshTokens.RevokeAllUserTokensAsync(targetUserId);
        _logger.LogInformation("Role for user {UserId} changed to {Role}", targetUserId, role);
    }

    private static UserProfileDto MapToProfile(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.Address, u.Role, u.CreatedAt);
}
