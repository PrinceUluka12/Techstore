using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Order;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Delivery fees per state. A state with no fee has no delivery (customers there can pick up
// from a shop); a fee of 0 is free delivery.
public class DeliveryRateService(AppDbContext db) : IDeliveryRateService
{
    public async Task<IEnumerable<DeliveryRateDto>> GetActiveAsync() =>
        (await db.DeliveryRates.OrderBy(r => r.State).ToListAsync())
            .Select(r => new DeliveryRateDto(r.State, r.Fee));

    public async Task<IEnumerable<DeliveryRateAdminDto>> GetAllStatesAsync()
    {
        var rates = await db.DeliveryRates.ToDictionaryAsync(r => r.State);
        return NigerianStates.All.Select(s => rates.TryGetValue(s, out var r)
            ? new DeliveryRateAdminDto(s, r.Fee, r.UpdatedByName, r.UpdatedAt)
            : new DeliveryRateAdminDto(s, null, null, null));
    }

    // Saves the states sent; a blank fee stops delivery there. States not sent are left alone.
    public async Task<IEnumerable<DeliveryRateAdminDto>> SaveAsync(int actorId, IEnumerable<SaveDeliveryRateRequest> rates)
    {
        var by = await db.Users.Where(u => u.Id == actorId)
            .Select(u => (u.FirstName + " " + u.LastName).Trim()).FirstOrDefaultAsync();
        var existing = await db.DeliveryRates.ToDictionaryAsync(r => r.State);
        var now = DateTime.UtcNow;

        foreach (var req in rates)
        {
            var state = NigerianStates.All.FirstOrDefault(s => s.Equals(req.State?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"'{req.State}' isn't a Nigerian state.");
            if (req.Fee is < 0 or > 1_000_000)
                throw new InvalidOperationException($"The delivery fee for {state} must be between ₦0 and ₦1,000,000.");

            existing.TryGetValue(state, out var row);
            if (req.Fee == null)
            {
                if (row != null) { db.DeliveryRates.Remove(row); existing.Remove(state); }
                continue;
            }

            var fee = Math.Round(req.Fee.Value, 2);
            if (row == null)
            {
                row = new DeliveryRate { State = state, Fee = fee, UpdatedByName = by, UpdatedAt = now };
                db.DeliveryRates.Add(row);
                existing[state] = row;
            }
            else if (row.Fee != fee)
            {
                row.Fee = fee;
                row.UpdatedByName = by;
                row.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync();
        return await GetAllStatesAsync();
    }
}
