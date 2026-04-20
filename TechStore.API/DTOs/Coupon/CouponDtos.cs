using TechStore.API.Models;

namespace TechStore.API.DTOs.Coupon;

public record CouponValidationDto(
    bool IsValid,
    string? Message,
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal EstimatedDiscount,
    decimal? MinOrderAmount
);

public record CouponDto(
    int Id,
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime StartsAt,
    DateTime ExpiresAt,
    int? UsageLimit,
    int TimesUsed,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateCouponRequest(
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime StartsAt,
    DateTime ExpiresAt,
    int? UsageLimit
);

public record UpdateCouponRequest(
    string? Description,
    decimal? MinOrderAmount,
    decimal? MaxDiscountAmount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? UsageLimit,
    bool? IsActive
);
