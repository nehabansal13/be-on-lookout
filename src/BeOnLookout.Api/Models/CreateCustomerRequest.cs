using System.ComponentModel.DataAnnotations;

namespace BeOnLookout.Api.Models;

public sealed class CreateCustomerRequest
{
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string CustomerReference { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string DisplayName { get; init; } = string.Empty;
}
