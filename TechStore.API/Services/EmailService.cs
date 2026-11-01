using Azure;
using Azure.Communication.Email;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class EmailService : IEmailService
{
    private readonly string _connectionString;
    private readonly string _from;
    private readonly string _fromName;
    private readonly string _siteUrl;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _connectionString = config["Email:ConnectionString"] ?? "";
        _from             = config["Email:From"] ?? "";
        _fromName         = config["Email:FromName"] ?? "Hytel Phones";
        _siteUrl          = (config["Email:SiteUrl"] ?? "").TrimEnd('/'); // the shop, for links in emails
        _logger           = logger;
    }

    public Task SendWelcomeAsync(string toEmail, string firstName) =>
        Send(toEmail, $"Welcome to Hytel Phones, {firstName}!", $"""
            <h2>Welcome, {firstName}!</h2>
            <p>Thank you for creating your Hytel Phones account. You can now shop our full range of smartphones, tablets, laptops and more.</p>
            <p><a href="{_siteUrl}/products" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block;margin-top:8px">Start Shopping</a></p>
            <p style="color:#6b7280;font-size:13px;margin-top:24px">If you didn't create this account, you can safely ignore this email.</p>
            """);

    public Task SendOrderConfirmationAsync(string toEmail, string firstName, string orderNumber, decimal total,
        DateTime? payBy = null, string? pickupShop = null, string orderPath = "/account/orders") =>
        Send(toEmail, $"Order Received — {orderNumber}", $"""
            <h2>Thanks for your order!</h2>
            <p>Hi {firstName}, your order <strong>{orderNumber}</strong> has been received.</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="padding:8px 0;color:#6b7280">Order Number</td><td style="font-family:monospace;font-weight:600">{orderNumber}</td></tr>
              <tr><td style="padding:8px 0;color:#6b7280">Total</td><td style="font-weight:600">₦{total:N2}</td></tr>
              <tr><td style="padding:8px 0;color:#6b7280">{(pickupShop != null ? "Pickup" : "Delivery")}</td><td>{(pickupShop != null ? $"Collect from {pickupShop}" : "To your address")}</td></tr>
            </table>
            {(payBy is { } due ? $"<p><strong>Please pay by bank transfer by {NigeriaTime(due)}</strong>, or the order will be cancelled. The account details are on your order page.</p>" : "")}
            <p>We'll email you when your payment is confirmed and again when your order is {(pickupShop != null ? "ready to collect" : "on its way")}.</p>
            <p><a href="{_siteUrl}{orderPath}" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block">View Order</a></p>
            """);

    public Task SendOrderStatusUpdateAsync(string toEmail, string firstName, string orderNumber, string newStatus, string? detail = null,
        string orderPath = "/account/orders") =>
        Send(toEmail, $"Order {orderNumber}: {newStatus}", $"""
            <h2>{newStatus}</h2>
            <p>Hi {firstName}, there's an update on your order <strong>{orderNumber}</strong>.</p>
            {(detail != null ? $"<p>{detail}</p>" : "")}
            <p><a href="{_siteUrl}{orderPath}" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block">Track Order</a></p>
            """);

    public Task SendPaymentReminderAsync(string toEmail, string firstName, string orderNumber, decimal total, DateTime payBy,
        string orderPath = "/account/orders") =>
        Send(toEmail, $"Reminder: payment for order {orderNumber}", $"""
            <h2>Your order is waiting for payment</h2>
            <p>Hi {firstName}, we haven't received payment for your order <strong>{orderNumber}</strong> yet.</p>
            <p>Please transfer <strong>₦{total:N2}</strong> by <strong>{NigeriaTime(payBy)}</strong>, or the order will be cancelled and the items go back on sale. The account details are on your order page.</p>
            <p>Already paid? Thank you, there's nothing more to do. We'll confirm as soon as it arrives.</p>
            <p><a href="{_siteUrl}{orderPath}" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block">View Order</a></p>
            """);

    // Nigeria is UTC+1 all year (no daylight saving)
    private static string NigeriaTime(DateTime utc) => $"{utc.AddHours(1):h:mm tt} on {utc.AddHours(1):d MMMM}";

    public Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink) =>
        Send(toEmail, "Reset Your Password", $"""
            <h2>Password Reset</h2>
            <p>Hi {firstName}, you requested a password reset for your Hytel Phones account.</p>
            <p><a href="{resetLink}" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block;margin:8px 0">Reset Password</a></p>
            <p style="color:#6b7280;font-size:13px">This link expires in 1 hour. If you did not request a reset, you can safely ignore this email.</p>
            """);

    private async Task Send(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_from))
        {
            _logger.LogWarning("Email not sent — no sender address (Email:From) configured. To: {Email}, Subject: {Subject}", toEmail, subject);
            return;
        }

        if (string.IsNullOrEmpty(_connectionString))
        {
            _logger.LogWarning("Email not sent — ACS connection string not configured. To: {Email}, Subject: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var client     = new EmailClient(_connectionString);
            var recipients = new EmailRecipients(new List<EmailAddress> { new(toEmail) });
            var msg        = new EmailMessage(_from, recipients, new EmailContent(subject) { Html = WrapHtml(subject, htmlBody) });
            var operation  = await client.SendAsync(WaitUntil.Started, msg);
            _logger.LogInformation("Email queued to {Email} — operation id: {Id}", toEmail, operation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
        }
    }

    private static string WrapHtml(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>{title}</title></head>
        <body style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:#f9fafb;margin:0;padding:32px 16px">
          <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:16px;padding:40px;box-shadow:0 1px 4px rgba(0,0,0,.06)">
            <div style="margin-bottom:32px">
              <span style="font-weight:700;font-size:20px;color:#2d52ff">Hytel Phones</span>
            </div>
            {body}
            <hr style="border:none;border-top:1px solid #f3f4f6;margin:32px 0">
            <p style="color:#9ca3af;font-size:12px;margin:0">Hytel Phones · Lagos, Nigeria</p>
          </div>
        </body>
        </html>
        """;
}
