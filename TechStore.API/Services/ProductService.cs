using TechStore.API.DTOs.Product;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;

    public ProductService(IProductRepository products, IInventoryRepository inventory)
    {
        _products = products;
        _inventory = inventory;
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _products.GetByIdAsync(id);
        return p == null ? null : MapToDto(p);
    }

    public async Task<PagedResult<ProductListDto>> SearchAsync(ProductSearchParams s)
    {
        var (items, total) = await _products.SearchAsync(s);
        return new PagedResult<ProductListDto>(items.Select(MapToListDto), total, s.Page, s.PageSize);
    }

    public async Task<IEnumerable<ProductListDto>> GetFeaturedAsync()
    {
        var items = await _products.GetFeaturedAsync();
        return items.Select(MapToListDto);
    }

    public async Task<IEnumerable<ProductListDto>> GetByCategoryAsync(int categoryId, int page, int pageSize)
    {
        var items = await _products.GetByCategoryAsync(categoryId, page, pageSize);
        return items.Select(MapToListDto);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest req)
    {
        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            SKU = req.SKU,
            Price = req.Price,
            CompareAtPrice = req.CompareAtPrice,
            Brand = req.Brand,
            ImageUrl = req.ImageUrl,
            CategoryId = req.CategoryId,
            IsActive = req.IsActive,
            IsFeatured = req.IsFeatured
        };

        var created = await _products.CreateAsync(product);

        // Create inventory record
        var inv = new Inventory
        {
            ProductId = created.Id,
            QuantityOnHand = req.InitialStock,
            LowStockThreshold = req.LowStockThreshold
        };
        await _inventory.CreateAsync(inv);

        return MapToDto(await _products.GetByIdAsync(created.Id) ?? created);
    }

    public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest req)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null) return null;

        if (req.Name != null) product.Name = req.Name;
        if (req.Description != null) product.Description = req.Description;
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.CompareAtPrice.HasValue) product.CompareAtPrice = req.CompareAtPrice;
        if (req.Brand != null) product.Brand = req.Brand;
        if (req.ImageUrl != null) product.ImageUrl = req.ImageUrl;
        if (req.CategoryId.HasValue) product.CategoryId = req.CategoryId.Value;
        if (req.IsActive.HasValue) product.IsActive = req.IsActive.Value;
        if (req.IsFeatured.HasValue) product.IsFeatured = req.IsFeatured.Value;

        await _products.UpdateAsync(product);
        return MapToDto(product);
    }

    public Task<bool> DeleteAsync(int id) => _products.DeleteAsync(id);

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var cats = await _products.GetCategoriesAsync();
        return cats.Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.ImageUrl, c.IsActive, c.Products.Count));
    }

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Name, p.Description, p.SKU, p.Price, p.CompareAtPrice,
        p.Brand, p.ImageUrl, p.IsActive, p.IsFeatured, p.Rating, p.ReviewCount,
        p.CategoryId, p.Category?.Name ?? "", p.Inventory?.QuantityAvailable, p.CreatedAt
    );

    private static ProductListDto MapToListDto(Product p) => new(
        p.Id, p.Name, p.SKU, p.Price, p.CompareAtPrice, p.Brand, p.ImageUrl,
        p.IsActive, p.CategoryId, p.Category?.Name ?? "", p.Inventory?.QuantityAvailable, p.Rating
    );
}
