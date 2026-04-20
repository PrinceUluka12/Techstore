using TechStore.API.DTOs.Coupon;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class CouponService : ICouponService
{
    private readonly ICouponRepository _coupons;

    public CouponService(ICouponRepository coupons)
    {
        _coupons = coupons;
    }

    public async Task<CouponValidationDto?> ValidateAsync(string code, decimal subtotal)
    {
        var coupon = await _coupons.GetByCodeAsync(code);
        if (coupon == null)
            return new CouponValidationDto(false, "Coupon not found.", code, default, 0, 0, null);

        if (!coupon.IsActive)
            return new CouponValidationDto(false, "Coupon is inactive.", code, coupon.DiscountType, coupon.DiscountValue, 0, coupon.MinOrderAmount);

        var now = DateTime.UtcNow;
        if (now < coupon.StartsAt)
            return new CouponValidationDto(false, "Coupon is not yet active.", code, coupon.DiscountType, coupon.DiscountValue, 0, coupon.MinOrderAmount);
        if (now > coupon.ExpiresAt)
            return new CouponValidationDto(false, "Coupon has expired.", code, coupon.DiscountType, coupon.DiscountValue, 0, coupon.MinOrderAmount);

        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
            return new CouponValidationDto(false, "Coupon usage limit reached.", code, coupon.DiscountType, coupon.DiscountValue, 0, coupon.MinOrderAmount);

        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
            return new CouponValidationDto(false, $"Minimum order amount is {coupon.MinOrderAmount.Value:C}.", code, coupon.DiscountType, coupon.DiscountValue, 0, coupon.MinOrderAmount);

        var estimatedDiscount = CalculateDiscount(coupon, subtotal, 0);
        return new CouponValidationDto(true, null, coupon.Code, coupon.DiscountType, coupon.DiscountValue, estimatedDiscount, coupon.MinOrderAmount);
    }

    public async Task<CouponDto?> GetByIdAsync(int id)
    {
        var coupon = await _coupons.GetByIdAsync(id);
        return coupon == null ? null : MapToDto(coupon);
    }

    public async Task<PagedResult<CouponDto>> GetAllAsync(int page, int pageSize, bool? activeOnly)
    {
        var (items, total) = await _coupons.GetAllAsync(page, pageSize, activeOnly);
        return new PagedResult<CouponDto>(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest request)
    {
        var existing = await _coupons.GetByCodeAsync(request.Code);
        if (existing != null)
            throw new InvalidOperationException("Coupon code already exists.");

        var coupon = new Coupon
        {
            Code = request.Code.ToUpper(),
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            StartsAt = request.StartsAt,
            ExpiresAt = request.ExpiresAt,
            UsageLimit = request.UsageLimit
        };

        var created = await _coupons.CreateAsync(coupon);
        return MapToDto(created);
    }

    public async Task<CouponDto?> UpdateAsync(int id, UpdateCouponRequest request)
    {
        var coupon = await _coupons.GetByIdAsync(id);
        if (coupon == null) return null;

        if (request.Description != null) coupon.Description = request.Description;
        if (request.MinOrderAmount != null) coupon.MinOrderAmount = request.MinOrderAmount;
        if (request.MaxDiscountAmount != null) coupon.MaxDiscountAmount = request.MaxDiscountAmount;
        if (request.StartsAt != null) coupon.StartsAt = request.StartsAt.Value;
        if (request.ExpiresAt != null) coupon.ExpiresAt = request.ExpiresAt.Value;
        if (request.UsageLimit != null) coupon.UsageLimit = request.UsageLimit;
        if (request.IsActive != null) coupon.IsActive = request.IsActive.Value;

        var updated = await _coupons.UpdateAsync(coupon);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _coupons.DeleteAsync(id);
    }

    public static decimal CalculateDiscount(Coupon coupon, decimal subtotal, decimal shipping)
    {
        return coupon.DiscountType switch
        {
            DiscountType.Percentage => CalculatePercentageDiscount(coupon, subtotal),
            DiscountType.FixedAmount => CalculateFixedAmountDiscount(coupon, subtotal),
            DiscountType.FreeShipping => shipping,
            _ => 0
        };
    }

    private static decimal CalculatePercentageDiscount(Coupon coupon, decimal subtotal)
    {
        var discount = Math.Round(subtotal * (coupon.DiscountValue / 100m), 2);
        if (coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
        return discount;
    }

    private static decimal CalculateFixedAmountDiscount(Coupon coupon, decimal subtotal)
    {
        return Math.Min(coupon.DiscountValue, subtotal);
    }

    private static CouponDto MapToDto(Coupon c) => new(
        c.Id, c.Code, c.Description, c.DiscountType, c.DiscountValue,
        c.MinOrderAmount, c.MaxDiscountAmount, c.StartsAt, c.ExpiresAt,
        c.UsageLimit, c.TimesUsed, c.IsActive, c.CreatedAt
    );
}
