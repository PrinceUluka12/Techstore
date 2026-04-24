using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/payments/paystack")]
public class PaystackController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IHttpClientFactory _httpFactory;

    public PaystackController(
        IConfiguration config,
        AppDbContext db,
        IEmailService email,
        IHttpClientFactory httpFactory)
    {
        _config     = config;
        _db         = db;
        _email      = email;
        _httpFactory = httpFactory;
    }

    /// <summary>Verify a Paystack payment and mark the order as paid</summary>
    [HttpPost("verify/{reference}")]
    [Authorize]
    public async Task<IActionResult> Verify(string reference)
    {
        var secretKey = _config["Paystack:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            return StatusCode(503, new { message = "Payment verification not configured." });

        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secretKey);

        var res = await http.GetAsync($"https://api.paystack.co/transaction/verify/{Uri.EscapeDataString(reference)}");
        if (!res.IsSuccessStatusCode)
            return BadRequest(new { message = "Could not verify payment with Paystack." });

        var json   = await res.Content.ReadAsStringAsync();
        var doc    = JsonDocument.Parse(json);
        var data   = doc.RootElement.GetProperty("data");
        var status = data.GetProperty("status").GetString();

        if (status != "success")
            return BadRequest(new { message = $"Payment not successful (status: {status})." });

        // Find order by transaction reference or metadata
        var metadata = data.TryGetProperty("metadata", out var meta) ? meta : default;
        int orderId  = 0;
        if (metadata.ValueKind == JsonValueKind.Object &&
            metadata.TryGetProperty("order_id", out var oId))
            orderId = oId.GetInt32();

        if (orderId == 0)
            return BadRequest(new { message = "No order_id in payment metadata." });

        var order = await _db.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound(new { message = "Order not found." });

        // Only the order owner can verify
        var userId = GetUserId();
        if (order.UserId != userId)
            return Forbid();

        order.PaymentStatus = PaymentStatus.Paid;
        order.Status        = OrderStatus.Confirmed;
        order.TransactionId = reference;
        await _db.SaveChangesAsync();

        if (order.User != null)
            _ = _email.SendOrderStatusUpdateAsync(order.User.Email, order.User.FirstName, order.OrderNumber, "Confirmed");

        return Ok(new { message = "Payment verified. Order confirmed.", orderId = order.Id });
    }

    /// <summary>Paystack webhook — called directly by Paystack servers</summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var webhookSecret = _config["Paystack:WebhookSecret"];
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            Request.Headers.TryGetValue("x-paystack-signature", out var sig);
            var expected = ComputeHmac(body, webhookSecret);
            if (!sig.ToString().Equals(expected, StringComparison.OrdinalIgnoreCase))
                return Unauthorized();
        }

        await ProcessWebhookPayload(body);
        return Ok();
    }

    private async Task ProcessWebhookPayload(string body)
    {
        var doc   = JsonDocument.Parse(body);
        var ev    = doc.RootElement.GetProperty("event").GetString();
        if (ev != "charge.success") return;

        var data     = doc.RootElement.GetProperty("data");
        var reference = data.GetProperty("reference").GetString() ?? "";
        var metadata  = data.TryGetProperty("metadata", out var meta) ? meta : default;

        if (metadata.ValueKind != JsonValueKind.Object ||
            !metadata.TryGetProperty("order_id", out var oId)) return;

        var orderId = oId.GetInt32();
        var order   = await _db.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null || order.PaymentStatus == PaymentStatus.Paid) return;

        order.PaymentStatus = PaymentStatus.Paid;
        order.Status        = OrderStatus.Confirmed;
        order.TransactionId = reference;
        await _db.SaveChangesAsync();

        if (order.User != null)
            _ = _email.SendOrderStatusUpdateAsync(order.User.Email, order.User.FirstName, order.OrderNumber, "Confirmed");
    }

    private static string ComputeHmac(string data, string secret)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash      = HMACSHA512.HashData(keyBytes, dataBytes);
        return Convert.ToHexString(hash).ToLower();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
