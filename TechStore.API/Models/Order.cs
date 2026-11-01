namespace TechStore.API.Models;

// Online orders: Pending (waiting for payment) → Confirmed (paid) → Processing →
// Shipped or ReadyForPickup → Delivered (handed over). They can be Cancelled until then.
// Refunded is only on older orders. In-store sales are saved as Delivered.
public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded,
    ReadyForPickup
}

// How an online order reaches the customer
public enum FulfilmentMethod
{
    Delivery,
    Pickup // collected from the order's shop (StoreId)
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum SalesChannel
{
    Online,
    InStore
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public SalesChannel Channel { get; set; } = SalesChannel.Online;
    // Null for walk-in sales and guest checkouts (no account); CustomerName/Phone/Email hold their details
    public int? UserId { get; set; }
    public User? User { get; set; }
    // Guest checkouts: the secret in the customer's link to their order (/orders/track/{token})
    public string? GuestToken { get; set; }
    // Shop whose stock this order reserved (online) or sold from (in store)
    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    // In-store sales
    public int? SoldByUserId { get; set; }
    public User? SoldBy { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public int? DiscountApprovedByUserId { get; set; } // manager who approved a discount or price change
    public User? DiscountApprovedBy { get; set; }
    public string? DiscountReason { get; set; }
    public ICollection<OrderPayment> Payments { get; set; } = new List<OrderPayment>();

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public FulfilmentMethod Fulfilment { get; set; } = FulfilmentMethod.Delivery;
    // Online orders: unpaid ones are cancelled at PaymentDueAt (null on orders from before this)
    public DateTime? PaymentDueAt { get; set; }
    public DateTime? PaymentReminderSentAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public string? CouponCode { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    // Shipping address snapshot
    public string ShippingFirstName { get; set; } = string.Empty;
    public string ShippingLastName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingProvince { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "Nigeria";
    public string? ShippingPhone { get; set; }

    public string? Notes { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty; // Snapshot
    public string? ProductSKU { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? OriginalUnitPrice { get; set; } // catalogue price, when an approved change replaced it
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public ICollection<OrderItemSerial> Serials { get; set; } = new List<OrderItemSerial>();
}

// One row per unit sold of an IMEI/serial-tracked product
public class OrderItemSerial
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
    public string SerialNumber { get; set; } = string.Empty;
}

// A sale can be paid in several parts (e.g. cash + transfer)
public class OrderPayment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Method { get; set; } = string.Empty; // PaymentMethods
    public decimal Amount { get; set; }                // applied to the order
    public decimal? Tendered { get; set; }             // cash handed over; Tendered - Amount = change
    public string? Reference { get; set; }             // terminal / transfer reference
    public int? ReceivedByUserId { get; set; }
    public User? ReceivedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class PaymentMethods
{
    public const string Cash = "Cash";
    public const string Card = "Card";               // POS terminal
    public const string BankTransfer = "BankTransfer";
    public const string Ussd = "USSD";
    public const string Other = "Other";

    public static readonly string[] InStore = [Cash, Card, BankTransfer, Ussd, Other];
    public static readonly string[] NeedReference = [Card, BankTransfer, Ussd];
}