using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using TechStore.API.Models;

namespace TechStore.API.Repositories;

public static class StockQueries
{
    // Loads only the stock row of the shop that sends online orders, which is what
    // the website shows and sells from (Product.OnlineInventory)
    public static IIncludableQueryable<Product, Store> IncludeOnlineStock(this IQueryable<Product> q) =>
        q.Include(p => p.Inventories.Where(i => i.Store.SendsOnlineOrders)).ThenInclude(i => i.Store);
}
