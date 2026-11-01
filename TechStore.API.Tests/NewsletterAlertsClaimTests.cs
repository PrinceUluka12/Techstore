using System.Net;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Tests;

// The newsletter sign-up, the morning low-stock email, and adding guest orders to an account
[Collection("api")]
public class NewsletterAlertsClaimTests(ApiFactory api)
{
    private Task<Res> Subscribe(string email) => api.Call(HttpMethod.Post, "/newsletter/subscribe", body: new { email });

    [Fact]
    public async Task Signing_up_twice_keeps_one_entry_and_unsubscribing_takes_them_off_the_download()
    {
        var email = ApiFactory.Unique("reader");

        Assert.Equal(HttpStatusCode.OK, (await Subscribe(email)).Status);
        Assert.Equal(HttpStatusCode.OK, (await Subscribe(email.ToUpperInvariant())).Status); // same answer, same entry
        var welcome = Assert.Single(api.Emails.To(email), e => e.Kind == "NewsletterWelcome");
        var unsubscribeLink = (string)welcome.Detail!;
        Assert.StartsWith("https://shop.test/newsletter/unsubscribe?token=", unsubscribeLink);

        var list = await api.Call(HttpMethod.Get, $"/newsletter?search={Uri.EscapeDataString(email)}", api.AdminToken);
        Assert.Equal(1, list.Int("total"));
        Assert.Contains(email, await Csv());

        var token = unsubscribeLink.Split("token=")[1];
        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Post, "/newsletter/unsubscribe", body: new { token })).Status);
        Assert.DoesNotContain(email, await Csv());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Call(HttpMethod.Post, "/newsletter/unsubscribe", body: new { token = "nope" })).Status);

        await Subscribe(email); // signing up again puts them back, with a fresh welcome
        Assert.Contains(email, await Csv());
        Assert.Equal(2, api.Emails.To(email).Count(e => e.Kind == "NewsletterWelcome"));
    }

    [Fact]
    public async Task The_download_is_for_staff_only_and_safe_to_open_in_a_spreadsheet()
    {
        var customer = await api.NewCustomer("Bayo");
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Get, "/newsletter", customer.Token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Get, "/newsletter/export", customer.Token)).Status);

        var sneaky = $"=cmd{Guid.NewGuid():N}@test.local";
        await Subscribe(sneaky);
        Assert.Contains($"'{sneaky}", await Csv()); // not run as a formula
        Assert.Equal(HttpStatusCode.BadRequest, (await Subscribe("not-an-email")).Status);
    }

    [Fact]
    public async Task The_same_email_signing_up_several_times_at_once_is_one_entry()
    {
        var email = ApiFactory.Unique("eager");

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Subscribe(email)));

        Assert.All(results, r => Assert.Equal(HttpStatusCode.OK, r.Status)); // the duplicates get the same friendly answer
        Assert.Equal(1, await api.Db(db => db.NewsletterSubscribers.CountAsync(s => s.Email == email)));
        Assert.Single(api.Emails.To(email), e => e.Kind == "NewsletterWelcome");
    }

    private async Task<string> Csv()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/newsletter/export");
        req.Headers.Authorization = new("Bearer", api.AdminToken);
        using var res = await api.Client.SendAsync(req);
        Assert.Equal("text/csv", res.Content.Headers.ContentType?.MediaType);
        return await res.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Stock_staff_get_one_low_stock_email_a_morning_for_their_own_shop()
    {
        var lagos = await api.NewShop("Hytel Lagos");
        var stockRole = await api.NewRole("Stock Keeper", "inventory.manage");
        var keeper = await api.NewStaff("Kemi", stockRole); // home shop: the main shop
        var cashier = await api.NewStaff("Tilly", await api.NewRole("Cashier", "pos.sell"));
        await api.Db(db => db.Users.Where(u => u.Id == keeper.Id).ExecuteUpdateAsync(s => s.SetProperty(u => u.StoreId, lagos)));
        var phone = await api.NewProduct(20000, stock: 0); // nothing in any shop: low everywhere
        Task<int> run(DateTime utc) => api.Service<IStockAlertService, int>(s => s.SendDailyLowStockAlertAsync(utc));

        Assert.Equal(0, await run(new DateTime(2031, 3, 1, 6, 0, 0, DateTimeKind.Utc))); // 7am in Nigeria: too early
        Assert.True(await run(new DateTime(2031, 3, 1, 7, 30, 0, DateTimeKind.Utc)) >= 2); // 8:30am: the Admin and Kemi
        Assert.Equal(0, await run(new DateTime(2031, 3, 1, 15, 0, 0, DateTimeKind.Utc))); // once a day

        var kemis = Assert.Single(api.Emails.To(keeper.Email), e => e.Kind == "LowStock");
        var lines = (IReadOnlyList<LowStockLine>)kemis.Detail!;
        Assert.All(lines, l => Assert.StartsWith("Hytel Lagos", l.Shop)); // only her shop
        var sku = await api.Db(db => db.Products.Where(p => p.Id == phone).Select(p => p.SKU).SingleAsync());
        Assert.Contains(lines, l => l.Sku == sku && l.Available == 0);
        Assert.Empty(api.Emails.To(cashier.Email)); // doesn't manage stock

        Assert.True(await run(new DateTime(2031, 3, 2, 7, 0, 0, DateTimeKind.Utc)) >= 2); // and again the next morning
    }

    [Fact]
    public async Task Guest_orders_join_an_account_with_the_same_email_once_the_emailed_link_is_opened()
    {
        var product = await api.NewProduct(20000, stock: 10);
        var email = ApiFactory.Unique("ngozi");
        var guestOrder = await api.Call(HttpMethod.Post, "/orders/guest/checkout", body: new
        {
            email,
            items = new[] { new { productId = product, quantity = 1 } },
            order = new JsonObject
            {
                ["shippingFirstName"] = "Ngozi", ["shippingLastName"] = "Test", ["shippingCountry"] = "Nigeria",
                ["shippingPhone"] = "08012345678", ["paymentMethod"] = "BankTransfer", ["shippingAddress"] = "1 Test Street",
                ["shippingCity"] = "Ikeja", ["shippingProvince"] = "Lagos", ["shippingPostalCode"] = "100001",
            },
        });
        Assert.Equal(HttpStatusCode.Created, guestOrder.Status);

        // She makes an account with the same email afterwards
        await ApiFactory.Ok(api.Call(HttpMethod.Post, "/auth/register", body: new { firstName = "Ngozi", lastName = "Test", email, password = api.Password }));
        var ngozi = await api.Login(email);
        var other = await api.NewCustomer("Bayo");

        Assert.Equal(1, (await api.Call(HttpMethod.Get, "/orders/my/claimable", ngozi)).Int("count"));
        Assert.Equal(0, (await api.Call(HttpMethod.Get, "/orders/my", ngozi)).Int("total")); // not hers until the email is proven

        Assert.Equal(1, (await api.Call(HttpMethod.Post, "/orders/my/claim/request", ngozi)).Int("count"));
        var token = (string)Assert.Single(api.Emails.To(email), e => e.Kind == "ClaimOrders").Detail!;

        // The link only works for the account it was sent to, and only once
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Call(HttpMethod.Post, "/orders/my/claim", other.Token, new { token })).Status);
        Assert.Equal(1, (await api.Call(HttpMethod.Post, "/orders/my/claim", ngozi, new { token })).Int("claimed"));
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Call(HttpMethod.Post, "/orders/my/claim", ngozi, new { token })).Status);

        var mine = await api.Call(HttpMethod.Get, "/orders/my", ngozi);
        Assert.Equal(guestOrder.Str("orderNumber"), mine["items"]!.AsArray()[0]!["orderNumber"]!.GetValue<string>());
        Assert.Equal(0, (await api.Call(HttpMethod.Get, "/orders/my/claimable", ngozi)).Int("count"));
        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Get, $"/orders/guest/{guestOrder.Str("guestToken")}")).Status); // old link still works
        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Get, $"/orders/{guestOrder.Int("id")}", ngozi)).Status);
    }

    [Fact]
    public async Task Nothing_to_claim_sends_nothing()
    {
        var bayo = await api.NewCustomer("Bayo");
        Assert.Equal(0, (await api.Call(HttpMethod.Post, "/orders/my/claim/request", bayo.Token)).Int("count"));
        Assert.DoesNotContain(api.Emails.To(bayo.Email), e => e.Kind == "ClaimOrders");
    }
}
