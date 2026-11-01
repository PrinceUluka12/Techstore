using System.Net;

namespace TechStore.API.Tests;

[Collection("api")]
public class SearchTests(ApiFactory api)
{
    [Fact]
    public async Task Every_word_counts_wherever_it_appears()
    {
        var model = $"Q{Guid.NewGuid().ToString("N")[..5]}"; // unique, so other tests' products don't match
        var sku = $"T-{Guid.NewGuid().ToString("N")[..10]}";
        await ApiFactory.Ok(api.Call(HttpMethod.Post, "/products", api.AdminToken, new
        {
            name = $"Galaxy {model}", sku, price = 1000, categoryId = 1, initialStock = 1, lowStockThreshold = 1,
            brand = "Samsung", description = "", imageUrl = "", isActive = true, isFeatured = false,
        }));
        async Task<int> found(string query) =>
            (await api.Call(HttpMethod.Get, $"/products?query={Uri.EscapeDataString(query)}")).Int("total");

        Assert.Equal(1, await found($"samsung {model}"));          // brand + name
        Assert.Equal(1, await found($"{model.ToLower()}  galaxy")); // any order, extra spaces
        Assert.Equal(1, await found($"{model} smartphones"));      // category
        Assert.Equal(0, await found($"apple {model}"));            // every word must match
    }
}
