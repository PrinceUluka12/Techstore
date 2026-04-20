using TechStore.API.DTOs.Review;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IProductRepository _products;

    public ReviewService(IReviewRepository reviews, IProductRepository products)
    {
        _reviews = reviews;
        _products = products;
    }

    public async Task<ReviewDto?> GetByIdAsync(int id)
    {
        var review = await _reviews.GetByIdAsync(id);
        return review == null ? null : MapToDto(review);
    }

    public async Task<PagedResult<ReviewDto>> GetByProductIdAsync(int productId, int page, int pageSize)
    {
        var (items, total) = await _reviews.GetByProductIdAsync(productId, page, pageSize, isApproved: true);
        return new PagedResult<ReviewDto>(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<ProductReviewsSummaryDto> GetProductSummaryAsync(int productId)
    {
        var (avgRating, totalReviews) = await _reviews.GetApprovedProductStatsAsync(productId);
        var ratingCounts = await _reviews.GetRatingDistributionAsync(productId);

        return new ProductReviewsSummaryDto(
            totalReviews > 0 ? avgRating : null,
            totalReviews,
            ratingCounts.GetValueOrDefault(5, 0),
            ratingCounts.GetValueOrDefault(4, 0),
            ratingCounts.GetValueOrDefault(3, 0),
            ratingCounts.GetValueOrDefault(2, 0),
            ratingCounts.GetValueOrDefault(1, 0)
        );
    }

    public async Task<ReviewDto> CreateAsync(int userId, CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var existing = await _reviews.GetByUserAndProductAsync(userId, request.ProductId);
        if (existing != null)
            throw new InvalidOperationException("You have already reviewed this product.");

        var product = await _products.GetByIdAsync(request.ProductId)
            ?? throw new KeyNotFoundException("Product not found.");

        var isVerifiedPurchase = await _reviews.HasUserPurchasedProductAsync(userId, request.ProductId);

        var review = new Review
        {
            ProductId = request.ProductId,
            UserId = userId,
            Rating = request.Rating,
            Title = request.Title,
            Comment = request.Comment,
            IsVerifiedPurchase = isVerifiedPurchase,
            IsApproved = false
        };

        var created = await _reviews.CreateAsync(review);
        return MapToDto(await _reviews.GetByIdAsync(created.Id) ?? created);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var review = await _reviews.GetByIdAsync(id);
        if (review == null) return false;
        if (review.UserId != userId) return false;

        var wasApproved = review.IsApproved;
        var productId = review.ProductId;

        var deleted = await _reviews.DeleteAsync(id);
        if (!deleted) return false;

        if (wasApproved)
            await RecalculateProductRatingAsync(productId);

        return true;
    }

    public async Task<PagedResult<ReviewDto>> GetAllAsync(int page, int pageSize, bool? isApproved)
    {
        var (items, total) = await _reviews.GetAllAsync(page, pageSize, isApproved);
        return new PagedResult<ReviewDto>(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<bool> ApproveAsync(int id)
    {
        var review = await _reviews.GetByIdAsync(id);
        if (review == null) return false;

        var success = await _reviews.SetApprovalAsync(id, true);
        if (!success) return false;

        await RecalculateProductRatingAsync(review.ProductId);
        return true;
    }

    public async Task<bool> RejectAsync(int id)
    {
        var review = await _reviews.GetByIdAsync(id);
        if (review == null) return false;

        return await _reviews.DeleteAsync(id);
    }

    private async Task RecalculateProductRatingAsync(int productId)
    {
        var (avgRating, count) = await _reviews.GetApprovedProductStatsAsync(productId);

        var product = await _products.GetByIdAsync(productId);
        if (product == null) return;

        product.Rating = count > 0 ? avgRating : null;
        product.ReviewCount = count;
        await _products.UpdateAsync(product);
    }

    private static ReviewDto MapToDto(Review r) => new(
        r.Id,
        r.ProductId,
        r.Product?.Name ?? "",
        r.UserId,
        $"{r.User?.FirstName} {r.User?.LastName}".Trim(),
        r.Rating,
        r.Title,
        r.Comment,
        r.IsVerifiedPurchase,
        r.IsApproved,
        r.CreatedAt
    );
}
