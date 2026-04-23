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
    string? PaymentMethodDetails,  // e.g. "Card ending 4242" or transfer ref
    decimal SubTotal,
    decimal Tax,
    decimal ShippingCost,
    decimal Total,
    string ShippingAddress,
    string ShippingCity,
    string ShippingProvince,       // = State in Nigeria
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
    string? Notes
);

public record UpdateOrderStatusRequest(OrderStatus Status, string? Notes = null);

public record OrderStatusLogDto(
    int Id,
    string FromStatus,
    string ToStatus,
    string ChangedByName,
    string ChangedByEmail,
    string? Note,
    DateTime ChangedAt
);