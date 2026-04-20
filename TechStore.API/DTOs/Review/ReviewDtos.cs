using TechStore.API.Models;

namespace TechStore.API.DTOs.Review;

public record ReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string UserName,
    int Rating,
    string? Comment,
    bool IsApproved,
    DateTime CreatedAt
);

public record CreateReviewRequest(
    int ProductId,
    int Rating,
    string? Comment
);

public record ApproveReviewRequest(bool Approve);
