using BeOnLookout.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeOnLookout.Api.Data;

public sealed class CashDepositRepository : ICashDepositRepository
{
    private const string SelectColumns =
        "CashDepositId, CustomerId, CustomerReference, Amount, DepositedAtUtc";

    private readonly IConfiguration _configuration;

    public CashDepositRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<CashDeposit?> CreateAsync(
        CreateCashDepositRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO dbo.CashDeposits (CustomerId, CustomerReference, Amount)
            OUTPUT INSERTED.CashDepositId,
                   INSERTED.CustomerId,
                   INSERTED.CustomerReference,
                   INSERTED.Amount,
                   INSERTED.DepositedAtUtc
            SELECT @CustomerId, customer.CustomerReference, @Amount
            FROM dbo.Customers AS customer
            WHERE customer.CustomerId = @CustomerId;
            """;

        command.Parameters.Add(
            new SqlParameter("@CustomerId", SqlDbType.BigInt)
            {
                Value = request.CustomerId
            });
        command.Parameters.Add(
            new SqlParameter("@Amount", SqlDbType.Decimal)
            {
                Precision = 11,
                Scale = 2,
                Value = request.Amount
            });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadCashDeposit(reader)
            : null;
    }

    public async Task<IReadOnlyList<CashDeposit>> GetLatestAsync(
        int take,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT TOP (@Take) {SelectColumns}
            FROM dbo.CashDeposits
            ORDER BY DepositedAtUtc DESC, CashDepositId DESC;
            """;
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = take });

        var deposits = new List<CashDeposit>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            deposits.Add(ReadCashDeposit(reader));
        }

        return deposits;
    }

    public async Task<CashDeposit?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {SelectColumns}
            FROM dbo.CashDeposits
            WHERE CashDepositId = @Id;
            """;
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadCashDeposit(reader)
            : null;
    }

    public async Task<IReadOnlyList<CashDeposit>?> GetByCustomerIdAsync(
        long customerId,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT customer.CustomerId,
                   deposit.CashDepositId,
                   deposit.CustomerId,
                   deposit.CustomerReference,
                   deposit.Amount,
                   deposit.DepositedAtUtc
            FROM dbo.Customers AS customer
            LEFT JOIN dbo.CashDeposits AS deposit
                ON deposit.CustomerId = customer.CustomerId
            WHERE customer.CustomerId = @CustomerId
            ORDER BY deposit.DepositedAtUtc DESC, deposit.CashDepositId DESC;
            """;
        command.Parameters.Add(
            new SqlParameter("@CustomerId", SqlDbType.BigInt) { Value = customerId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var deposits = new List<CashDeposit>();
        if (!reader.IsDBNull(1))
        {
            deposits.Add(ReadCashDeposit(reader, 1));
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            deposits.Add(ReadCashDeposit(reader, 1));
        }

        return deposits;
    }

    private SqlConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("BankingDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:BankingDatabase is not configured.");
        }

        return new SqlConnection(connectionString);
    }

    private static CashDeposit ReadCashDeposit(SqlDataReader reader, int offset = 0) =>
        new(
            reader.GetInt64(offset),
            reader.IsDBNull(offset + 1) ? null : reader.GetInt64(offset + 1),
            reader.GetString(offset + 2),
            reader.GetDecimal(offset + 3),
            reader.GetDateTimeOffset(offset + 4));
}
