using System.Net;

namespace TechStore.API.Tests;

// Terms, returns, privacy and disclaimer: anyone reads them, staff with "Edit Policy Pages" change them
[Collection("api")]
public class PolicyTests(ApiFactory api)
{
    [Fact]
    public async Task Anyone_can_read_a_page_and_until_its_edited_it_shows_the_starting_text()
    {
        var r = await api.Call(HttpMethod.Get, "/policies/terms");

        Assert.Equal(HttpStatusCode.OK, r.Status);
        Assert.Equal("Terms of Service & Compliance", r.Str("title"));
        Assert.Contains("## Who can buy", r.Str("body"));
        Assert.True(r["isDefault"]!.GetValue<bool>());
        Assert.Equal(HttpStatusCode.NotFound, (await api.Call(HttpMethod.Get, "/policies/about-us")).Status);
    }

    [Fact]
    public async Task Only_staff_who_can_edit_policy_pages_can_change_them()
    {
        var customer = await api.NewCustomer("Bayo");
        var cashier = await api.NewStaff("Tilly", await api.NewRole("Cashier", "pos.sell"));
        var body = new { body = "## Hijacked" };

        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Call(HttpMethod.Put, "/policies/returns", body: body)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Put, "/policies/returns", customer.Token, body)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Put, "/policies/returns", cashier.Token, body)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Get, "/policies/returns/history", cashier.Token)).Status);
        Assert.DoesNotContain("Hijacked", (await api.Call(HttpMethod.Get, "/policies/returns")).Str("body"));
    }

    [Fact]
    public async Task Each_save_is_kept_as_a_version_and_the_newest_is_shown()
    {
        var editor = await api.NewStaff("Ed", await api.NewRole("Policy Editor", "pages.manage"));
        Task<Res> save(string text, string? token = null) =>
            api.Call(HttpMethod.Put, "/policies/disclaimer", token ?? editor.Token, new { body = text });

        var first = await save("## Product information\n\nFirst wording.");
        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.False(first["isDefault"]!.GetValue<bool>());
        Assert.Equal("Ed Test", first.Str("updatedByName"));

        await save("## Product information\n\nFirst wording.   "); // the same text: no new version
        await save("## Product information\n\nSecond wording.", api.AdminToken); // Admins can too

        var shown = await api.Call(HttpMethod.Get, "/policies/disclaimer");
        Assert.Contains("Second wording.", shown.Str("body"));

        var history = (await api.Call(HttpMethod.Get, "/policies/disclaimer/history", editor.Token)).Data!.AsArray();
        Assert.Equal(2, history.Count);
        Assert.Contains("Second wording.", history[0]!["body"]!.GetValue<string>()); // newest first
        Assert.Equal("Ed Test", history[1]!["createdByName"]!.GetValue<string>());

        Assert.Equal(HttpStatusCode.BadRequest, (await save("   ")).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await save(new string('x', 30001))).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Call(HttpMethod.Put, "/policies/about-us", editor.Token, new { body = "x" })).Status);
    }
}
