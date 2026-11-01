using System.Net;
using Microsoft.EntityFrameworkCore;

namespace TechStore.API.Tests;

[Collection("api")]
public class ReviewTests(ApiFactory api)
{
    private async Task<int> BuyAndReceive(string token, int productId)
    {
        var order = (await api.Checkout(token, [(productId, 1)])).Int("id");
        await ApiFactory.Ok(api.MarkPaid(order));
        await ApiFactory.Ok(api.SetStatus(order, "Shipped"));
        await ApiFactory.Ok(api.SetStatus(order, "Delivered"));
        return order;
    }

    [Fact]
    public async Task Reviews_need_a_sign_in_and_a_rating_from_1_to_5()
    {
        var phone = await api.NewProduct(20000, stock: 5);
        var buyer = await api.NewCustomer("Bayo");

        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Get, $"/reviews/product/{phone}")).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Call(HttpMethod.Post, "/reviews", body: new { productId = phone, rating = 5 })).Status);
        var r = await api.Call(HttpMethod.Post, "/reviews", buyer.Token, new { productId = phone, rating = 6 });
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
        Assert.Contains("between 1 and 5", r.Message);
    }

    [Fact]
    public async Task Buyers_reviews_are_verified_one_each_and_update_the_products_stars()
    {
        var phone = await api.NewProduct(20000, stock: 5);
        var buyer = await api.NewCustomer("Bayo");
        var browser = await api.NewCustomer("Chidi");
        await BuyAndReceive(buyer.Token, phone);

        var mine = await api.Call(HttpMethod.Post, "/reviews", buyer.Token, new { productId = phone, rating = 5, title = "Great" });
        Assert.Equal(HttpStatusCode.OK, mine.Status);
        Assert.True(mine["isVerifiedPurchase"]!.GetValue<bool>());

        var theirs = await api.Call(HttpMethod.Post, "/reviews", browser.Token, new { productId = phone, rating = 2 });
        Assert.False(theirs["isVerifiedPurchase"]!.GetValue<bool>());

        var again = await api.Call(HttpMethod.Post, "/reviews", buyer.Token, new { productId = phone, rating = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, again.Status);
        Assert.Contains("already reviewed", again.Message);

        var stars = await api.Db(db => db.Products.Where(p => p.Id == phone).Select(p => new { p.Rating, p.ReviewCount }).SingleAsync());
        Assert.Equal(2, stars.ReviewCount);
        Assert.Equal(3.5, stars.Rating!.Value, precision: 2);

        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Delete, $"/reviews/{mine.Int("id")}", browser.Token)).Status);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Call(HttpMethod.Delete, $"/reviews/{mine.Int("id")}", buyer.Token)).Status);
    }
}
