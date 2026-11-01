using System.Net;
using Microsoft.EntityFrameworkCore;

namespace TechStore.API.Tests;

// What staff with "Manage users" or "Manage roles" can and can't do, and the sign-in limit
[Collection("api")]
public class StaffAndSecurityTests(ApiFactory api)
{
    private Task<Res> SetRole(string token, int userId, string role) =>
        api.Call(HttpMethod.Put, $"/admin/users/{userId}/role", token, new { role });

    private async Task<(string Manager, int ManagerId, string ManagerRole, string HeadOfficeRole)> ShopManager()
    {
        var managerRole = await api.NewRole("Shop Manager", "users.manage", "users.view", "pos.sell", "orders.view");
        var headOfficeRole = await api.NewRole("Head Office", "users.manage", "users.view", "pos.sell", "orders.view", "reports.view");
        var manager = await api.NewStaff("Mo", managerRole);
        return (manager.Token, manager.Id, managerRole, headOfficeRole);
    }

    [Fact]
    public async Task A_manager_cant_make_anyone_an_admin_or_give_more_than_they_have()
    {
        var (manager, managerId, _, headOffice) = await ShopManager();
        var buyer = await api.NewCustomer("Bayo");

        var r = await SetRole(manager, managerId, "Admin");
        Assert.Equal(HttpStatusCode.Forbidden, r.Status);
        Assert.Contains("own role", r.Message);

        Assert.Equal(HttpStatusCode.Forbidden, (await SetRole(manager, buyer.Id, "Admin")).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await SetRole(manager, buyer.Id, "admin")).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await SetRole(manager, buyer.Id, headOffice)).Status);
        Assert.Equal("Customer", await api.RoleOf(buyer.Email));
    }

    [Fact]
    public async Task A_manager_can_give_a_role_within_their_own_permissions()
    {
        var (manager, _, _, _) = await ShopManager();
        var cashierRole = await api.NewRole("Cashier", "pos.sell");
        var buyer = await api.NewCustomer("Bayo");

        Assert.Equal(HttpStatusCode.OK, (await SetRole(manager, buyer.Id, cashierRole.ToLowerInvariant())).Status);
        Assert.Equal(cashierRole, await api.RoleOf(buyer.Email)); // saved as spelt in Roles
    }

    [Fact]
    public async Task A_manager_cant_change_or_switch_off_an_admin_or_themselves()
    {
        var (manager, managerId, _, _) = await ShopManager();
        var adminId = await api.Db(db => db.Users.Where(u => u.Role == "Admin").Select(u => u.Id).FirstAsync());
        var cashier = await api.NewStaff("Tilly", await api.NewRole("Cashier", "pos.sell"));

        Assert.Equal(HttpStatusCode.Forbidden, (await SetRole(manager, adminId, "Customer")).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Put, $"/admin/users/{adminId}/toggle", manager)).Status);
        var r = await api.Call(HttpMethod.Put, $"/admin/users/{managerId}/toggle", manager);
        Assert.Equal(HttpStatusCode.Forbidden, r.Status);
        Assert.Contains("your own account", r.Message);

        Assert.Equal(HttpStatusCode.OK, (await api.Call(HttpMethod.Put, $"/admin/users/{cashier.Id}/toggle", manager)).Status);
        Assert.False(await api.Db(db => db.Users.Where(u => u.Id == cashier.Id).Select(u => u.IsActive).SingleAsync()));
    }

    [Fact]
    public async Task A_manager_cant_add_staff_above_themselves()
    {
        var (manager, _, _, headOffice) = await ShopManager();
        var email = ApiFactory.Unique("sneaky");

        var r = await api.Call(HttpMethod.Post, "/admin/users/staff", manager,
            new { firstName = "Sneaky", lastName = "Test", email, password = api.Password, role = headOffice, storeId = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, r.Status);
        Assert.False(await api.Db(db => db.Users.AnyAsync(u => u.Email == email)));
    }

    [Fact]
    public async Task A_role_editor_cant_create_a_role_with_permissions_they_dont_have()
    {
        var editor = await api.NewStaff("Ed", await api.NewRole("Role Editor", "roles.manage", "pos.sell"));

        var r = await api.Call(HttpMethod.Post, "/roles", editor.Token, new { name = $"Sneaky {Guid.NewGuid():N}", permissions = new[] { "users.manage" } });
        Assert.Equal(HttpStatusCode.Forbidden, r.Status);

        r = await api.Call(HttpMethod.Post, "/roles", editor.Token, new { name = $"Till Only {Guid.NewGuid():N}", permissions = new[] { "pos.sell" } });
        Assert.Equal(HttpStatusCode.OK, r.Status);
    }

    [Fact]
    public async Task Customers_are_kept_out_of_the_back_office()
    {
        var buyer = await api.NewCustomer("Bayo");

        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Get, "/admin/users", buyer.Token)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Call(HttpMethod.Put, "/delivery-rates", buyer.Token, new[] { new { state = "Lagos", fee = 1 } })).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Call(HttpMethod.Get, "/orders/my")).Status);
    }

    [Fact]
    public async Task Sign_in_is_limited_to_10_tries_a_minute_per_visitor()
    {
        var email = await api.Register("Bayo");
        var ip = $"192.0.2.{Random.Shared.Next(1, 255)}";
        Task<Res> login(string from) => api.Call(HttpMethod.Post, "/auth/login", body: new { email, password = "wrong-password" }, fromIp: from);

        for (var i = 0; i < 10; i++) Assert.NotEqual(HttpStatusCode.TooManyRequests, (await login(ip)).Status);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await login(ip)).Status);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await login("198.51.100.7")).Status); // someone else isn't locked out
    }
}
