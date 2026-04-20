using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;

namespace TechStore.API.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly AppDbContext _db;
    public CouponRepository(AppDbContext db) => _db = db;

    public Task<Coupon?> GetByIdAsync(int id) =>
        _db.Coupons.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Coupon?> GetByCodeAsync(string code) =>
        _db.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper());

    public async Task<(IEnumerable<Coupon> Items, int Total)> GetAllAsync(int page, int pageSize, bool? isActive)
    {
        var q = _db.Coupons.AsQueryable();
        if (isActive.HasValue) q = q.Where(c => c.IsActive == isActive.Value);
        q = q.OrderByDescending(c => c.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Coupon> CreateAsync(Coupon coupon)
    {
        coupon.Code = coupon.Code.ToUpper();
        coupon.CreatedAt = DateTime.UtcNow;
        coupon.UpdatedAt = DateTime.UtcNow;
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return coupon;
    }

    public async Task<Coupon> UpdateAsync(Coupon coupon)
    {
        coupon.Code = coupon.Code.ToUpper();
        coupon.UpdatedAt = DateTime.UtcNow;
        _db.Coupons.Update(coupon);
        await _db.SaveChangesAsync();
        return coupon;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null) return false;
        _db.Coupons.Remove(c);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var q = _db.Coupons.Where(c => c.Code == code.ToUpper());
        if (excludeId.HasValue) q = q.Where(c => c.Id != excludeId.Value);
        return q.AnyAsync();
    }

    public async Task IncrementUsageAsync(int couponId)
    {
        var coupon = await _db.Coupons.FindAsync(couponId);
        if (coupon != null)
        {
            coupon.UsedCount++;
            coupon.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public Task<int> CountAsync() => _db.Coupons.CountAsync();
}
