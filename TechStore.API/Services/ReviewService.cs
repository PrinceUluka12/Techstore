using TechStore.API.DTOs.Review;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IProductRepository _products;
    private readonly IOrderService _orders;
    private readonly IUserRepository _users;

    public ReviewService(
        IReviewRepository reviews,
        IProductRepository products,
        IOrderService orders,
        IUserRepository users)
    {
        _reviews = reviews;
        _products = products;
        _orders = orders;
        _users = users;
    }

    public async Task<PagedResult<ReviewDto>> GetProductReviewsAsync(int productId, int page, int pageSize)
    {
        var (items, total) = await _reviews.GetByProductIdAsync(productId, true, page, pageSize);
        return new PagedResult<ReviewDto>(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<ReviewDto> CreateReviewAsync(int userId, CreateReviewRequest req)
    {
        if (req.Rating < 1 || req.Rating > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var hasPurchased = await _orders.HasUserPurchasedProductAsync(userId, req.ProductId);
        if (!hasPurchased)
            throw new InvalidOperationException("You can only review products you have purchased and received.");

        var already = await _reviews.ExistsForUserAndProductAsync(userId, req.ProductId);
        if (already)
            throw new InvalidOperationException("You have already reviewed this product.");

        var product = await _products.GetByIdAsync(req.ProductId);
        if (product == null)
            throw new KeyNotFoundException("Product not found.");

        var review = new Review
        {
            ProductId = req.ProductId,
            UserId = userId,
            Rating = req.Rating,
            Comment = req.Comment,
            IsApproved = false
        };

        var created = await _reviews.CreateAsync(review);
        return MapToDto(created);
    }

    public async Task<bool> DeleteReviewAsync(int userId, int reviewId, bool isAdmin = false)
    {
        var review = await _reviews.GetByIdAsync(reviewId);
        if (review == null) return false;

        if (!isAdmin && review.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own reviews.");

        var deleted = await _reviews.DeleteAsync(reviewId);
        if (deleted)
            await RecalculateProductRatingAsync(review.ProductId);
        return deleted;
    }

    public async Task<ReviewDto?> ApproveReviewAsync(int reviewId)
    {
        var review = await _reviews.GetByIdAsync(reviewId);
        if (review == null) return null;

        if (!review.IsApproved)
        {
            review.IsApproved = true;
            await _reviews.UpdateAsync(review);
            await RecalculateProductRatingAsync(review.ProductId);
        }
        return MapToDto(review);
    }

    public async Task<ReviewDto?> RejectReviewAsync(int reviewId)
    {
        var review = await _reviews.GetByIdAsync(reviewId);
        if (review == null) return null;

        if (review.IsApproved)
        {
            review.IsApproved = false;
            await _reviews.UpdateAsync(review);
            await RecalculateProductRatingAsync(review.ProductId);
        }
        return MapToDto(review);
    }

    private async Task RecalculateProductRatingAsync(int productId)
    {
        var product = await _products.GetByIdAsync(productId);
        if (product == null) return;

        var count = await _reviews.CountApprovedForProductAsync(productId);
        var avg = await _reviews.GetAverageRatingForProductAsync(productId);

        product.ReviewCount = count;
        product.Rating = avg.HasValue ? Math.Round(avg.Value, 1) : null;

        await _products.UpdateAsync(product);
    }

    private static ReviewDto MapToDto(Review r) => new(
        r.Id, r.ProductId, r.UserId,
        $"{r.User?.FirstName} {r.User?.LastName}".Trim(),
        r.Rating, r.Comment, r.IsApproved, r.CreatedAt
    );
}
