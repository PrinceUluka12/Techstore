namespace TechStore.API.DTOs.Newsletter;

public record SubscribeRequest(string Email);
public record UnsubscribeRequest(string Token);

public record SubscriberDto(int Id, string Email, DateTime SubscribedAt, DateTime? UnsubscribedAt);

// A page of sign-ups, plus how many are still subscribed overall
public record NewsletterPage(IEnumerable<SubscriberDto> Items, int Total, int Page, int PageSize, int Active);
