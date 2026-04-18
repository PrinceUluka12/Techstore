using TechStore.API.DTOs.Auth;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IJwtHelper _jwt;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IJwtHelper jwt, IConfiguration config)
    {
        _users = users;
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
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _users.GetByEmailAsync(req.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        return BuildAuthResponse(user);
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

    private AuthResponse BuildAuthResponse(User user)
    {
        var expiryHours = double.Parse(_config["Jwt:ExpiryHours"] ?? "24");
        return new AuthResponse(
            user.Id, user.Email, user.FirstName, user.LastName, user.Role,
            _jwt.GenerateAccessToken(user),
            _jwt.GenerateRefreshToken(),
            DateTime.UtcNow.AddHours(expiryHours)
        );
    }

    private static UserProfileDto MapToProfile(User u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Phone, u.Address, u.Role, u.CreatedAt);
}
