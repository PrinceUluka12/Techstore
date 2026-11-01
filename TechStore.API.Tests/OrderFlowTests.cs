using System.Net;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Models;

namespace TechStore.API.Tests;

// Checkout (delivery and pickup), the order steps staff can take, and who can see an order
[Collection("api")]
public class OrderFlowTests(ApiFactory api)
{
    [Fact]
    public async Task Delivery_adds_the_states_fee_and_holds_the_stock()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);

        var r = await api.Checkout(buyer.Token, [(phone, 1)]);

        Assert.Equal(HttpStatusCode.Created, r.Status);
        Assert.Equal("Delivery", r.Str("fulfilment"));
        Assert.Equal(ApiFactory.LagosFee, r.Num("shippingCost"));
        Assert.Equal(23000m, r.Num("total"));
        var dueHours = (DateTime.Parse(r.Str("paymentDueAt")) - DateTime.Parse(r.Str("createdAt"))).TotalHours;
        Assert.InRange(dueHours, 23.99, 24.01);
        Assert.True(r["canMarkPaid"]!.GetValue<bool>());
        Assert.Equal("""["Cancelled"]""", r["nextStatuses"]!.ToJsonString());
        Assert.Equal("10/1", await api.Stock(1, phone));
    }

    [Fact]
    public async Task No_delivery_to_a_state_without_a_fee()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);

        var r = await api.Checkout(buyer.Token, [(phone, 1)], new { shippingProvince = "Kano" });

        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("don't deliver to Kano", r.Message);
        Assert.Equal("10/0", await api.Stock(1, phone));
    }

    [Fact]
    public async Task Pickup_is_free_and_holds_the_stock_at_the_chosen_shop()
    {
        var buyer = await api.NewCustomer("Bayo");
        var watch = await api.NewProduct(80000, stock: 5);
        var lagos = await api.NewShop("Hytel Lagos");
        var noAddress = new { fulfilment = "Pickup", pickupStoreId = lagos, shippingAddress = "", shippingCity = "", shippingProvince = "", shippingPostalCode = "" };

        var r = await api.Checkout(buyer.Token, [(watch, 1)], new { fulfilment = "Pickup" });
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("shop", r.Message, StringComparison.OrdinalIgnoreCase);

        r = await api.Checkout(buyer.Token, [(watch, 1)], noAddress);
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("only has 0", r.Message);

        await ApiFactory.Ok(api.Call(HttpMethod.Post, $"/inventory/product/{watch}/restock?storeId={lagos}", api.AdminToken, new { quantity = 2 }));
        r = await api.Checkout(buyer.Token, [(watch, 1)], noAddress);

        Assert.Equal(HttpStatusCode.Created, r.Status);
        Assert.Equal("Pickup", r.Str("fulfilment"));
        Assert.Equal(0m, r.Num("shippingCost"));
        Assert.Equal(80000m, r.Num("total"));
        Assert.Equal("12 Allen Avenue, Ikeja", r.Str("pickupAddress"));
        Assert.Equal("2/1", await api.Stock(lagos, watch));
        Assert.Equal("5/0", await api.Stock(1, watch));
    }

    [Fact]
    public async Task A_delivery_order_goes_paid_then_shipped_then_delivered()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);
        var order = (await api.Checkout(buyer.Token, [(phone, 1)])).Int("id");

        var r = await api.SetStatus(order, "Shipped");
        Assert.Equal(HttpStatusCode.BadRequest, r.Status); // not paid yet
        r = await api.SetStatus(order, "Confirmed");
        Assert.Contains("Mark as paid", r.Message);

        r = await api.MarkPaid(order, "ZEN-001");
        Assert.Equal(HttpStatusCode.OK, r.Status);
        Assert.Equal("Confirmed", r.Str("status"));
        Assert.Equal("Paid", r.Str("paymentStatus"));
        Assert.False(r.Has("paymentDueAt"));
        Assert.Equal("""["Processing","Shipped","Cancelled"]""", r["nextStatuses"]!.ToJsonString());
        var history = await api.Call(HttpMethod.Get, $"/orders/{order}/history", api.AdminToken);
        Assert.Contains(history.Data!.AsArray(), h => h!["note"]?.GetValue<string>() == "Payment received (ref: ZEN-001)");
        Assert.Contains("already", (await api.MarkPaid(order)).Message);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.SetStatus(order, "ReadyForPickup")).Status);
        Assert.Equal(HttpStatusCode.OK, (await api.SetStatus(order, "Shipped")).Status);
        Assert.Equal(HttpStatusCode.OK, (await api.SetStatus(order, "Delivered")).Status);
        Assert.Equal("9/0", await api.Stock(1, phone));

        // No returns: a delivered order can't be cancelled, and the stock stays sold
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SetStatus(order, "Cancelled")).Status);
        Assert.Equal("9/0", await api.Stock(1, phone));
    }

    [Fact]
    public async Task A_pickup_order_is_ready_then_collected()
    {
        var buyer = await api.NewCustomer("Bayo");
        var watch = await api.NewProduct(80000, stock: 3);
        var order = (await api.Checkout(buyer.Token, [(watch, 1)],
            new { fulfilment = "Pickup", pickupStoreId = 1, shippingProvince = "" })).Int("id");
        await ApiFactory.Ok(api.MarkPaid(order));

        Assert.Equal(HttpStatusCode.BadRequest, (await api.SetStatus(order, "Shipped")).Status);
        var r = await api.SetStatus(order, "ReadyForPickup");
        Assert.Equal("""["Delivered","Cancelled"]""", r["nextStatuses"]!.ToJsonString());
        Assert.Equal(HttpStatusCode.OK, (await api.SetStatus(order, "Delivered")).Status);
        Assert.Equal("2/0", await api.Stock(1, watch));
    }

    [Fact]
    public async Task Cancelling_releases_the_stock_and_marks_paid_orders_refunded()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);

        var paid = (await api.Checkout(buyer.Token, [(phone, 2)])).Int("id");
        await ApiFactory.Ok(api.MarkPaid(paid));
        var r = await api.SetStatus(paid, "Cancelled");
        Assert.Equal("Refunded", r.Str("paymentStatus"));

        var unpaid = (await api.Checkout(buyer.Token, [(phone, 1)])).Int("id");
        r = await api.SetStatus(unpaid, "Cancelled");
        Assert.Equal("Pending", r.Str("paymentStatus"));

        Assert.Equal("10/0", await api.Stock(1, phone));
    }

    [Fact]
    public async Task Two_cancels_at_once_release_the_stock_only_once()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);
        var order = (await api.Checkout(buyer.Token, [(phone, 1)])).Int("id");

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => api.SetStatus(order, "Cancelled")));

        // Late arrivals either find it already cancelled (nothing to do) or are told it just changed
        Assert.All(results, x => Assert.Contains(x.Status, new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest }));
        Assert.Equal("10/0", await api.Stock(1, phone));
        Assert.Equal(1, await api.Db(db => db.OrderStatusLogs.CountAsync(l => l.OrderId == order && l.ToStatus == OrderStatus.Cancelled)));
    }

    [Fact]
    public async Task My_orders_are_paged_newest_first()
    {
        var buyer = await api.NewCustomer("Bayo");
        var phone = await api.NewProduct(20000, stock: 10);
        var ids = new List<int>();
        for (var i = 0; i < 6; i++) ids.Add((await api.Checkout(buyer.Token, [(phone, 1)])).Int("id"));

        var first = await api.Call(HttpMethod.Get, "/orders/my?page=1&pageSize=5", buyer.Token);
        Assert.Equal(6, first.Int("total"));
        Assert.Equal(2, first.Int("totalPages"));
        Assert.Equal(ids[5], first["items"]!.AsArray()[0]!["id"]!.GetValue<int>());
        Assert.Equal(5, first["items"]!.AsArray().Count);

        var second = await api.Call(HttpMethod.Get, "/orders/my?page=2&pageSize=5", buyer.Token);
        Assert.Equal(ids[0], Assert.Single(second["items"]!.AsArray())!["id"]!.GetValue<int>());

        var huge = await api.Call(HttpMethod.Get, "/orders/my?page=1&pageSize=100000", buyer.Token);
        Assert.Equal(50, huge.Int("pageSize"));
    }

    [Fact]
    public async Task Only_the_customer_and_staff_who_can_view_orders_can_open_an_order()
    {
        var buyer = await api.NewCustomer("Bayo");
        var other = await api.NewCustomer("Bisi");
        var viewer = await api.NewStaff("Vee", await api.NewRole("Order Viewer", "orders.view"));
        var phone = await api.NewProduct(20000, stock: 10);
        var order = (await api.Checkout(buyer.Token, [(phone, 1)])).Int("id");

        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Get, $"/orders/{order}", buyer.Token)).Status);
        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Get, $"/orders/{order}", viewer.Token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Get, $"/orders/{order}", other.Token)).Status);
    }
}
