using System.Net;

namespace TechStore.API.Tests;

[Collection("api")]
public class ProductTests(ApiFactory api)
{
    [Fact]
    public async Task The_warranty_is_set_per_product_and_can_be_cleared()
    {
        var phone = await api.NewProduct(20000, stock: 1);
        Task<Res> update(object body) => api.Call(HttpMethod.Put, $"/products/{phone}", api.AdminToken, body);

        Assert.False((await api.Call(HttpMethod.Get, $"/products/{phone}")).Has("warranty")); // none until it's set

        Assert.Equal(HttpStatusCode.OK, (await update(new { warranty = "  1-year Samsung warranty " })).Status);
        Assert.Equal("1-year Samsung warranty", (await api.Call(HttpMethod.Get, $"/products/{phone}")).Str("warranty"));

        Assert.Equal(HttpStatusCode.OK, (await update(new { price = 21000 })).Status); // other edits leave it alone
        Assert.Equal("1-year Samsung warranty", (await api.Call(HttpMethod.Get, $"/products/{phone}")).Str("warranty"));

        Assert.Equal(HttpStatusCode.BadRequest, (await update(new { warranty = new string('x', 101) })).Status);

        Assert.Equal(HttpStatusCode.OK, (await update(new { warranty = "" })).Status);
        Assert.False((await api.Call(HttpMethod.Get, $"/products/{phone}")).Has("warranty"));
    }
}
