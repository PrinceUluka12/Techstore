using TechStore.API.DTOs.Cart;
using TechStore.API.Models;

namespace TechStore.API.DTOs.Order;

public record OrderDto(
    int Id,
    string OrderNumber,
    int? UserId, // null for walk-in sales without an account
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    string? PaymentMethod,
    string? PaymentMethodDetails,
    decimal SubTotal,
    decimal DiscountAmount,
    string? CouponCode,
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
    List<OrderItemDto> Items,
    SalesChannel Channel = SalesChannel.Online,
    string? StoreName = null,
    FulfilmentMethod Fulfilment = FulfilmentMethod.Delivery,
    string? ShippingPhone = null,
    string? PickupAddress = null,      // the shop's, for pickup orders
    string? PickupPhone = null,
    DateTime? PaymentDueAt = null,     // unpaid online orders are cancelled then
    DateTime? PaidAt = null,
    List<OrderStatus>? NextStatuses = null, // what staff can move the order to now
    bool CanMarkPaid = false,
    string? GuestToken = null // only in the guest checkout's reply: the key to the order's private link
);

// Checking out without an account: the cart comes from the browser (prices and stock are the
// API's), an email to send the order to, and the same details as a signed-in checkout
public record GuestCheckoutRequest(string Email, List<CartLine> Items, CreateOrderRequest Order);
public record FindGuestOrderRequest(string OrderNumber, string Email);

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
    DateTime CreatedAt,
    SalesChannel Channel = SalesChannel.Online,
    string? StoreName = null,
    FulfilmentMethod Fulfilment = FulfilmentMethod.Delivery
);

// For pickup orders the address fields are left empty
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
    string? CouponCode = null,
    FulfilmentMethod Fulfilment = FulfilmentMethod.Delivery,
    int? PickupStoreId = null
);

public record UpdateOrderStatusRequest(OrderStatus Status, string? Notes = null);

public record MarkPaidRequest(string? Reference = null);

// A shop the customer can collect from; HasStock = everything in their cart is there
public record PickupShopDto(int Id, string Name, string? Address, string? City, string? Phone, bool HasStock);

public record DeliveryRateDto(string State, decimal Fee);

// Admin view: every state, with Fee null where there's no delivery
public record DeliveryRateAdminDto(string State, decimal? Fee, string? UpdatedByName, DateTime? UpdatedAt);

public record SaveDeliveryRateRequest(string State, decimal? Fee);

public record OrderStatusLogDto(
    int Id,
    string FromStatus,
    string ToStatus,
    string ChangedByName,
    string ChangedByEmail,
    string? Note,
    DateTime ChangedAt
);