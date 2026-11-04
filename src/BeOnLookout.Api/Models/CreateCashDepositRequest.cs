using System.ComponentModel.DataAnnotations;

namespace BeOnLookout.Api.Models;

public sealed class CreateCashDepositRequest
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string CustomerReference { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal Amount { get; init; }
}
