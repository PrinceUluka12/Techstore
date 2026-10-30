using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.API.DTOs.Review;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private const string AlreadyReviewed = "You have already reviewed this product.";

    private readonly IReviewRepository _reviews;
    private readonly IProductRepository _products;

    public ReviewsController(IReviewRepository reviews, IProductRepository products)
    {
        _reviews  = reviews;
        _products = products;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>Get approved reviews for a product, newest first</summary>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page     = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (items, total) = await _reviews.GetByProductIdAsync(productId, page, pageSize, isApproved: true);
        return Ok(new PagedResult<ReviewDto>(items.Select(Map), total, page, pageSize));
    }

    /// <summary>Get average rating and star breakdown for a product</summary>
    [HttpGet("product/{productId}/summary")]
    public async Task<IActionResult> GetSummary(int productId)
    {
        var (average, count) = await _reviews.GetApprovedProductStatsAsync(productId);
        var dist = await _reviews.GetRatingDistributionAsync(productId);

        return Ok(new ProductReviewsSummaryDto(
            count == 0 ? null : Math.Round(average, 1),
            count,
            dist.GetValueOrDefault(5),
            dist.GetValueOrDefault(4),
            dist.GetValueOrDefault(3),
            dist.GetValueOrDefault(2),
            dist.GetValueOrDefault(1)));
    }

    // ── Signed-in customers ───────────────────────────────────────────────────

    /// <summary>Submit a review — one per customer per product</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest req)
    {
        var userId  = GetUserId();
        var product = await _products.GetByIdAsync(req.ProductId);
        if (product == null || !product.IsActive)
            return NotFound(new { message = "Product not found." });

        if (await _reviews.GetByUserAndProductAsync(userId, req.ProductId) != null)
            return BadRequest(new { message = AlreadyReviewed });

        var review = new Review
        {
            ProductId          = req.ProductId,
            UserId             = userId,
            Rating             = req.Rating,
            Title              = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            Comment            = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim(),
            IsVerifiedPurchase = await _reviews.HasUserPurchasedProductAsync(userId, req.ProductId),
            // Published immediately — there is no moderation queue in the back office yet
            IsApproved         = true,
        };

        try { await _reviews.CreateAsync(review); }
        // Unique (ProductId, UserId) index — a concurrent duplicate submit lands here
        catch (DbUpdateException) { return BadRequest(new { message = AlreadyReviewed }); }

        await SyncProductRatingAsync(product);

        var created = await _reviews.GetByIdAsync(review.Id);
        return Ok(Map(created!));
    }

    /// <summary>Delete a review (its author, or staff who manage products)</summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _reviews.GetByIdAsync(id);
        if (review == null) return NotFound();

        if (review.UserId != GetUserId() && !User.HasPermission(Perms.ProductsManage))
            return Forbid();

        if (!await _reviews.DeleteAsync(id)) return NotFound();

        var product = await _products.GetByIdAsync(review.ProductId);
        if (product != null) await SyncProductRatingAsync(product);

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Product.Rating / ReviewCount are denormalised for listings; recompute from approved reviews
    private async Task SyncProductRatingAsync(Product product)
    {
        var (average, count) = await _reviews.GetApprovedProductStatsAsync(product.Id);
        product.Rating      = count == 0 ? null : Math.Round(average, 1);
        product.ReviewCount = count;
        await _products.UpdateAsync(product);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static ReviewDto Map(Review r) => new(
        r.Id, r.ProductId, r.Product?.Name ?? string.Empty,
        r.UserId, $"{r.User?.FirstName} {r.User?.LastName}".Trim(),
        r.Rating, r.Title, r.Comment,
        r.IsVerifiedPurchase, r.IsApproved, r.CreatedAt);
}
