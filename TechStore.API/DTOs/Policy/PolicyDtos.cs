namespace TechStore.API.DTOs.Policy;

// IsDefault: never saved, so it's the starting text
public record PolicyPageDto(string Slug, string Title, string Body, DateTime UpdatedAt, string? UpdatedByName, bool IsDefault);
public record PolicyVersionDto(int Id, DateTime CreatedAt, string CreatedByName, string Body);
public record SavePolicyRequest(string Body);
