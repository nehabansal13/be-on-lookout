namespace BeOnLookout.Api.Models;

public sealed record Customer(
    long Id,
    string CustomerReference,
    string DisplayName,
    DateTimeOffset CreatedAtUtc);
