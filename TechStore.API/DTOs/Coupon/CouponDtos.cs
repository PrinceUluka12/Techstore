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
