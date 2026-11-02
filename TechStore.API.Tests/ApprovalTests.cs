using System.Net;
using Microsoft.EntityFrameworkCore;

namespace TechStore.API.Tests;

// A till asking the shop's managers to approve a discount or price change, and a manager
// answering from their own phone or computer (with a live alert) instead of coming to the till
[Collection("api")]
public class ApprovalTests(ApiFactory api)
{
    private const string Pin = "4826";

    private async Task<(string Email, string Token, int Id)> Manager(string name, int storeId = 1)
    {
        var manager = await api.NewStaff(name, await api.NewRole("Shop Manager", "pos.sell", "pos.discount.approve"), storeId: storeId);
        await ApiFactory.Ok(api.Call(HttpMethod.Put, "/pos/approval-pin", manager.Token, new { currentPassword = api.Password, pin = Pin }));
        return manager;
    }

    private async Task<(string Email, string Token, int Id)> Cashier(string name, int storeId = 1) =>
        await api.NewStaff(name, await api.NewRole("Cashier", "pos.sell"), storeId: storeId);

    // ₦50,000 product sold at ₦45,000 (a price change), or at full price with a discount
    private Task<Res> Ask(string token, int productId, decimal? unitPrice = 45000m, decimal discount = 0, int quantity = 1) =>
        api.Call(HttpMethod.Post, "/pos/approval-requests", token, new
        {
            items = new[] { new { productId, quantity, unitPrice } },
            discountAmount = discount,
            reason = "Price match with the shop next door",
        });

    private Task<Res> Sale(string token, int productId, int? approvalRequestId, decimal? unitPrice = 45000m, decimal discount = 0, int quantity = 1) =>
        api.Call(HttpMethod.Post, "/pos/sales", token, new
        {
            items = new[] { new { productId, quantity, unitPrice } },
            payments = new[] { new { method = "Cash", amount = 100000m * quantity } },
            discountAmount = discount,
            approvalRequestId,
        });

    private Task<Res> Approve(string token, int id, string pin = Pin) =>
        api.Call(HttpMethod.Post, $"/pos/approval-requests/{id}/approve", token, new { pin });

    private Task<Res> Decline(string token, int id, string? note = null) =>
        api.Call(HttpMethod.Post, $"/pos/approval-requests/{id}/decline", token, new { note });

    private static int IdOf(Res asked) => asked["request"]!["id"]!.GetValue<int>();

    [Fact]
    public async Task Only_the_shops_own_managers_get_the_alert_and_Admins_see_it_too()
    {
        var shop = await api.NewShop("Ikeja");
        var cashier = await Cashier("Tilly", shop);
        var here = await Manager("Mona", shop);
        var elsewhere = await Manager("Bisi"); // Main Shop
        var product = await api.NewProduct(50000, 5);

        await using var hereLive = await api.Live(here.Token);
        await using var elsewhereLive = await api.Live(elsewhere.Token);
        await using var adminLive = await api.Live(api.AdminToken);

        var asked = await Ask(cashier.Token, product);
        Assert.Equal(HttpStatusCode.OK, asked.Status);
        Assert.Equal(1, asked.Int("managersOnline")); // Mona; the Admin's home shop is the Main Shop
        var id = IdOf(asked);

        var alert = await hereLive.Next("ApprovalRequested", id);
        Assert.Equal("Waiting", alert["status"]!.GetValue<string>());
        Assert.Equal("Tilly Test", alert["requestedByName"]!.GetValue<string>());
        Assert.Equal(50000m, alert["normalSubTotal"]!.GetValue<decimal>());
        Assert.Equal(45000m, alert["total"]!.GetValue<decimal>());
        Assert.Equal("Price match with the shop next door", alert["reason"]!.GetValue<string>());
        await adminLive.Next("ApprovalRequested", id);
        await Task.Delay(300);
        Assert.False(elsewhereLive.Heard("ApprovalRequested", id));

        // The waiting list says the same
        var mine = (await api.Call(HttpMethod.Get, "/pos/approval-requests?waiting=true", here.Token)).Data!.AsArray();
        Assert.Contains(mine, r => r!["id"]!.GetValue<int>() == id);
        var theirs = (await api.Call(HttpMethod.Get, "/pos/approval-requests?waiting=true", elsewhere.Token)).Data!.AsArray();
        Assert.DoesNotContain(theirs, r => r!["id"]!.GetValue<int>() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Call(HttpMethod.Get, $"/pos/approval-requests/{id}", elsewhere.Token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Get, "/pos/approval-requests", cashier.Token)).Status);

        // Nobody from a shop online: the till is told straight away
        var quietShop = await api.NewShop("Lekki");
        var alone = await Cashier("Lola", quietShop);
        Assert.Equal(0, (await Ask(alone.Token, product)).Int("managersOnline"));
    }

    [Fact]
    public async Task A_manager_approves_on_their_device_and_the_till_completes_the_sale_once()
    {
        var cashier = await Cashier("Tunde");
        var manager = await Manager("Mary");
        var product = await api.NewProduct(50000, 5);
        await using var till = await api.Live(cashier.Token);

        var id = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product)));

        // Not approved yet: no sale
        Assert.Equal(HttpStatusCode.BadRequest, (await Sale(cashier.Token, product, id)).Status);

        var wrong = await Approve(manager.Token, id, "0000");
        Assert.Equal(HttpStatusCode.BadRequest, wrong.Status);
        Assert.StartsWith("Wrong PIN", wrong.Message);

        var approved = await Approve(manager.Token, id);
        Assert.Equal(HttpStatusCode.OK, approved.Status);
        var heard = await till.Next("ApprovalUpdated", id, "Approved"); // the till hears straight away
        Assert.Equal("Mary Test", heard["decidedByName"]!.GetValue<string>());

        var sale = await Sale(cashier.Token, product, id);
        Assert.Equal(HttpStatusCode.Created, sale.Status);
        Assert.Equal("Mary Test", sale.Str("discountApprovedBy"));
        Assert.Equal("Price match with the shop next door", sale.Str("discountReason"));
        Assert.Equal(45000m, sale.Num("total"));

        var used = await api.Call(HttpMethod.Get, $"/pos/approval-requests/{id}", cashier.Token);
        Assert.Equal("Used", used.Str("status"));
        Assert.Equal(sale.Str("orderNumber"), used.Str("orderNumber"));

        var again = await Sale(cashier.Token, product, id);
        Assert.Equal(HttpStatusCode.BadRequest, again.Status);
        Assert.Contains("already been used", again.Message);
        Assert.Equal("4/0", await api.Stock(1, product)); // one sold, not two
    }

    [Fact]
    public async Task An_approval_only_covers_the_cart_and_the_cashier_it_was_given_for()
    {
        var cashier = await Cashier("Tobi");
        var other = await Cashier("Ojo");
        var manager = await Manager("Musa");
        var product = await api.NewProduct(50000, 5);

        var id = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product, unitPrice: null, discount: 5000)));
        await ApiFactory.Ok(Approve(manager.Token, id));

        var moreOff = await Sale(cashier.Token, product, id, unitPrice: null, discount: 6000);
        Assert.Equal(HttpStatusCode.BadRequest, moreOff.Status);
        Assert.Contains("changed since the manager approved it", moreOff.Message);
        Assert.Equal(HttpStatusCode.BadRequest, (await Sale(cashier.Token, product, id, unitPrice: null, discount: 5000, quantity: 2)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Sale(cashier.Token, product, id, unitPrice: 49000m, discount: 5000)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await Sale(other.Token, product, id, unitPrice: null, discount: 5000)).Status);
        Assert.Equal("5/0", await api.Stock(1, product));

        // Using an approval and a PIN together isn't allowed either
        var both = await api.Call(HttpMethod.Post, "/pos/sales", cashier.Token, new
        {
            items = new[] { new { productId = product, quantity = 1 } },
            payments = new[] { new { method = "Cash", amount = 45000m } },
            discountAmount = 5000m,
            approval = new { approverUserId = manager.Id, pin = Pin },
            approvalRequestId = id,
        });
        Assert.Equal(HttpStatusCode.BadRequest, both.Status);

        Assert.Equal(HttpStatusCode.Created, (await Sale(cashier.Token, product, id, unitPrice: null, discount: 5000)).Status);
    }

    [Fact]
    public async Task Approval_problems_are_marked_so_a_sale_that_fails_for_another_reason_keeps_its_approval()
    {
        var cashier = await Cashier("Tade");
        var manager = await Manager("Moji");
        var product = await api.NewProduct(50000, 1);
        // A name with "pin" in it, which the stock message repeats
        await api.Db(db => db.Products.Where(p => p.Id == product)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Name, "iPhone 15 Pink")));
        var id = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product, quantity: 2)));
        await ApiFactory.Ok(Approve(manager.Token, id));

        var noStock = await Sale(cashier.Token, product, id, quantity: 2);
        Assert.Equal(HttpStatusCode.BadRequest, noStock.Status);
        Assert.Contains("iPhone 15 Pink", noStock.Message);
        Assert.False(noStock.Has("code")); // not about the approval, which is still good
        Assert.Equal("Approved", (await api.Call(HttpMethod.Get, $"/pos/approval-requests/{id}", cashier.Token)).Str("status"));

        var changed = await Sale(cashier.Token, product, id, quantity: 1);
        Assert.Equal("approval", changed.Str("code"));

        // Stock arrives: the same approval completes the sale
        await api.Db(db => db.Inventories.Where(i => i.StoreId == 1 && i.ProductId == product)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.QuantityOnHand, 5)));
        Assert.Equal(HttpStatusCode.Created, (await Sale(cashier.Token, product, id, quantity: 2)).Status);

        // Answering a request that's no longer waiting
        var late = await Approve(manager.Token, id);
        Assert.Equal("answered", late.Str("code"));
        Assert.Equal("Moji Test has already approved this.", late.Message);
    }

    [Fact]
    public async Task Only_a_manager_of_that_shop_or_an_Admin_can_answer_and_not_their_own_request()
    {
        var shop = await api.NewShop("Yaba");
        var cashier = await Cashier("Temi", shop);
        var here = await Manager("Maro", shop);
        var elsewhere = await Manager("Bola");
        var product = await api.NewProduct(50000, 5);
        var id = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product)));

        Assert.Equal(HttpStatusCode.Forbidden, (await Approve(cashier.Token, id)).Status);
        var otherShop = await Approve(elsewhere.Token, id);
        Assert.Equal(HttpStatusCode.Forbidden, otherShop.Status);
        Assert.Equal("This request is for another shop.", otherShop.Message);

        // A manager selling at the till can't approve their own request
        var own = IdOf(await ApiFactory.Ok(Ask(here.Token, product)));
        Assert.Equal(HttpStatusCode.Forbidden, (await Approve(here.Token, own)).Status);

        // Declined, with a note for the cashier; then it can't be approved
        var declined = await Decline(here.Token, id, "₦3,000 off at most");
        Assert.Equal("Declined", declined.Str("status"));
        Assert.Equal("₦3,000 off at most", declined.Str("decisionNote"));
        var late = await Approve(api.AdminToken, id);
        Assert.Equal(HttpStatusCode.BadRequest, late.Status);
        Assert.Equal("Maro Test has already declined this.", late.Message);

        // An Admin can answer for any shop (the Admin's PIN isn't set, so set it first)
        await ApiFactory.Ok(api.Call(HttpMethod.Put, "/pos/approval-pin", api.AdminToken, new { currentPassword = api.Password, pin = Pin }));
        var next = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product)));
        Assert.Equal("Approved", (await Approve(api.AdminToken, next)).Str("status"));
    }

    [Fact]
    public async Task Two_managers_answering_at_once_only_one_counts()
    {
        var cashier = await Cashier("Tayo");
        var first = await Manager("Ngozi");
        var second = await Manager("Kemi");
        var product = await api.NewProduct(50000, 5);
        var id = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product)));

        var answers = await Task.WhenAll(Approve(first.Token, id), Decline(second.Token, id));

        Assert.Single(answers, a => a.Status == HttpStatusCode.OK);
        Assert.Single(answers, a => a.Status == HttpStatusCode.BadRequest && a.Message.Contains("has already"));
        var status = (await api.Call(HttpMethod.Get, $"/pos/approval-requests/{id}", cashier.Token)).Str("status");
        Assert.Equal(answers[0].Status == HttpStatusCode.OK ? "Approved" : "Declined", status);
    }

    [Fact]
    public async Task Unanswered_requests_expire_and_asking_again_replaces_the_last_one()
    {
        var cashier = await Cashier("Uche");
        var manager = await Manager("Femi");
        var product = await api.NewProduct(50000, 5);
        await using var managerLive = await api.Live(manager.Token);

        var first = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product)));
        var second = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product, unitPrice: 46000m))); // the cart changed
        await managerLive.Next("ApprovalUpdated", first, "Cancelled"); // its alert goes away
        Assert.Equal(HttpStatusCode.BadRequest, (await Approve(manager.Token, first)).Status);

        await api.Db(db => db.ApprovalRequests.Where(r => r.Id == second)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiresAt, DateTime.UtcNow.AddSeconds(-1))));
        var expired = await Approve(manager.Token, second);
        Assert.Equal(HttpStatusCode.BadRequest, expired.Status);
        Assert.Equal("This request has expired.", expired.Message);
        Assert.Equal("Expired", (await api.Call(HttpMethod.Get, $"/pos/approval-requests/{second}", cashier.Token)).Str("status"));
        var waiting = (await api.Call(HttpMethod.Get, "/pos/approval-requests?waiting=true", manager.Token)).Data!.AsArray();
        Assert.DoesNotContain(waiting, r => r!["id"]!.GetValue<int>() == second);

        // The cashier can withdraw a request
        var third = IdOf(await ApiFactory.Ok(Ask(cashier.Token, product)));
        Assert.Equal("Cancelled", (await api.Call(HttpMethod.Post, $"/pos/approval-requests/{third}/cancel", cashier.Token)).Str("status"));
        Assert.Equal("The cashier has cancelled this request.", (await Approve(manager.Token, third)).Message);

        // Nothing to approve
        var nothing = await Ask(cashier.Token, product, unitPrice: null);
        Assert.Equal(HttpStatusCode.BadRequest, nothing.Status);
    }

    [Fact]
    public async Task Only_signed_in_staff_can_connect_for_live_alerts()
    {
        var customer = await api.NewCustomer("Chidi");
        var cashier = await Cashier("Tola");

        await Assert.ThrowsAnyAsync<Exception>(() => api.Live(null));
        await Assert.ThrowsAnyAsync<Exception>(() => api.Live("not-a-token"));
        await Assert.ThrowsAnyAsync<Exception>(() => api.Live(customer.Token));
        await using var staff = await api.Live(cashier.Token); // connects
    }

    [Fact]
    public async Task The_PIN_at_the_till_still_works_for_the_shops_own_managers()
    {
        var shop = await api.NewShop("Surulere");
        var cashier = await Cashier("Tosin");
        var here = await Manager("Mide");
        var elsewhere = await Manager("Bimbo", shop);
        var product = await api.NewProduct(50000, 5);

        var approvers = (await api.Call(HttpMethod.Get, "/pos/approvers", cashier.Token)).Data!.AsArray()
            .Select(a => a!["id"]!.GetValue<int>()).ToList();
        Assert.Contains(here.Id, approvers);
        Assert.DoesNotContain(elsewhere.Id, approvers);

        Task<Res> sale(int approverUserId) => api.Call(HttpMethod.Post, "/pos/sales", cashier.Token, new
        {
            items = new[] { new { productId = product, quantity = 1, unitPrice = 45000m } },
            payments = new[] { new { method = "Cash", amount = 45000m } },
            discountReason = "Price match",
            approval = new { approverUserId, pin = Pin },
        });
        var otherShop = await sale(elsewhere.Id);
        Assert.Equal(HttpStatusCode.BadRequest, otherShop.Status);
        Assert.Equal("Bimbo isn't a manager at this shop.", otherShop.Message);

        var ok = await sale(here.Id);
        Assert.Equal(HttpStatusCode.Created, ok.Status);
        Assert.Equal("Mide Test", ok.Str("discountApprovedBy"));
        Assert.Equal(0, await api.Db(db => db.ApprovalRequests.CountAsync(r => r.RequestedByUserId == cashier.Id))); // no request needed
    }
}
