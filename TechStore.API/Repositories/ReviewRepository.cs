using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;

namespace TechStore.API.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _db;
    public ReviewRepository(AppDbContext db) => _db = db;

    public Task<Review?> GetByIdAsync(int id) =>
        _db.Reviews.Include(r => r.User).Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<Review?> GetByUserAndProductAsync(int userId, int productId) =>
        _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

    public async Task<(IEnumerable<Review> Items, int Total)> GetByProductIdAsync(int productId, bool? approvedOnly, int page, int pageSize)
    {
        var q = _db.Reviews.Include(r => r.User)
            .Where(r => r.ProductId == productId);
        if (approvedOnly.HasValue)
            q = q.Where(r => r.IsApproved == approvedOnly.Value);
        q = q.OrderByDescending(r => r.CreatedAt);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<Review> UpdateAsync(Review review)
    {
        review.UpdatedAt = DateTime.UtcNow;
        _db.Reviews.Update(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r == null) return false;
        _db.Reviews.Remove(r);
        await _db.SaveChangesAsync();
        return true;
    }

    public Task<bool> ExistsForUserAndProductAsync(int userId, int productId) =>
        _db.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId);

    public Task<int> CountApprovedForProductAsync(int productId) =>
        _db.Reviews.CountAsync(r => r.ProductId == productId && r.IsApproved);

    public async Task<double?> GetAverageRatingForProductAsync(int productId)
    {
        var approved = await _db.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .Select(r => (double?)r.Rating)
            .ToListAsync();
        return approved.Count > 0 ? approved.Average() : null;
    }
}
