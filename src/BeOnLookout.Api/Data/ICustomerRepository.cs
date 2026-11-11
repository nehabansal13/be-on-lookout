using BeOnLookout.Api.Models;

namespace BeOnLookout.Api.Data;

public interface ICustomerRepository
{
    Task<Customer> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> GetLatestAsync(
        int take,
        CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);
}
