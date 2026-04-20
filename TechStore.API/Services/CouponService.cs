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

    public async Task<CouponDto?> GetByIdAsync(int id)
    {
        var c = await _coupons.GetByIdAsync(id);
        return c == null ? null : MapToDto(c);
    }

    public async Task<CouponDto?> GetByCodeAsync(string code)
    {
        var c = await _coupons.GetByCodeAsync(code);
        return c == null ? null : MapToDto(c);
    }

    public async Task<PagedResult<CouponListDto>> GetAllAsync(int page, int pageSize, bool? isActive)
    {
        var (items, total) = await _coupons.GetAllAsync(page, pageSize, isActive);
        return new PagedResult<CouponListDto>(items.Select(MapToListDto), total, page, pageSize);
    }

    public async Task<CouponDto> CreateAsync(CreateCouponRequest req)
    {
        if (await _coupons.CodeExistsAsync(req.Code))
            throw new InvalidOperationException("Coupon code already exists.");

        if (req.Type == DiscountType.Percentage && (req.Value <= 0 || req.Value > 100))
            throw new InvalidOperationException("Percentage value must be between 1 and 100.");

        if (req.Type == DiscountType.FixedAmount && req.Value <= 0)
            throw new InvalidOperationException("Fixed amount must be greater than zero.");

        var coupon = new Coupon
        {
            Code = req.Code,
            Description = req.Description,
            Type = req.Type,
            Value = req.Value,
            MinimumOrderAmount = req.MinimumOrderAmount,
            MaximumDiscountAmount = req.MaximumDiscountAmount,
            UsageLimit = req.UsageLimit,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            IsActive = req.IsActive
        };

        var created = await _coupons.CreateAsync(coupon);
        return MapToDto(created);
    }

    public async Task<CouponDto?> UpdateAsync(int id, UpdateCouponRequest req)
    {
        var coupon = await _coupons.GetByIdAsync(id);
        if (coupon == null) return null;

        if (req.Code != null)
        {
            if (await _coupons.CodeExistsAsync(req.Code, id))
                throw new InvalidOperationException("Coupon code already exists.");
            coupon.Code = req.Code;
        }
        if (req.Description != null) coupon.Description = req.Description;
        if (req.Type.HasValue) coupon.Type = req.Type.Value;
        if (req.Value.HasValue) coupon.Value = req.Value.Value;
        if (req.MinimumOrderAmount != null) coupon.MinimumOrderAmount = req.MinimumOrderAmount;
        if (req.MaximumDiscountAmount != null) coupon.MaximumDiscountAmount = req.MaximumDiscountAmount;
        if (req.UsageLimit != null) coupon.UsageLimit = req.UsageLimit;
        if (req.ValidFrom != null) coupon.ValidFrom = req.ValidFrom;
        if (req.ValidTo != null) coupon.ValidTo = req.ValidTo;
        if (req.IsActive.HasValue) coupon.IsActive = req.IsActive.Value;

        if (coupon.Type == DiscountType.Percentage && (coupon.Value <= 0 || coupon.Value > 100))
            throw new InvalidOperationException("Percentage value must be between 1 and 100.");
        if (coupon.Type == DiscountType.FixedAmount && coupon.Value <= 0)
            throw new InvalidOperationException("Fixed amount must be greater than zero.");

        var updated = await _coupons.UpdateAsync(coupon);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _coupons.DeleteAsync(id);
    }

    public async Task IncrementUsageAsync(int couponId)
    {
        await _coupons.IncrementUsageAsync(couponId);
    }

    public async Task<(bool IsValid, string? ErrorMessage, decimal DiscountAmount, Coupon? CouponEntity)> ValidateAndCalculateDiscountAsync(string code, decimal subTotal)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, "Coupon code is required.", 0, null);

        var coupon = await _coupons.GetByCodeAsync(code);
        if (coupon == null)
            return (false, "Invalid coupon code.", 0, null);

        if (!coupon.IsActive)
            return (false, "This coupon is no longer active.", 0, null);

        var now = DateTime.UtcNow;
        if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom.Value)
            return (false, "This coupon is not yet valid.", 0, null);
        if (coupon.ValidTo.HasValue && now > coupon.ValidTo.Value)
            return (false, "This coupon has expired.", 0, null);

        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            return (false, "This coupon has reached its usage limit.", 0, null);

        if (coupon.MinimumOrderAmount.HasValue && subTotal < coupon.MinimumOrderAmount.Value)
            return (false, $"Minimum order amount of {coupon.MinimumOrderAmount.Value:C} required.", 0, null);

        decimal discount = 0;
        if (coupon.Type == DiscountType.Percentage)
        {
            discount = Math.Round(subTotal * (coupon.Value / 100m), 2);
        }
        else
        {
            discount = coupon.Value;
        }

        if (coupon.MaximumDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);

        discount = Math.Min(discount, subTotal);

        return (true, null, discount, coupon);
    }

    private static CouponDto MapToDto(Coupon c) => new(
        c.Id, c.Code, c.Description, c.Type, c.Value,
        c.MinimumOrderAmount, c.MaximumDiscountAmount,
        c.UsageLimit, c.UsedCount, c.ValidFrom, c.ValidTo,
        c.IsActive, c.CreatedAt, c.UpdatedAt
    );

    private static CouponListDto MapToListDto(Coupon c) => new(
        c.Id, c.Code, c.Type, c.Value,
        c.MinimumOrderAmount, c.UsageLimit, c.UsedCount,
        c.IsActive, c.ValidTo
    );
}
