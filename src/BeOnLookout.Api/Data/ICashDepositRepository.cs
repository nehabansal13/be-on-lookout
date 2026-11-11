using BeOnLookout.Api.Models;

namespace BeOnLookout.Api.Data;

public interface ICashDepositRepository
{
    Task<CashDeposit?> CreateAsync(
        CreateCashDepositRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CashDeposit>> GetLatestAsync(
        int take,
        CancellationToken cancellationToken);

    Task<CashDeposit?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);
}
