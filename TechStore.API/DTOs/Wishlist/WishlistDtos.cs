namespace TechStore.API.DTOs.Wishlist;

public record WishlistItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal Price,
    DateTime AddedAt
);
