namespace TechStore.API.DTOs.Review;

public record ReviewDto(
    int Id,
    int ProductId,
    string ProductName,
    int UserId,
    string UserName,
    int Rating,
    string? Title,
    string? Comment,
    bool IsVerifiedPurchase,
    bool IsApproved,
    DateTime CreatedAt
);

public record CreateReviewRequest(
    int ProductId,
    int Rating,
    string? Title,
    string? Comment
);

public record ProductReviewsSummaryDto(
    double? AverageRating,
    int TotalReviews,
    int Rating5Count,
    int Rating4Count,
    int Rating3Count,
    int Rating2Count,
    int Rating1Count
);
