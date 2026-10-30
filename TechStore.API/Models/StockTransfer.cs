namespace TechStore.API.Models;

public enum TransferStatus
{
    InTransit, // sent: units are off the sending shop's stock but not yet in the receiving shop's
    Received,
    Cancelled  // returned to the sending shop
}

// Stock moving from one shop to another
public class StockTransfer
{
    public int Id { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public int FromStoreId { get; set; }
    public Store FromStore { get; set; } = null!;
    public int ToStoreId { get; set; }
    public Store ToStore { get; set; } = null!;
    public TransferStatus Status { get; set; } = TransferStatus.InTransit;
    public string? Notes { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Who received or cancelled it
    public int? CompletedByUserId { get; set; }
    public User? CompletedBy { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}

public class StockTransferItem
{
    public int Id { get; set; }
    public int StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty; // snapshot
    public string? ProductSKU { get; set; }
    public int Quantity { get; set; }
}
