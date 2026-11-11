namespace BeOnLookout.Api.Models;

public sealed record CashDepositSummary(
    long CustomerId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    long DepositCount,
    decimal TotalAmount);
