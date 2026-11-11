using System.ComponentModel.DataAnnotations;

namespace BeOnLookout.Api.Models;

public sealed class CreateCashDepositRequest
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; init; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Amount { get; init; }
}
