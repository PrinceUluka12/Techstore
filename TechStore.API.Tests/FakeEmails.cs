using System.Collections.Concurrent;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Tests;

/// <summary>Records the emails the API would send, so tests can check who'd get what and follow its links.</summary>
public class FakeEmails : IEmailService
{
    public record Email(string Kind, string To, object? Detail = null);

    private readonly ConcurrentQueue<Email> _sent = new();

    public IReadOnlyList<Email> To(string address) =>
        _sent.Where(e => string.Equals(e.To, address, StringComparison.OrdinalIgnoreCase)).ToList();

    private Task Record(string kind, string to, object? detail = null)
    {
        _sent.Enqueue(new Email(kind, to, detail));
        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string toEmail, string firstName) => Record("Welcome", toEmail);
    public Task SendOrderConfirmationAsync(string toEmail, string firstName, string orderNumber, decimal total,
        DateTime? payBy = null, string? pickupShop = null, string orderPath = "/account/orders") => Record("OrderConfirmation", toEmail, orderPath);
    public Task SendOrderStatusUpdateAsync(string toEmail, string firstName, string orderNumber, string newStatus, string? detail = null,
        string orderPath = "/account/orders") => Record("OrderStatus", toEmail, orderPath);
    public Task SendPaymentReminderAsync(string toEmail, string firstName, string orderNumber, decimal total, DateTime payBy,
        string orderPath = "/account/orders") => Record("PaymentReminder", toEmail, orderPath);
    public Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink) => Record("PasswordReset", toEmail, resetLink);
    public Task SendNewsletterWelcomeAsync(string toEmail, string unsubscribeLink) => Record("NewsletterWelcome", toEmail, unsubscribeLink);
    public Task SendLowStockAlertAsync(string toEmail, string firstName, IReadOnlyList<LowStockLine> lines) => Record("LowStock", toEmail, lines);
    public Task SendClaimOrdersAsync(string toEmail, string firstName, int count, string token) => Record("ClaimOrders", toEmail, token);
}
