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
    public const string StoresManage    = "stores.manage";
    public const string PosSell         = "pos.sell";
    public const string PosApproveDiscounts = "pos.discount.approve";
    public const string PosCashUpShop   = "pos.cashup.shop";
    public const string TransfersManage = "transfers.manage";

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
        StoresManage,
        PosSell,
        PosApproveDiscounts,
        PosCashUpShop,
        TransfersManage,
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
        [StoresManage]    = "Manage Shops",
        [PosSell]         = "Sell In Store",
        [PosApproveDiscounts] = "Approve Discounts",
        [PosCashUpShop]   = "See Whole-Shop Cash-up",
        [TransfersManage] = "Transfer Stock",
    };
}
