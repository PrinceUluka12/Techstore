namespace TechStore.API.DTOs.Product;

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    string? Brand,
    string? ImageUrl,
    List<string>? AdditionalImages,
    bool IsActive,
    bool IsFeatured,
    double? Rating,
    int ReviewCount,
    int CategoryId,
    string CategoryName,
    int? QuantityAvailable,
    DateTime CreatedAt,
    IEnumerable<ProductVariantDto>? Variants
);

public record ProductListDto(
    int Id,
    string Name,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    string? Brand,
    string? ImageUrl,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    int? QuantityAvailable,
    double? Rating
);

public record CreateProductRequest(
    string Name,
    string? Description,
    string SKU,
    decimal Price,
    decimal? CompareAtPrice,
    string? Brand,
    string? ImageUrl,
    List<string>? AdditionalImages,
    int CategoryId,
    bool IsActive = true,
    bool IsFeatured = false,
    int InitialStock = 0,
    int LowStockThreshold = 10
);

public record UpdateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    decimal? CompareAtPrice,
    string? Brand,
    string? ImageUrl,
    List<string>? AdditionalImages,
    int? CategoryId,
    bool? IsActive,
    bool? IsFeatured
);

public record ProductSearchParams(
    string? Query = null,
    int? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Brand = null,
    bool? InStock = null,
    string SortBy = "name",
    string SortDir = "asc",
    int Page = 1,
    int PageSize = 20
);

public record CategoryDto(int Id, string Name, string? Description, string? ImageUrl, bool IsActive, int ProductCount);

public record ProductVariantDto(
    int Id,
    int ProductId,
    string Name,
    string? SKU,
    decimal PriceAdjustment,
    string? Attributes,
    int StockQuantity,
    bool IsActive
);

public record CreateVariantRequest(
    string Name,
    string? SKU = null,
    decimal PriceAdjustment = 0,
    string? Attributes = null,
    int StockQuantity = 0
);

public record UpdateVariantRequest(
    string? Name = null,
    string? SKU = null,
    decimal? PriceAdjustment = null,
    string? Attributes = null,
    int? StockQuantity = null,
    bool? IsActive = null
);
