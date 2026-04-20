using TechStore.API.Models;

namespace TechStore.API.DTOs.Order;

public record OrderDto(
    int Id,
    string OrderNumber,
    int UserId,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string? PaymentMethod,
    decimal SubTotal,
    decimal DiscountAmount,
    string? CouponCode,
    decimal Tax,
    decimal ShippingCost,
    decimal Total,
    string ShippingAddress,
    string ShippingCity,
    string ShippingProvince,
    string ShippingPostalCode,
    string ShippingCountry,
    string? Notes,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime CreatedAt,
    List<OrderItemDto> Items
);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? ProductSKU,
    string? ProductImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);

public record OrderSummaryDto(
    int Id,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal Total,
    decimal DiscountAmount,
    string? CouponCode,
    int ItemCount,
    DateTime CreatedAt
);

public record CreateOrderRequest(
    string ShippingFirstName,
    string ShippingLastName,
    string ShippingAddress,
    string ShippingCity,
    string ShippingProvince,
    string ShippingPostalCode,
    string ShippingCountry,
    string? ShippingPhone,
    string? PaymentMethod,
    string? TransactionId,
    string? Notes,
    string? CouponCode
);

public record UpdateOrderStatusRequest(OrderStatus Status, string? Notes = null);
