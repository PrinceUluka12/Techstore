namespace TechStore.API.Helpers;

public static class Perms
{
    public const string OrdersView      = "orders.view";
    public const string OrdersManage    = "orders.manage";
    public const string ProductsView    = "products.view";
    public const string ProductsManage  = "products.manage";
    public const string InventoryManage = "inventory.manage";
    public const string UsersView       = "users.view";
    public const string UsersManage     = "users.manage";
    public const string RolesManage     = "roles.manage";
    public const string ReportsView     = "reports.view";
    public const string CouponsManage   = "coupons.manage";
    public const string ImagesManage    = "images.manage";

    public static readonly string[] All =
    [
        OrdersView, OrdersManage,
        ProductsView, ProductsManage,
        InventoryManage,
        UsersView, UsersManage,
        RolesManage,
        ReportsView,
        CouponsManage,
        ImagesManage,
    ];

    public static readonly Dictionary<string, string> Labels = new()
    {
        [OrdersView]      = "View Orders",
        [OrdersManage]    = "Manage Orders",
        [ProductsView]    = "View Products",
        [ProductsManage]  = "Manage Products",
        [InventoryManage] = "Manage Inventory",
        [UsersView]       = "View Users",
        [UsersManage]     = "Manage Users",
        [RolesManage]     = "Manage Roles",
        [ReportsView]     = "View Reports",
        [CouponsManage]   = "Manage Coupons",
        [ImagesManage]    = "Manage Images",
    };
}
