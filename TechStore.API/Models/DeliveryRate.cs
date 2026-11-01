namespace TechStore.API.Models;

// The delivery fee for one state. States without a row don't get delivery (pickup only).
public class DeliveryRate
{
    public int Id { get; set; }
    public string State { get; set; } = string.Empty; // one of NigerianStates.All
    public decimal Fee { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class NigerianStates
{
    // Same names as the checkout's state list
    public static readonly string[] All =
    [
        "Abia", "Adamawa", "Akwa Ibom", "Anambra", "Bauchi", "Bayelsa", "Benue", "Borno",
        "Cross River", "Delta", "Ebonyi", "Edo", "Ekiti", "Enugu", "FCT - Abuja", "Gombe",
        "Imo", "Jigawa", "Kaduna", "Kano", "Katsina", "Kebbi", "Kogi", "Kwara", "Lagos",
        "Nasarawa", "Niger", "Ogun", "Ondo", "Osun", "Oyo", "Plateau", "Rivers", "Sokoto",
        "Taraba", "Yobe", "Zamfara",
    ];
}
