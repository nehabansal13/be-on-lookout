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

    Task<IReadOnlyList<CashDeposit>?> GetByCustomerIdAsync(
        long customerId,
        CancellationToken cancellationToken);

    Task<CashDepositSummary?> GetSummaryByCustomerIdAsync(
        long customerId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken);

    Task<CashDeposit?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken);
}
