namespace TechStore.API.Models;

// People who signed up in the shop's footer. Staff download the list, with each person's
// unsubscribe link, to use with a mailing tool.
public class NewsletterSubscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UnsubscribeToken { get; set; } = string.Empty;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }
}

// A job that runs once a day records the day here, so a restart (or a second copy of the app)
// doesn't run it twice
public class JobRun
{
    public const string LowStockAlert = "LowStockAlert";

    public string Name { get; set; } = string.Empty;
    public DateOnly LastRunOn { get; set; }
}
