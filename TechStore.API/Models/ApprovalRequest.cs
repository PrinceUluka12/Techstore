namespace TechStore.API.Models;

public enum ApprovalStatus
{
    Waiting,   // sent to the shop's managers; nobody has answered
    Approved,  // the till can now complete the sale (once)
    Declined,
    Cancelled, // by the cashier, or replaced by a newer request from the same till
    Expired,   // nobody answered in time (set when it's next read; see ApprovalService)
    Used       // the approved sale went through
}

// A cashier asking the shop's managers to approve a discount or price change from their own
// phone or computer, instead of a manager coming to the till to type their PIN. It's for one
// exact cart: the sale must have the same items, quantities, prices and discount.
public class ApprovalRequest
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Waiting;

    public int RequestedByUserId { get; set; }
    public User RequestedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } // a request nobody answers by then has expired

    public decimal SubTotal { get; set; }       // at the prices asked for
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Who approved or declined it, and their note when declining
    public int? DecidedByUserId { get; set; }
    public User? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }

    public int? OrderId { get; set; } // the sale that used it
    public Order? Order { get; set; }

    public ICollection<ApprovalRequestItem> Items { get; set; } = new List<ApprovalRequestItem>();
}

public class ApprovalRequestItem
{
    public int Id { get; set; }
    public int ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty; // snapshot
    public string? ProductSKU { get; set; }
    public int Quantity { get; set; }
    public decimal NormalPrice { get; set; } // the product's price when asked
    public decimal UnitPrice { get; set; }   // the price asked for
}
