using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.DTOs.Coupon;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouponsController : ControllerBase
{
    private readonly ICouponRepository _coupons;
    public CouponsController(ICouponRepository coupons) => _coupons = coupons;

    // ── Admin CRUD ────────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        var (items, total) = await _coupons.GetAllAsync(page, pageSize);
        return Ok(new { items = items.Select(Map), total, page, pageSize });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest req)
    {
        if (await _coupons.CodeExistsAsync(req.Code.ToUpper()))
            return BadRequest(new { message = "A coupon with this code already exists." });

        var coupon = new Coupon
        {
            Code                  = req.Code.ToUpper(),
            Description           = req.Description,
            Type                  = req.Type,
            Value                 = req.Value,
            MinimumOrderAmount    = req.MinimumOrderAmount,
            MaximumDiscountAmount = req.MaximumDiscountAmount,
            UsageLimit            = req.UsageLimit,
            ValidFrom             = req.ValidFrom,
            ValidTo               = req.ValidTo,
            IsActive              = req.IsActive,
        };

        var created = await _coupons.CreateAsync(coupon);
        return Ok(Map(created));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCouponRequest req)
    {
        var coupon = await _coupons.GetByIdAsync(id);
        if (coupon == null) return NotFound();

        if (req.Code != null)
        {
            var upper = req.Code.ToUpper();
            if (upper != coupon.Code && await _coupons.CodeExistsAsync(upper, id))
                return BadRequest(new { message = "A coupon with this code already exists." });
            coupon.Code = upper;
        }

        if (req.Description           != null) coupon.Description           = req.Description;
        if (req.Type                  != null) coupon.Type                  = req.Type.Value;
        if (req.Value                 != null) coupon.Value                 = req.Value.Value;
        if (req.MinimumOrderAmount    != null) coupon.MinimumOrderAmount    = req.MinimumOrderAmount;
        if (req.MaximumDiscountAmount != null) coupon.MaximumDiscountAmount = req.MaximumDiscountAmount;
        if (req.UsageLimit            != null) coupon.UsageLimit            = req.UsageLimit;
        if (req.ValidFrom             != null) coupon.ValidFrom             = req.ValidFrom;
        if (req.ValidTo               != null) coupon.ValidTo               = req.ValidTo;
        if (req.IsActive              != null) coupon.IsActive              = req.IsActive.Value;

        var updated = await _coupons.UpdateAsync(coupon);
        return Ok(Map(updated));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _coupons.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // ── Public — coupon validation ────────────────────────────────────────────

    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest req)
    {
        var coupon = await _coupons.GetByCodeAsync(req.Code.ToUpper());

        if (coupon == null || !coupon.IsActive)
            return Ok(new ValidateCouponResponse(false, "Coupon code not found or inactive.", 0, req.SubTotal));

        if (coupon.ValidFrom.HasValue && coupon.ValidFrom > DateTime.UtcNow)
            return Ok(new ValidateCouponResponse(false, "Coupon is not yet valid.", 0, req.SubTotal));

        if (coupon.ValidTo.HasValue && coupon.ValidTo < DateTime.UtcNow)
            return Ok(new ValidateCouponResponse(false, "Coupon has expired.", 0, req.SubTotal));

        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit)
            return Ok(new ValidateCouponResponse(false, "Coupon usage limit reached.", 0, req.SubTotal));

        if (coupon.MinimumOrderAmount.HasValue && req.SubTotal < coupon.MinimumOrderAmount)
            return Ok(new ValidateCouponResponse(false,
                $"Minimum order of ₦{coupon.MinimumOrderAmount:N2} required.", 0, req.SubTotal));

        var discount = coupon.Type == DiscountType.Percentage
            ? Math.Round(req.SubTotal * coupon.Value / 100, 2)
            : coupon.Value;

        if (coupon.MaximumDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);

        return Ok(new ValidateCouponResponse(true, null, discount, req.SubTotal - discount));
    }

    private static CouponDto Map(Coupon c) => new(
        c.Id, c.Code, c.Description, c.Type, c.Value,
        c.MinimumOrderAmount, c.MaximumDiscountAmount,
        c.UsageLimit, c.UsedCount, c.ValidFrom, c.ValidTo,
        c.IsActive, c.CreatedAt, c.UpdatedAt);
}
