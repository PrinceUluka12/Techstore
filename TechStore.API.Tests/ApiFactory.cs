using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TechStore.API.Data;

namespace TechStore.API.Tests;

// Every test class in the "api" collection shares one running API and one throwaway database,
// so each test makes its own customers, products and coupons rather than relying on others'.
[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFactory>;

/// <summary>
/// Runs the real API in memory against a new SQL Server database, which is deleted afterwards.
/// TEST_SQL is the server to use (CI sets it to a SQL Server container); locally it's LocalDB.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string SqlServer = Environment.GetEnvironmentVariable("TEST_SQL")
        ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";
    private readonly string _connStr = $"{SqlServer};Database=techstore_tests_{Guid.NewGuid():N}";

    // Throwaway database, so a fresh random password per run
    public string Password { get; } = $"Test{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}a1";
    public string AdminToken { get; private set; } = "";
    public HttpClient Client { get; private set; } = null!;

    // Delivery fees every test can rely on: Lagos costs ₦3,000; anywhere else isn't delivered to
    public const decimal LagosFee = 3000m;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("KeyVault:Url", "");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connStr);
        builder.UseSetting("AzureStorage:ConnectionString", "UseDevelopmentStorage=true");
        builder.UseSetting("Jwt:Secret", "test-only-secret-test-only-secret-test-only-secret");
        builder.UseSetting("Email:From", ""); // emails are skipped
        builder.ConfigureTestServices(services =>
        {
            // Tests run the unpaid-order sweep themselves, when they're ready for it
            foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService)
                         && d.ImplementationType?.Name is "UnpaidOrderService" or "TokenCleanupService").ToList())
                services.Remove(d);
        });
    }

    public async Task InitializeAsync()
    {
        // Each request comes from a different made-up address, so the sign-in limit
        // (10 a minute per visitor) doesn't trip across the whole run
        Client = CreateDefaultClient(new ForwardedForHandler());

        var adminEmail = await Register("Ada");
        await Db(db => db.Users.Where(u => u.Email == adminEmail)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, "Admin").SetProperty(u => u.StoreId, 1)));
        AdminToken = await Login(adminEmail);

        await Ok(Call(HttpMethod.Post, "/bank-accounts", AdminToken,
            new { bankName = "Zenith Bank", accountName = "Hytel Phones Ltd", accountNumber = "1234567890", isActive = true }));
        await Ok(Call(HttpMethod.Put, "/delivery-rates", AdminToken, new[] { new { state = "Lagos", fee = (decimal?)LagosFee } }));
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Db(db => db.Database.EnsureDeletedAsync());
        await base.DisposeAsync();
    }

    // ── Calling the API ──

    public async Task<Res> Call(HttpMethod method, string path, string? token = null, object? body = null, string? fromIp = null)
    {
        using var req = new HttpRequestMessage(method, "/api" + path);
        if (token != null) req.Headers.Authorization = new("Bearer", token);
        if (fromIp != null) req.Headers.Add("X-Forwarded-For", fromIp);
        if (body != null) req.Content = JsonContent.Create(body);
        using var res = await Client.SendAsync(req);
        var text = await res.Content.ReadAsStringAsync();
        JsonNode? data = null;
        try { data = text.Length > 0 ? JsonNode.Parse(text) : null; } catch (System.Text.Json.JsonException) { }
        return new Res(res.StatusCode, data);
    }

    public static async Task<Res> Ok(Task<Res> call)
    {
        var r = await call;
        Assert.True((int)r.Status < 300, $"set-up call failed: {r}");
        return r;
    }

    // ── People ──

    public static string Unique(string name) => $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";

    public async Task<string> Register(string firstName)
    {
        var email = Unique(firstName);
        await Ok(Call(HttpMethod.Post, "/auth/register", body: new { firstName, lastName = "Test", email, password = Password }));
        return email;
    }

    public async Task<string> Login(string email)
    {
        var r = await Ok(Call(HttpMethod.Post, "/auth/login", body: new { email, password = Password }));
        return r.Str("accessToken");
    }

    public async Task<(string Email, string Token, int Id)> NewCustomer(string firstName)
    {
        var email = await Register(firstName);
        return (email, await Login(email), await UserId(email));
    }

    public async Task<(string Email, string Token, int Id)> NewStaff(string firstName, string role, string? token = null)
    {
        var email = Unique(firstName);
        await Ok(Call(HttpMethod.Post, "/admin/users/staff", token ?? AdminToken,
            new { firstName, lastName = "Test", email, password = Password, role, storeId = 1 }));
        return (email, await Login(email), await UserId(email));
    }

    public async Task<string> NewRole(string name, params string[] permissions)
    {
        var unique = $"{name} {Guid.NewGuid().ToString("N")[..6]}";
        await Ok(Call(HttpMethod.Post, "/roles", AdminToken, new { name = unique, permissions }));
        return unique;
    }

    public Task<int> UserId(string email) => Db(db => db.Users.Where(u => u.Email == email).Select(u => u.Id).SingleAsync());
    public Task<string> RoleOf(string email) => Db(db => db.Users.Where(u => u.Email == email).Select(u => u.Role).SingleAsync());

    // ── Products, shops and stock ──

    public async Task<int> NewProduct(decimal price, int stock)
    {
        var sku = $"T-{Guid.NewGuid().ToString("N")[..10]}";
        var r = await Ok(Call(HttpMethod.Post, "/products", AdminToken, new
        {
            name = $"Test {sku}", sku, price, categoryId = 5, initialStock = stock, lowStockThreshold = 1,
            brand = "Oraimo", description = "", imageUrl = "", isActive = true, isFeatured = false,
        }));
        return r.Int("id");
    }

    public async Task<int> NewShop(string name)
    {
        var code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var r = await Ok(Call(HttpMethod.Post, "/stores", AdminToken,
            new { name = $"{name} {code}", code, address = "12 Allen Avenue", city = "Ikeja", phone = "08011112222" }));
        return r.Int("id");
    }

    /// <summary>"on hand/held for orders" at one shop, e.g. "10/1"</summary>
    public async Task<string> Stock(int storeId, int productId)
    {
        var inv = await Db(db => db.Inventories.AsNoTracking().SingleAsync(i => i.StoreId == storeId && i.ProductId == productId));
        return $"{inv.QuantityOnHand}/{inv.QuantityReserved}";
    }

    // ── Orders ──

    /// <summary>Fills the cart and checks out by bank transfer, delivering to Lagos unless overridden.</summary>
    public async Task<Res> Checkout(string token, (int ProductId, int Quantity)[] items, object? overrides = null)
    {
        await Call(HttpMethod.Delete, "/cart", token);
        foreach (var (productId, quantity) in items)
            await Ok(Call(HttpMethod.Post, "/cart/items", token, new { productId, quantity }));

        var body = new JsonObject
        {
            ["shippingFirstName"] = "Bayo", ["shippingLastName"] = "Test", ["shippingCountry"] = "Nigeria",
            ["shippingPhone"] = "08012345678", ["paymentMethod"] = "BankTransfer",
            ["shippingAddress"] = "1 Test Street", ["shippingCity"] = "Ikeja",
            ["shippingProvince"] = "Lagos", ["shippingPostalCode"] = "100001",
        };
        if (overrides != null)
            foreach (var (key, value) in JsonSerializerHelpers.ToObject(overrides))
                body[key] = value?.DeepClone();
        return await Call(HttpMethod.Post, "/orders/checkout", token, body);
    }

    public Task<Res> SetStatus(int orderId, string status) =>
        Call(HttpMethod.Put, $"/orders/{orderId}/status", AdminToken, new { status });

    public Task<Res> MarkPaid(int orderId, string? reference = null) =>
        Call(HttpMethod.Post, $"/orders/{orderId}/mark-paid", AdminToken, new { reference });

    // ── Database ──

    public async Task<T> Db<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async Task<T> Service<TService, T>(Func<TService, Task<T>> use) where TService : notnull
    {
        using var scope = Services.CreateScope();
        return await use(scope.ServiceProvider.GetRequiredService<TService>());
    }

    private sealed class ForwardedForHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (!request.Headers.Contains("X-Forwarded-For"))
                request.Headers.Add("X-Forwarded-For", $"10.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}.{Random.Shared.Next(1, 255)}");
            return base.SendAsync(request, ct);
        }
    }
}

/// <summary>A response: its status and its JSON body, with helpers for reading fields.</summary>
public record Res(HttpStatusCode Status, JsonNode? Data)
{
    public string Message =>
        Data is JsonObject o
            ? o["message"]?.GetValue<string>()
              ?? (o["errors"] as JsonObject)?.Select(e => (e.Value as JsonArray)?.FirstOrDefault()?.GetValue<string>()).FirstOrDefault()
              ?? ""
            : "";

    public JsonNode? this[string field] => Data?[field];
    public string Str(string field) => Data?[field]?.GetValue<string>() ?? "";
    public int Int(string field) => Data![field]!.GetValue<int>();
    public decimal Num(string field) => Data![field]!.GetValue<decimal>();
    public bool Has(string field) => Data is JsonObject o && o[field] != null;

    public override string ToString() => $"{(int)Status} {Message}";
}

internal static class JsonSerializerHelpers
{
    public static JsonObject ToObject(object value) =>
        System.Text.Json.JsonSerializer.SerializeToNode(value) as JsonObject ?? new JsonObject();
}
