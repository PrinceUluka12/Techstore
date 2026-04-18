namespace TechStore.API.DTOs.Cart;

public record CartDto(
    int Id,
    int UserId,
    List<CartItemDto> Items,
    decimal SubTotal,
    int TotalItems,
    DateTime UpdatedAt
);

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int? AvailableStock
);

public record AddToCartRequest(int ProductId, int Quantity = 1);
public record UpdateCartItemRequest(int Quantity);
