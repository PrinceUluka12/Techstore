using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.DTOs.Newsletter;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class NewsletterService : INewsletterService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly string _siteUrl;

    public NewsletterService(AppDbContext db, IEmailService email, IConfiguration config)
    {
        _db = db;
        _email = email;
        _siteUrl = (config["Email:SiteUrl"] ?? "").TrimEnd('/');
    }

    // The reply is the same whether or not they were already on the list, so the box can't be
    // used to find out who is
    public async Task SubscribeAsync(string email)
    {
        email = email.Trim();
        var subscriber = await _db.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email);
        if (subscriber is { UnsubscribedAt: null }) return;

        if (subscriber == null)
        {
            subscriber = new NewsletterSubscriber
            {
                Email = email,
                UnsubscribeToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(24)),
            };
            _db.NewsletterSubscribers.Add(subscriber);
        }
        subscriber.SubscribedAt = DateTime.UtcNow;
        subscriber.UnsubscribedAt = null;

        // The same email twice at once: the unique index stops the second, and the first saved it.
        // Any other failure is a real error, not a sign-up, so it isn't caught.
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { return; }

        _ = _email.SendNewsletterWelcomeAsync(email, UnsubscribeLink(subscriber));
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        var subscriber = await _db.NewsletterSubscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);
        if (subscriber == null) return false;
        subscriber.UnsubscribedAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<NewsletterPage> ListAsync(int page, int pageSize, string? search)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.NewsletterSubscribers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(s => s.Email.Contains(search.Trim()));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(s => s.SubscribedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SubscriberDto(s.Id, s.Email, s.SubscribedAt, s.UnsubscribedAt))
            .ToListAsync();
        var active = await _db.NewsletterSubscribers.CountAsync(s => s.UnsubscribedAt == null);
        return new NewsletterPage(items, total, page, pageSize, active);
    }

    // For a mailing tool: everyone still subscribed, each with their own unsubscribe link
    public async Task<string> ExportCsvAsync()
    {
        var subscribers = await _db.NewsletterSubscribers.AsNoTracking()
            .Where(s => s.UnsubscribedAt == null).OrderBy(s => s.SubscribedAt).ToListAsync();

        var csv = new StringBuilder("email,subscribed_at,unsubscribe_link\r\n");
        foreach (var s in subscribers)
            csv.Append(Csv(s.Email)).Append(',')
               .Append(s.SubscribedAt.ToString("yyyy-MM-dd")).Append(',')
               .Append(Csv(UnsubscribeLink(s))).Append("\r\n");
        return csv.ToString();
    }

    private string UnsubscribeLink(NewsletterSubscriber s) => $"{_siteUrl}/newsletter/unsubscribe?token={s.UnsubscribeToken}";

    // Quotes a value that would break the row, and stops a spreadsheet treating it as a formula
    private static string Csv(string value)
    {
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
