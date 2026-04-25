using Azure;
using Azure.Communication.Email;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class EmailService : IEmailService
{
    private readonly string _connectionString;
    private readonly string _from;
    private readonly string _fromName;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _connectionString = config["Email:ConnectionString"] ?? "";
        _from             = config["Email:From"] ?? "admin@pgusolutions.com";
        _fromName         = config["Email:FromName"] ?? "Hytel Phones";
        _logger           = logger;
    }

    public Task SendWelcomeAsync(string toEmail, string firstName) =>
        Send(toEmail, $"Welcome to Hytel Phones, {firstName}!", $"""
            <h2>Welcome, {firstName}!</h2>
            <p>Thank you for creating your Hytel Phones account. You can now shop our full range of smartphones, tablets, laptops and more.</p>
            <p><a href="https://www.pgusolutions.com/products" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block;margin-top:8px">Start Shopping</a></p>
            <p style="color:#6b7280;font-size:13px;margin-top:24px">If you didn't create this account, you can safely ignore this email.</p>
            """);

    public Task SendOrderConfirmationAsync(string toEmail, string firstName, string orderNumber, decimal total) =>
        Send(toEmail, $"Order Confirmed — {orderNumber}", $"""
            <h2>Order Confirmed!</h2>
            <p>Hi {firstName}, your order <strong>{orderNumber}</strong> has been received.</p>
            <table style="width:100%;border-collapse:collapse;margin:16px 0">
              <tr><td style="padding:8px 0;color:#6b7280">Order Number</td><td style="font-family:monospace;font-weight:600">{orderNumber}</td></tr>
              <tr><td style="padding:8px 0;color:#6b7280">Total</td><td style="font-weight:600">₦{total:N2}</td></tr>
              <tr><td style="padding:8px 0;color:#6b7280">Status</td><td><span style="background:#fef3c7;color:#92400e;padding:2px 10px;border-radius:999px;font-size:13px">Pending</span></td></tr>
            </table>
            <p>We'll send you another email when your order ships. You can track your order in your account.</p>
            <p><a href="https://www.pgusolutions.com/account/orders" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block">View Order</a></p>
            """);

    public Task SendOrderStatusUpdateAsync(string toEmail, string firstName, string orderNumber, string newStatus) =>
        Send(toEmail, $"Order Update — {orderNumber}", $"""
            <h2>Order Status Updated</h2>
            <p>Hi {firstName}, your order <strong>{orderNumber}</strong> has been updated.</p>
            <p>New status: <strong>{newStatus}</strong></p>
            <p><a href="https://www.pgusolutions.com/account/orders" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block">Track Order</a></p>
            """);

    public Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink) =>
        Send(toEmail, "Reset Your Password", $"""
            <h2>Password Reset</h2>
            <p>Hi {firstName}, you requested a password reset for your Hytel Phones account.</p>
            <p><a href="{resetLink}" style="background:#2d52ff;color:#fff;padding:10px 22px;border-radius:8px;text-decoration:none;display:inline-block;margin:8px 0">Reset Password</a></p>
            <p style="color:#6b7280;font-size:13px">This link expires in 1 hour. If you did not request a reset, you can safely ignore this email.</p>
            """);

    private async Task Send(string toEmail, string subject, string htmlBody)
    {
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
