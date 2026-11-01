using System.Net;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Tests;

// Coupon limits and the sweep that reminds about, then cancels, orders not paid in time
[Collection("api")]
public class CouponAndPaymentTests(ApiFactory api)
{
    private static string Code() => "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    [Fact]
    public async Task A_fixed_coupon_never_takes_off_more_than_the_basket()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);
        var code = Code();
        await ApiFactory.Ok(api.Call(HttpMethod.Post, "/coupons", api.AdminToken, new { code, type = "FixedAmount", value = 50000, isActive = true }));

        var check = await api.Call(HttpMethod.Post, "/coupons/validate", buyer.Token, new { code, subTotal = 20000 });
        Assert.Equal(20000m, check.Num("discountAmount"));
        Assert.Equal(0m, check.Num("finalTotal"));

        var r = await api.Checkout(buyer.Token, [(phone, 1)], new { couponCode = code });
        Assert.Equal(HttpStatusCode.Created, r.Status);
        Assert.Equal(ApiFactory.LagosFee, r.Num("total")); // just the delivery fee, never below it
    }

    [Fact]
    public async Task Per_customer_and_overall_coupon_limits_hold_and_cancelling_gives_a_use_back()
    {
        var bayo = await api.NewCustomer("Bayo");
        var bisi = await api.NewCustomer("Bisi");
        var tobi = await api.NewCustomer("Tobi");
        var phone = await api.NewProduct(20000, stock: 20);
        var code = Code();
        await ApiFactory.Ok(api.Call(HttpMethod.Post, "/coupons", api.AdminToken,
            new { code, type = "Percentage", value = 10, usesPerCustomer = 1, usageLimit = 2, isActive = true }));
        Task<int> usedCount() => api.Db(db => db.Coupons.Where(c => c.Code == code).Select(c => c.UsedCount).SingleAsync());

        var first = await api.Checkout(bayo.Token, [(phone, 1)], new { couponCode = code });
        Assert.Equal(2000m, first.Num("discountAmount"));

        var again = await api.Call(HttpMethod.Post, "/coupons/validate", bayo.Token, new { code, subTotal = 20000 });
        Assert.False(again["isValid"]!.GetValue<bool>());
        Assert.Contains("already used", again.Str("errorMessage"));
        Assert.Contains("already used this coupon", (await api.Checkout(bayo.Token, [(phone, 1)], new { couponCode = code })).Message);

        await ApiFactory.Ok(api.SetStatus(first.Int("id"), "Cancelled"));
        Assert.Equal(0, await usedCount());

        Assert.Equal(HttpStatusCode.Created, (await api.Checkout(bayo.Token, [(phone, 1)], new { couponCode = code })).Status);
        Assert.Equal(HttpStatusCode.Created, (await api.Checkout(bisi.Token, [(phone, 1)], new { couponCode = code })).Status);
        var third = await api.Checkout(tobi.Token, [(phone, 1)], new { couponCode = code });
        Assert.Equal(HttpStatusCode.BadRequest, third.Status);
        Assert.Contains("usage limit", third.Message);
        Assert.Equal(2, await usedCount());
    }

    [Fact]
    public async Task Unpaid_orders_get_a_reminder_then_are_cancelled_at_the_deadline()
    {
        var buyer = await api.NewCustomer("Bisi");
        var phone = await api.NewProduct(20000, stock: 10);
        async Task<int> order() => (await api.Checkout(buyer.Token, [(phone, 1)])).Int("id");
        var remindMe = await order();
        var expireMe = await order();
        var paidLate = await order();
        var noDeadline = await order();
        await ApiFactory.Ok(api.MarkPaid(paidLate));

        await api.Db(async db =>
        {
            await db.Orders.Where(o => o.Id == remindMe)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.PaymentDueAt, DateTime.UtcNow.AddHours(1)));
            await db.Orders.Where(o => o.Id == expireMe || o.Id == paidLate)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.PaymentDueAt, DateTime.UtcNow.AddMinutes(-1)));
            return await db.Orders.Where(o => o.Id == noDeadline).ExecuteUpdateAsync(s => s
                .SetProperty(o => o.PaymentDueAt, (DateTime?)null)
                .SetProperty(o => o.CreatedAt, DateTime.UtcNow.AddDays(-10)));
        });
        Assert.Equal("10/4", await api.Stock(1, phone)); // all four hold stock, paid or not, until they leave the shop

        await api.Service<IOrderService, int>(s => s.ProcessUnpaidOrdersAsync());

        var orders = await api.Db(db => db.Orders.AsNoTracking()
            .Where(o => o.Id == remindMe || o.Id == expireMe || o.Id == paidLate || o.Id == noDeadline)
            .ToDictionaryAsync(o => o.Id));
        Assert.Equal(OrderStatus.Cancelled, orders[expireMe].Status);
        Assert.Equal(PaymentStatus.Pending, orders[expireMe].PaymentStatus);
        Assert.Equal(OrderStatus.Pending, orders[remindMe].Status);
        Assert.NotNull(orders[remindMe].PaymentReminderSentAt);
        Assert.Equal(OrderStatus.Confirmed, orders[paidLate].Status);
        Assert.Equal(OrderStatus.Pending, orders[noDeadline].Status);
        Assert.Equal("10/3", await api.Stock(1, phone)); // the cancelled one's stock is back on sale

        var history = await api.Call(HttpMethod.Get, $"/orders/{expireMe}/history", api.AdminToken);
        Assert.Contains(history.Data!.AsArray(), h =>
            h!["changedByName"]?.GetValue<string>() == "System" && h["note"]?.GetValue<string>() == "Not paid within 24 hours");
    }
}
