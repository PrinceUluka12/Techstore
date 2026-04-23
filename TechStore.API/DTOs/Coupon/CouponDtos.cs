namespace TechStore.API.DTOs.Coupon;

using TechStore.API.Models;

public record CouponDto(
    int Id,
    string Code,
    string? Description,
    DiscountType Type,
    decimal Value,
    decimal? MinimumOrderAmount,
    decimal? MaximumDiscountAmount,
    int? UsageLimit,
    int UsedCount,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CouponListDto(
    int Id,
    string Code,
    DiscountType Type,
    decimal Value,
    decimal? MinimumOrderAmount,
    int? UsageLimit,
    int UsedCount,
    bool IsActive,
    DateTime? ValidTo
);

public record CreateCouponRequest(
    string Code,
    string? Description,
    DiscountType Type,
    decimal Value,
    decimal? MinimumOrderAmount = null,
    decimal? MaximumDiscountAmount = null,
    int? UsageLimit = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    bool IsActive = true
);

public record UpdateCouponRequest(
    string? Code = null,
    string? Description = null,
    DiscountType? Type = null,
    decimal? Value = null,
    decimal? MinimumOrderAmount = null,
    decimal? MaximumDiscountAmount = null,
    int? UsageLimit = null,
    DateTime? ValidFrom = null,
    DateTime? ValidTo = null,
    bool? IsActive = null
);

/// <summary>
/// Minimal public-facing coupon info for unauthenticated users.
/// Does not expose usage counts, limits, or internal amounts.
/// </summary>
public record PublicCouponDto(
    string Code,
    string? Description,
    DiscountType Type,
    bool IsValid
);

public record ValidateCouponRequest(string Code, decimal SubTotal);

public record ValidateCouponResponse(
    bool IsValid,
    string? ErrorMessage,
    decimal DiscountAmount,
    decimal FinalTotal
);
