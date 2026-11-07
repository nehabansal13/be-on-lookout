using BeOnLookout.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeOnLookout.Api.Data;

public sealed class CashDepositRepository : ICashDepositRepository
{
    private const string SelectColumns =
        "CashDepositId, CustomerReference, Amount, DepositedAtUtc";

    private readonly IConfiguration _configuration;

    public CashDepositRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<CashDeposit> CreateAsync(
        CreateCashDepositRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO dbo.CashDeposits (CustomerReference, Amount)
            OUTPUT INSERTED.CashDepositId,
                   INSERTED.CustomerReference,
                   INSERTED.Amount,
                   INSERTED.DepositedAtUtc
            VALUES (@CustomerReference, @Amount);
            """;

        command.Parameters.Add(
            new SqlParameter("@CustomerReference", SqlDbType.NVarChar, 64)
            {
                Value = request.CustomerReference.Trim()
            });
        command.Parameters.Add(
            new SqlParameter("@Amount", SqlDbType.Decimal)
            {
                Precision = 11,
                Scale = 2,
                Value = request.Amount
            });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ReadCashDeposit(reader);
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

    private static CashDeposit ReadCashDeposit(SqlDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetDecimal(2),
            reader.GetDateTimeOffset(3));
}
