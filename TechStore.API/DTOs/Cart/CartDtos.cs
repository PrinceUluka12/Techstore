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

// A signed-out shopper's cart lives in their browser; the API prices it (cart/preview) and
// checks it out (orders/guest/checkout)
public record CartLine(int ProductId, int Quantity);
public record GuestCartRequest(List<CartLine> Items);
public record UpdateCartItemRequest(int Quantity);
