using TechStore.API.Models;

namespace TechStore.API.Services;

// One set of coupon rules for "Apply" at checkout and for placing the order
public static class CouponRules
{
    // Why the coupon can't be used on this order, or null when it can.
    // usesByCustomer: this customer's orders with the coupon, not counting cancelled ones.
    public static string? Problem(Coupon? coupon, decimal subTotal, int usesByCustomer, DateTime now)
    {
        if (coupon == null || !coupon.IsActive)
            return "Coupon code not found or inactive.";
        if (coupon.ValidFrom > now)
            return "Coupon is not yet valid.";
        if (coupon.ValidTo < now)
            return "Coupon has expired.";
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit)
            return "Coupon usage limit reached.";
        if (coupon.UsesPerCustomer.HasValue && usesByCustomer >= coupon.UsesPerCustomer)
            return coupon.UsesPerCustomer == 1
                ? "You've already used this coupon."
                : $"You've already used this coupon {coupon.UsesPerCustomer} times.";
        if (coupon.MinimumOrderAmount.HasValue && subTotal < coupon.MinimumOrderAmount)
            return $"Minimum order of ₦{coupon.MinimumOrderAmount:N2} required.";
        return null;
    }

    // Never more than the items cost, so a fixed-amount coupon can't make the total negative
    public static decimal Discount(Coupon coupon, decimal subTotal)
    {
        var discount = coupon.Type == DiscountType.Percentage
            ? Math.Round(subTotal * coupon.Value / 100, 2)
            : coupon.Value;
        if (coupon.MaximumDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);
        return Math.Clamp(discount, 0, subTotal);
    }
}
