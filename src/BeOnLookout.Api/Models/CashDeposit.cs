namespace BeOnLookout.Api.Models;

public sealed record CashDeposit(
    long Id,
    long? CustomerId,
    string CustomerReference,
    decimal Amount,
    DateTimeOffset DepositedAtUtc);
