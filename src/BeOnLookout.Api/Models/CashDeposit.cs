namespace BeOnLookout.Api.Models;

public sealed record CashDeposit(
    long Id,
    string CustomerReference,
    decimal Amount,
    DateTimeOffset DepositedAtUtc);
