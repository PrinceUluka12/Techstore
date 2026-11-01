using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Tests;

// Buying without an account: the cart comes from the browser, and the order is tracked by its private link
[Collection("api")]
public class GuestCheckoutTests(ApiFactory api)
{
    // The order details: delivery to Lagos unless overridden
    private static JsonObject Lagos(object? overrides = null)
    {
        var order = new JsonObject
        {
            ["shippingFirstName"] = "Ngozi", ["shippingLastName"] = "Test", ["shippingCountry"] = "Nigeria",
            ["shippingPhone"] = "08012345678", ["paymentMethod"] = "BankTransfer",
            ["shippingAddress"] = "1 Test Street", ["shippingCity"] = "Ikeja",
            ["shippingProvince"] = "Lagos", ["shippingPostalCode"] = "100001",
        };
        if (overrides != null)
            foreach (var (key, value) in JsonSerializerHelpers.ToObject(overrides))
                order[key] = value?.DeepClone();
        return order;
    }

    private Task<Res> Checkout(string email, (int ProductId, int Quantity)[] items, object? order = null, string? fromIp = null) =>
        api.Call(HttpMethod.Post, "/orders/guest/checkout", body: new
        {
            email,
            items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }),
            order = order ?? Lagos(),
        }, fromIp: fromIp);

    [Fact]
    public async Task A_guest_order_has_no_account_uses_our_prices_and_has_a_private_link()
    {
        var phone = await api.NewProduct(20000, stock: 10);
        var email = ApiFactory.Unique("ngozi");

        var r = await Checkout(email, [(phone, 2)]);

        Assert.Equal(HttpStatusCode.Created, r.Status);
        Assert.False(r.Has("userId"));
        Assert.Equal("Ngozi Test", r.Str("customerName"));
        Assert.Equal(email, r.Str("customerEmail"));
        Assert.Equal(2 * 20000m + ApiFactory.LagosFee, r.Num("total"));
        Assert.Equal("10/2", await api.Stock(1, phone));
        var token = r.Str("guestToken");
        Assert.Equal(43, token.Length);

        var tracked = await api.Call(HttpMethod.Get, $"/orders/guest/{token}");
        Assert.Equal(HttpStatusCode.OK, tracked.Status);
        Assert.Equal(r.Str("orderNumber"), tracked.Str("orderNumber"));
        Assert.False(tracked.Has("guestToken")); // only handed out once, at checkout
        var history = await api.Call(HttpMethod.Get, $"/orders/guest/{token}/history");
        Assert.Contains(history.Data!.AsArray(), h => h!["note"]?.GetValue<string>() == "Order placed without an account");
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Call(HttpMethod.Get, $"/orders/{r.Int("id")}")).Status); // the id alone isn't enough
        Assert.Equal(HttpStatusCode.NotFound, (await api.Call(HttpMethod.Get, $"/orders/guest/{token[..^1]}x")).Status);

        // Staff find and handle it like any other order
        var found = await api.Call(HttpMethod.Get, $"/orders?search={Uri.EscapeDataString(email)}", api.AdminToken);
        Assert.Equal(1, found.Int("total"));
        Assert.Equal(HttpStatusCode.OK, (await api.MarkPaid(r.Int("id"))).Status);
        Assert.Equal("Confirmed", (await api.Call(HttpMethod.Get, $"/orders/guest/{token}")).Str("status"));
    }

    [Fact]
    public async Task A_guest_needs_a_phone_and_an_email_that_isnt_already_an_account()
    {
        var phone = await api.NewProduct(20000, stock: 10);
        var member = await api.NewCustomer("Bayo");

        var r = await Checkout(ApiFactory.Unique("ngozi"), [(phone, 1)], Lagos(new { shippingPhone = "" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("phone", r.Message);

        r = await Checkout(member.Email.ToUpperInvariant(), [(phone, 1)]);
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("already an account", r.Message);

        r = await Checkout("not-an-email", [(phone, 1)]);
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Equal("10/0", await api.Stock(1, phone));
    }

    [Fact]
    public async Task A_name_has_to_be_a_name_because_it_goes_into_emails_sent_to_any_address()
    {
        var phone = await api.NewProduct(20000, stock: 10);

        // Markup in place of a name would be emailed, from the shop, to whoever the "guest" chose
        var r = await Checkout(ApiFactory.Unique("victim"), [(phone, 1)], Lagos(new { shippingFirstName = "<a href=//evil.example>Pay here</a>" }));
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("letters", r.Message);
        Assert.Equal("10/0", await api.Stock(1, phone));

        // Real names, tone marks and all, are fine
        r = await Checkout(ApiFactory.Unique("adebayo"), [(phone, 1)], Lagos(new { shippingFirstName = "Adébáyọ̀", shippingLastName = "O'Neil-Smith Jr." }));
        Assert.Equal(HttpStatusCode.Created, r.Status);
        Assert.Equal("Adébáyọ̀ O'Neil-Smith Jr.", r.Str("customerName"));
    }

    [Fact]
    public async Task A_browser_cart_is_priced_from_the_catalogue()
    {
        var phone = await api.NewProduct(20000, stock: 10);
        var gone = await api.NewProduct(5000, stock: 10);
        await ApiFactory.Ok(api.Call(HttpMethod.Put, $"/products/{gone}", api.AdminToken, new { isActive = false }));

        var r = await api.Call(HttpMethod.Post, "/cart/preview", body: new
        {
            items = new[] { new { productId = phone, quantity = 1 }, new { productId = gone, quantity = 1 }, new { productId = phone, quantity = 2 } },
        });

        Assert.Equal(HttpStatusCode.OK, r.Status);
        var item = Assert.Single(r["items"]!.AsArray())!; // the switched-off product is left out
        Assert.Equal(3, item["quantity"]!.GetValue<int>());
        Assert.Equal(10, item["availableStock"]!.GetValue<int>());
        Assert.Equal(60000m, r.Num("subTotal"));

        // Checking out something that's been switched off is refused, with nothing held
        var co = await Checkout(ApiFactory.Unique("ngozi"), [(phone, 1), (gone, 1)]);
        Assert.Equal(HttpStatusCode.BadRequest, co.Status);
        Assert.Equal("10/0", await api.Stock(1, phone));
    }

    [Fact]
    public async Task Coupons_work_for_guests_and_per_customer_limits_count_by_email()
    {
        var phone = await api.NewProduct(20000, stock: 10);
        var code = "G" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await ApiFactory.Ok(api.Call(HttpMethod.Post, "/coupons", api.AdminToken,
            new { code, type = "Percentage", value = 10, usesPerCustomer = 1, isActive = true }));
        var email = ApiFactory.Unique("ngozi");

        var check = await api.Call(HttpMethod.Post, "/coupons/validate", body: new { code, subTotal = 20000 }); // signed out
        Assert.True(check["isValid"]!.GetValue<bool>());

        var first = await Checkout(email, [(phone, 1)], Lagos(new { couponCode = code }));
        Assert.Equal(2000m, first.Num("discountAmount"));
        var again = await Checkout(email, [(phone, 1)], Lagos(new { couponCode = code }));
        Assert.Contains("already used this coupon", again.Message);
        Assert.Equal(HttpStatusCode.Created, (await Checkout(ApiFactory.Unique("tobi"), [(phone, 1)], Lagos(new { couponCode = code }))).Status);
    }

    [Fact]
    public async Task A_guest_can_find_their_order_with_its_number_and_email()
    {
        var phone = await api.NewProduct(20000, stock: 10);
        var email = ApiFactory.Unique("ngozi");
        var order = await Checkout(email, [(phone, 1)]);

        var r = await api.Call(HttpMethod.Post, "/orders/guest/find", body: new { orderNumber = order.Str("orderNumber"), email = $"  {email.ToUpperInvariant()} " });
        Assert.Equal(order.Str("guestToken"), r.Str("token"));

        r = await api.Call(HttpMethod.Post, "/orders/guest/find", body: new { orderNumber = order.Str("orderNumber"), email = ApiFactory.Unique("someone") });
        Assert.Equal(HttpStatusCode.NotFound, r.Status);
    }

    [Fact]
    public async Task A_guest_can_collect_from_a_shop_that_has_their_cart()
    {
        var watch = await api.NewProduct(80000, stock: 3);
        var lagos = await api.NewShop("Hytel Lagos");

        var shops = await api.Call(HttpMethod.Post, "/orders/guest/pickup-shops", body: new { items = new[] { new { productId = watch, quantity = 1 } } });
        Assert.Contains(shops.Data!.AsArray(), s => s!["id"]!.GetValue<int>() == 1 && s["hasStock"]!.GetValue<bool>());
        Assert.Contains(shops.Data!.AsArray(), s => s!["id"]!.GetValue<int>() == lagos && !s["hasStock"]!.GetValue<bool>());

        var r = await Checkout(ApiFactory.Unique("ngozi"), [(watch, 1)],
            Lagos(new { fulfilment = "Pickup", pickupStoreId = 1, shippingAddress = "", shippingCity = "", shippingProvince = "", shippingPostalCode = "" }));
        Assert.Equal(HttpStatusCode.Created, r.Status);
        Assert.Equal(80000m, r.Num("total"));
        Assert.Equal("3/1", await api.Stock(1, watch));
    }

    [Fact]
    public async Task An_unpaid_guest_order_is_cancelled_at_the_deadline()
    {
        var phone = await api.NewProduct(20000, stock: 10);
        var order = (await Checkout(ApiFactory.Unique("ngozi"), [(phone, 1)])).Int("id");
        await api.Db(db => db.Orders.Where(o => o.Id == order)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.PaymentDueAt, DateTime.UtcNow.AddMinutes(-1))));

        await api.Service<IOrderService, int>(s => s.ProcessUnpaidOrdersAsync());

        Assert.Equal(OrderStatus.Cancelled, await api.Db(db => db.Orders.Where(o => o.Id == order).Select(o => o.Status).SingleAsync()));
        Assert.Equal("10/0", await api.Stock(1, phone));
    }

    [Fact]
    public async Task Guest_checkout_is_limited_to_10_orders_per_10_minutes_per_visitor()
    {
        var phone = await api.NewProduct(1000, stock: 20);
        var ip = $"203.0.113.{Random.Shared.Next(1, 255)}";

        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Created, (await Checkout(ApiFactory.Unique("ngozi"), [(phone, 1)], fromIp: ip)).Status);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Checkout(ApiFactory.Unique("ngozi"), [(phone, 1)], fromIp: ip)).Status);
        Assert.Equal("20/10", await api.Stock(1, phone));
    }
}
