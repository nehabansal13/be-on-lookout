using BeOnLookout.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BeOnLookout.Api.Data;

public sealed class CustomerRepository : ICustomerRepository
{
    private const string SelectColumns =
        "CustomerId, CustomerReference, DisplayName, CreatedAtUtc";

    private readonly IConfiguration _configuration;

    public CustomerRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Customer> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO dbo.Customers (CustomerReference, DisplayName)
            OUTPUT INSERTED.CustomerId,
                   INSERTED.CustomerReference,
                   INSERTED.DisplayName,
                   INSERTED.CreatedAtUtc
            VALUES (@CustomerReference, @DisplayName);
            """;

        command.Parameters.Add(
            new SqlParameter("@CustomerReference", SqlDbType.NVarChar, 64)
            {
                Value = request.CustomerReference.Trim()
            });
        command.Parameters.Add(
            new SqlParameter("@DisplayName", SqlDbType.NVarChar, 128)
            {
                Value = request.DisplayName.Trim()
            });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ReadCustomer(reader);
    }

    public async Task<IReadOnlyList<Customer>> GetLatestAsync(
        int take,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT TOP (@Take) {SelectColumns}
            FROM dbo.Customers
            ORDER BY CreatedAtUtc DESC, CustomerId DESC;
            """;
        command.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = take });

        var customers = new List<Customer>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(ReadCustomer(reader));
        }

        return customers;
    }

    public async Task<Customer?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {SelectColumns}
            FROM dbo.Customers
            WHERE CustomerId = @Id;
            """;
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadCustomer(reader)
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

    private static Customer ReadCustomer(SqlDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetDateTimeOffset(3));
}
